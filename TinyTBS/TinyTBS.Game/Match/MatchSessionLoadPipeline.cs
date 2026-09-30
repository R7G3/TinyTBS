using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Levels;
using TinyTBS.Game.Levels.Models;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Saves.Models;
using TinyTBS.Game.Scripting;

namespace TinyTBS.Game.Match;

/// <summary>
/// Builds a match session in discrete stages. Call <see cref="AnnounceNextStage"/>,
/// redraw, then <see cref="RunAnnouncedStage"/> so the UI can show the label before heavy work.
/// </summary>
public sealed class MatchSessionLoadPipeline
{
    public const string DefaultScenarioModuleId = "vanilla_scenario";

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteBatch _spriteBatch;
    private readonly IAssetResolver _assets;
    private readonly IFileContentProvider _files;
    private readonly IUserDataPaths _userDataPaths;
    private readonly string _levelId;
    private readonly string _scenarioModuleId;
    private readonly MatchContentComposition? _compositionOverride;
    private readonly int? _playerCountOverride;
    private readonly int? _startingGoldOverride;
    private readonly int? _unitCapOverride;
    private readonly IReadOnlyList<MatchPlayerSeat>? _playerSeatsOverride;
    private readonly MatchRuntimeSnapshot? _hydrateSnapshot;
    private readonly IReadOnlyList<LoadStage> _stages;

    private MatchContentLoadResult? _matchContent;
    private LevelDefinition? _level;
    private MatchState? _state;
    private MapScriptHost? _scriptHost;
    private MatchTextureAtlas? _textures;
    private MatchScene? _scene;
    private CursorHighlightRenderer? _cursorHighlight;
    private MinimapRenderer? _minimap;
    private UnitLevelLabelRenderer? _unitLevelLabels;
    private int _nextStageIndex;
    private bool _stageAnnounced;

    /// <summary>Optional heartbeat while a main-thread stage runs (keeps loading UI / background alive).</summary>
    public Action? UiPump { get; set; }

    private readonly record struct LoadStage(string Label, Action Work, bool RequiresMainThread);

    public MatchSessionLoadPipeline(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        IUserDataPaths userDataPaths,
        string levelId,
        string? scenarioModuleId = null,
        MatchContentComposition? composition = null,
        int? playerCount = null,
        int? startingGold = null,
        int? unitCap = null,
        IReadOnlyList<MatchPlayerSeat>? playerSeats = null,
        MatchRuntimeSnapshot? hydrateSnapshot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);

        _graphicsDevice = graphicsDevice;
        _content = content;
        _spriteBatch = spriteBatch;
        _assets = assets;
        _files = files;
        _userDataPaths = userDataPaths;
        _levelId = levelId.Trim();
        _scenarioModuleId = string.IsNullOrWhiteSpace(scenarioModuleId)
            ? DefaultScenarioModuleId
            : scenarioModuleId.Trim();
        _compositionOverride = composition;
        _playerCountOverride = playerCount;
        _startingGoldOverride = startingGold;
        _unitCapOverride = unitCap;
        _playerSeatsOverride = playerSeats;
        _hydrateSnapshot = hydrateSnapshot;

        _stages =
        [
            new("Loading match content…", LoadMatchContentComposition, RequiresMainThread: false),
            new("Loading level and map…", LoadLevelAndMap, RequiresMainThread: false),
            new("Preparing match state…", PrepareMatchState, RequiresMainThread: false),
            new("Compiling map script…", CompileMapScript, RequiresMainThread: false),
            new("Loading textures…", LoadTextures, RequiresMainThread: true),
            new("Building scene…", BuildScene, RequiresMainThread: true),
        ];

