using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Levels;
using TinyTBS.Rules.Levels.Models;
using TinyTBS.Rules.Ai;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.Presentation.Match.Board;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Scripting;

namespace TinyTBS.Game.Match.Loading;

/// <summary>
/// Loads a match in discrete stages: content, level, rules, script, then textures and scene.
/// Call <see cref="AnnounceNextStage"/>, redraw, then <see cref="RunAnnouncedStage"/> so the loading
/// screen can show the label before the heavy work. The result is a <see cref="GameplaySession"/>.
/// </summary>
public sealed class MatchSessionLoadPipeline
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteBatch _spriteBatch;
    private readonly IAssetResolver _assets;
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;
    private readonly MatchStartRequest _request;
    private readonly IReadOnlyList<LoadStage> _stages;

    private MatchContentLoadResult? _matchContent;
    private LevelDefinition? _level;
    private MatchState? _state;
    private int _unitCap;
    private MatchRuntime? _runtime;
    private MatchCursor? _cursor;
    private MatchTextureAtlas? _textures;
    private MatchScene? _scene;
    private CursorHighlightRenderer? _cursorHighlight;
    private MinimapRenderer? _minimap;
    private CellLabelRenderer? _cellLabels;
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
        IFileSystem files,
        IUserDataPaths userDataPaths,
        MatchStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LevelId);

        _graphicsDevice = graphicsDevice;
        _content = content;
        _spriteBatch = spriteBatch;
        _assets = assets;
        _files = files;
        _userDataPaths = userDataPaths;
        _request = request;

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

    private void LoadMatchContentComposition()
    {
        var locator = new ContentModuleLocator(_files, _userDataPaths);
        var composition = _request.Composition;
        if (composition is null)
        {
            var scenarioModuleId = string.IsNullOrWhiteSpace(_request.ScenarioModuleId)
                ? VanillaContentIds.ScenarioModuleId
                : _request.ScenarioModuleId;
            var scenario = ScenarioModuleLoader.Load(locator.ResolveModuleRoot(scenarioModuleId), _files);
            composition = MatchContentComposition.FromScenarioDefaults(scenario);
        }

        _matchContent = MatchContentCompositionLoader.Load(composition, locator, _files);
    }

    private void LoadLevelAndMap()
    {
        ArgumentNullException.ThrowIfNull(_matchContent);
        _level = LevelFolderLoader.LoadFromModuleLevels(
            _matchContent.Scenario.ModuleRootPath,
            _request.LevelId,
            _files);
        MatchContentCompositionLoader.ValidateMapTypes(
            _level.Map,
            _matchContent.Catalog,
            _matchContent.Replaces);
    }

    private void PrepareMatchState()
    {
        ArgumentNullException.ThrowIfNull(_level);
        ArgumentNullException.ThrowIfNull(_matchContent);

        var snapshot = _request.ResumeSnapshot;
        var playerCount = snapshot?.PlayerCount
            ?? Math.Clamp(
                _request.PlayerCount ?? _level.Players.DefaultSlots,
                _level.Players.Min,
                _level.Players.Max);
        _unitCap = snapshot?.UnitCap ?? _request.UnitCap ?? _level.DefaultUnitCap;

        _state = MatchState.FromMap(
            _level.Map,
            _matchContent.Catalog,
            _matchContent.Replaces,
            playerCount: playerCount,
            startingGold: _request.StartingGold ?? _level.DefaultStartingGold,
            unitCap: _unitCap,
            victoryType: _level.Victory.Type,
            defeatType: _level.Defeat.Type);

        _cursor = MatchCursor.AtBoardCentre(_state.Width, _state.Height);
        if (snapshot is null)
            return;

        _state.HydrateFromSnapshot(snapshot);
        _cursor.MoveTo(new GridCell(snapshot.Cursor.X, snapshot.Cursor.Y));
        _cursor.SelectedUnitId = _state.PendingActivationUnitId();
    }

    private void CompileMapScript()
    {
        ArgumentNullException.ThrowIfNull(_state);
        ArgumentNullException.ThrowIfNull(_level);
        ArgumentNullException.ThrowIfNull(_matchContent);

        var scriptHost = MapScriptHost.LoadForMap(
            _level.Map.ScriptPath,
            _files,
            new RoslynMapScriptEngine());

        _runtime = new MatchRuntime(
            _state,
            scriptHost,
            CreateLevelBrief(_level),
            ResolvePlayerSeats(_state),
            _matchContent.Composition,
            MatchSaveDocumentFactory.CollectModuleVersions(_matchContent.Composition, _files, _userDataPaths));

        // Resumed saves already contain the effects of earlier turn-start hooks.
        if (!_request.IsResume)
            _runtime.StartFreshMatch();
    }

    private void LoadTextures()
    {
        ArgumentNullException.ThrowIfNull(_matchContent);
        _textures = MatchTextureAtlas.Load(
            _graphicsDevice,
            _content,
            _files,
            _assets,
            _matchContent.Catalog,
            onItemLoaded: () => UiPump?.Invoke());
    }

    private void BuildScene()
    {
        ArgumentNullException.ThrowIfNull(_state);
        ArgumentNullException.ThrowIfNull(_cursor);
        ArgumentNullException.ThrowIfNull(_textures);

        _scene = new MatchScene(_state, _cursor, _graphicsDevice, _spriteBatch, _textures);
        _cursorHighlight = new CursorHighlightRenderer(_graphicsDevice);
        _minimap = new MinimapRenderer(_graphicsDevice);
        _cellLabels = new CellLabelRenderer(_content.Load<SpriteFont>("Fonts/MatchCell"));
    }

    private GameplaySession AssembleSession()
    {
        ArgumentNullException.ThrowIfNull(_runtime);
        ArgumentNullException.ThrowIfNull(_cursor);
        ArgumentNullException.ThrowIfNull(_scene);
        ArgumentNullException.ThrowIfNull(_cursorHighlight);
        ArgumentNullException.ThrowIfNull(_textures);
        ArgumentNullException.ThrowIfNull(_minimap);
        ArgumentNullException.ThrowIfNull(_cellLabels);

        return new GameplaySession(
            _runtime,
            _cursor,
            _scene,
            _cursorHighlight,
            _textures,
            _minimap,
            _cellLabels);
    }

    private MatchLevelBrief CreateLevelBrief(LevelDefinition level) =>
        new()
        {
            LevelId = level.Id,
            Title = level.Title,
            Description = level.Description,
            VictoryType = level.Victory.Type,
            DefeatType = level.Defeat.Type,
            TeamDefeatMode = level.TeamDefeatMode,
            UnitCap = _unitCap,
        };

    private IReadOnlyList<MatchPlayerSeat> ResolvePlayerSeats(MatchState state)
    {
        var requestedSeats = _request.PlayerSeats;
        var count = state.PlayerCount;
        var seats = new MatchPlayerSeat[count];
        for (var i = 0; i < count; i++)
        {
            if (requestedSeats is not null && i < requestedSeats.Count)
                seats[i] = requestedSeats[i];
            else
                seats[i] = new MatchPlayerSeat { Kind = MatchPlayerKind.Local };
        }

        return seats;
    }
}