        Progress = MatchLoadProgress.Starting(_stages.Count, "Preparing…");
    }

    public MatchLoadProgress Progress { get; private set; }

    public GameplaySession? Result { get; private set; }

    public bool IsComplete => Result is not null;

    public bool HasPendingStages => !IsComplete && _nextStageIndex < _stages.Count;

    /// <summary>
    /// True after <see cref="AnnounceNextStage"/> when the pending work must stay on the game thread
    /// (GraphicsDevice / Content). Other stages may run on a worker so Draw keeps pumping.
    /// </summary>
    public bool AnnouncedStageRequiresMainThread =>
        _stageAnnounced
        && _nextStageIndex >= 0
        && _nextStageIndex < _stages.Count
        && _stages[_nextStageIndex].RequiresMainThread;

    /// <summary>Updates progress text for the next stage without running it yet.</summary>
    public bool AnnounceNextStage()
    {
        if (!HasPendingStages || _stageAnnounced)
            return false;

        var stage = _stages[_nextStageIndex];
        Progress = new MatchLoadProgress(_nextStageIndex, _stages.Count, stage.Label);
        _stageAnnounced = true;
        return true;
    }

    /// <summary>Runs the stage previously announced via <see cref="AnnounceNextStage"/>.</summary>
    public void RunAnnouncedStage()
    {
        if (!_stageAnnounced || _nextStageIndex >= _stages.Count)
            throw new InvalidOperationException("No announced match-load stage to run.");

        var stage = _stages[_nextStageIndex];
        stage.Work();
        _nextStageIndex++;
        _stageAnnounced = false;
        Progress = new MatchLoadProgress(_nextStageIndex, _stages.Count, stage.Label);

        if (_nextStageIndex < _stages.Count)
            return;

        Result = AssembleSession();
        Progress = new MatchLoadProgress(_stages.Count, _stages.Count, "Ready");
    }

    /// <summary>Runs every remaining stage synchronously (no UI yield).</summary>
    public GameplaySession RunToCompletion()
    {
        while (HasPendingStages)
        {
            AnnounceNextStage();
            RunAnnouncedStage();
        }

        return Result
            ?? throw new InvalidOperationException("Match load pipeline finished without a session.");
    }

    private void LoadMatchContentComposition()
    {
        var locator = new ContentModuleLocator(_files, _userDataPaths);
        MatchContentComposition composition;
        if (_compositionOverride is not null)
        {
            composition = _compositionOverride;
        }
        else
        {
            var scenario = ScenarioModuleLoader.Load(locator.ResolveModuleRoot(_scenarioModuleId), _files);
            composition = MatchContentComposition.FromScenarioDefaults(scenario);
        }

        _matchContent = MatchContentCompositionLoader.Load(composition, locator, _files);
    }

    private void LoadLevelAndMap()
    {
        ArgumentNullException.ThrowIfNull(_matchContent);
        _level = LevelFolderLoader.LoadFromModuleLevels(_matchContent.Scenario.ModuleRootPath, _levelId, _files);
        MatchContentCompositionLoader.ValidateMapTypes(
            _level.Map,
            _matchContent.Catalog,
            _matchContent.Replaces);
    }

    private void PrepareMatchState()
    {
        ArgumentNullException.ThrowIfNull(_level);
        ArgumentNullException.ThrowIfNull(_matchContent);

        var playerCount = _playerCountOverride ?? _level.Players.DefaultSlots;
        playerCount = Math.Clamp(playerCount, _level.Players.Min, _level.Players.Max);
        if (_hydrateSnapshot is not null)
            playerCount = _hydrateSnapshot.PlayerCount;

        var unitCap = _unitCapOverride ?? _level.DefaultUnitCap;
        if (_hydrateSnapshot is not null)
            unitCap = _hydrateSnapshot.UnitCap;

        _state = MatchState.FromMap(
            _level.Map,
            _matchContent.Catalog,
            _matchContent.Replaces,
            playerCount: playerCount,
            startingGold: _startingGoldOverride ?? _level.DefaultStartingGold,
            unitCap: unitCap,
            victoryType: _level.Victory.Type,
            defeatType: _level.Defeat.Type);

        if (_hydrateSnapshot is not null)
            _state.HydrateFromSnapshot(_hydrateSnapshot);
    }

    private void CompileMapScript()
    {
        ArgumentNullException.ThrowIfNull(_state);
        ArgumentNullException.ThrowIfNull(_level);

        var scriptEngine = new RoslynMapScriptEngine();
        _scriptHost = MapScriptHost.LoadForMap(
            _state,
            _level.Map.ScriptPath,
            _files,
            scriptEngine);

        // Fresh matches fire turn-start hooks; resumed saves keep mid-match state via snapshot.
        if (_hydrateSnapshot is null)
        {
            _scriptHost.NotifyMatchStarted(_state);
            _state.EvaluateStandardOutcome();
        }
    }

    private void LoadTextures()
    {
        ArgumentNullException.ThrowIfNull(_matchContent);
        _textures = MatchTextureAtlas.Load(
            _graphicsDevice,
            _content,
            _assets,
            _matchContent.Catalog,
            onItemLoaded: () => UiPump?.Invoke());
    }

    private void BuildScene()
    {
        ArgumentNullException.ThrowIfNull(_state);
        ArgumentNullException.ThrowIfNull(_textures);

        _scene = new MatchScene(_state, _graphicsDevice, _spriteBatch, _textures);
        _cursorHighlight = new CursorHighlightRenderer(_graphicsDevice);
        _minimap = new MinimapRenderer(_graphicsDevice);
        _unitLevelLabels = new UnitLevelLabelRenderer(_content.Load<SpriteFont>("Fonts/MatchCell"));
    }

    private GameplaySession AssembleSession()
    {
        ArgumentNullException.ThrowIfNull(_state);
        ArgumentNullException.ThrowIfNull(_scene);
        ArgumentNullException.ThrowIfNull(_cursorHighlight);
        ArgumentNullException.ThrowIfNull(_textures);
        ArgumentNullException.ThrowIfNull(_scriptHost);
        ArgumentNullException.ThrowIfNull(_level);
        ArgumentNullException.ThrowIfNull(_minimap);
        ArgumentNullException.ThrowIfNull(_matchContent);
        ArgumentNullException.ThrowIfNull(_unitLevelLabels);

        var unitCap = _unitCapOverride ?? _level.DefaultUnitCap;
        if (_hydrateSnapshot is not null)
            unitCap = _hydrateSnapshot.UnitCap;

        var levelBrief = new MatchLevelBrief
        {
            LevelId = _level.Id,
            Title = _level.Title,
            Description = _level.Description,
            VictoryType = _level.Victory.Type,
            DefeatType = _level.Defeat.Type,
            TeamDefeatMode = _level.TeamDefeatMode,
            UnitCap = unitCap,
        };

        var moduleVersions = MatchSaveDocumentFactory.CollectModuleVersions(
            _matchContent.Composition,
            _files,
            _userDataPaths);

        return new GameplaySession(
            _state,
            _scene,
            _cursorHighlight,
            _textures,
            _scriptHost,
            levelBrief,
            _minimap,
            _matchContent.Catalog,
            _unitLevelLabels,
            ResolvePlayerSeats(_state),
            _matchContent.Composition,
            moduleVersions);
    }

    private IReadOnlyList<MatchPlayerSeat> ResolvePlayerSeats(MatchState state)
    {
        var count = state.MoneyByPlayer.Count;
        var seats = new MatchPlayerSeat[count];
        for (var i = 0; i < count; i++)
        {
            if (_playerSeatsOverride is not null && i < _playerSeatsOverride.Count)
                seats[i] = _playerSeatsOverride[i];
            else
                seats[i] = new MatchPlayerSeat { Kind = MatchPlayerKind.Local };
        }

        return seats;
    }
}
