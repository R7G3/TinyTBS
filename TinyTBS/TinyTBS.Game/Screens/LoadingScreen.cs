using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Presentation.Loading;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Loads a match session one pipeline stage per frame and shows progress, then opens gameplay.
/// </summary>
public sealed class LoadingScreen : GameScreen
{
    private enum LoadPhase
    {
        Warmup,
        Announce,
        Run,
        Done,
        Failed,
    }

    private readonly IAssetResolver _assets;
    private readonly string _levelId;
    private readonly string? _bundleId;
    private readonly string? _scenarioModuleId;
    private readonly MatchContentComposition? _composition;
    private readonly int? _playerCount;
    private readonly int? _startingGold;
    private readonly int? _unitCap;
    private readonly IReadOnlyList<TinyTBS.Game.Ai.MatchPlayerSeat>? _playerSeats;
    private readonly ContinueMatchRequest? _continueRequest;
    private readonly CampaignRunState? _campaignRun;
    private readonly LoadingViewModel _viewModel = new();
    private readonly LoadingView _view = new();

    private MainMenuBackground? _background;
    private MatchSessionLoadPipeline? _pipeline;
    private LoadPhase _phase = LoadPhase.Warmup;
    private int _warmupFrames;

    public LoadingScreen(
        GameMain game,
        IAssetResolver assets,
        string levelId,
        string? bundleId = null)
        : this(
            game,
            assets,
            levelId,
            bundleId,
            scenarioModuleId: null,
            composition: null,
            playerCount: null,
            startingGold: null,
            unitCap: null,
            playerSeats: null,
            continueRequest: null,
            campaignRun: null)
    {
    }

    public LoadingScreen(GameMain game, IAssetResolver assets, NewGameStartRequest request)
        : this(
            game,
            assets,
            request.LevelId,
            bundleId: null,
            scenarioModuleId: request.ScenarioModuleId,
            composition: request.Composition,
            playerCount: request.PlayerCount,
            startingGold: request.StartingGold,
            unitCap: request.UnitCap,
            playerSeats: request.PlayerSeats,
            continueRequest: null,
            campaignRun: request.CampaignRun)
    {
        ArgumentNullException.ThrowIfNull(request);
    }

    public LoadingScreen(GameMain game, IAssetResolver assets, ContinueMatchRequest request)
        : this(
            game,
            assets,
            request.LevelId,
            bundleId: null,
            scenarioModuleId: request.ScenarioModuleId,
            composition: request.Composition,
            playerCount: request.PlayerCount,
            startingGold: null,
            unitCap: request.UnitCap,
            playerSeats: request.PlayerSeats,
            continueRequest: request,
            campaignRun: request.CampaignRun)
    {
        ArgumentNullException.ThrowIfNull(request);
    }

    private LoadingScreen(
        GameMain game,
        IAssetResolver assets,
        string levelId,
        string? bundleId,
        string? scenarioModuleId,
        MatchContentComposition? composition,
        int? playerCount,
        int? startingGold,
        int? unitCap,
        IReadOnlyList<TinyTBS.Game.Ai.MatchPlayerSeat>? playerSeats,
        ContinueMatchRequest? continueRequest,
        CampaignRunState? campaignRun)
        : base(game)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        _assets = assets;
        _levelId = levelId.Trim();
        _bundleId = string.IsNullOrWhiteSpace(bundleId) ? null : bundleId.Trim();
        _scenarioModuleId = string.IsNullOrWhiteSpace(scenarioModuleId) ? null : scenarioModuleId.Trim();
        _composition = composition;
        _playerCount = playerCount;
        _startingGold = startingGold;
        _unitCap = unitCap;
        _playerSeats = playerSeats;
        _continueRequest = continueRequest;
        _campaignRun = campaignRun;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();

        _viewModel.Title = _continueRequest is null ? "Loading match" : "Loading save";
        _viewModel.StageLabel = string.IsNullOrWhiteSpace(_continueRequest?.VersionWarning)
            ? "Preparing…"
            : _continueRequest.VersionWarning!;
        _viewModel.ProgressFraction = 0f;
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        _view.Build(_viewModel);

        var composition = _composition;
        var scenarioModuleId = _scenarioModuleId;
        if (_bundleId is not null)
        {
            composition = GameplaySessionFactory.LoadCompositionFromBundle(
                _bundleId,
                TinyGame.Files,
                TinyGame.UserDataPaths);
            scenarioModuleId = composition.ScenarioModuleId;
        }

        _pipeline = new MatchSessionLoadPipeline(
            GraphicsDevice,
            Content,
            TinyGame.SharedSpriteBatch,
            _assets,
            TinyGame.Files,
            TinyGame.UserDataPaths,
            _levelId,
            scenarioModuleId,
            composition,
            _playerCount,
            _startingGold,
            _unitCap,
            _playerSeats,
            _continueRequest?.RuntimeSnapshot);

        ApplyProgress(_pipeline.Progress);
        _phase = LoadPhase.Warmup;
        _warmupFrames = 0;
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _pipeline = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);

        if (_pipeline is null || _phase is LoadPhase.Done or LoadPhase.Failed)
            return;

        try
        {
            switch (_phase)
            {
                case LoadPhase.Warmup:
                    _warmupFrames++;
                    if (_warmupFrames >= 2)
                        _phase = LoadPhase.Announce;
                    break;

                case LoadPhase.Announce:
                    if (!_pipeline.HasPendingStages)
                    {
                        FinishWithSession();
                        break;
                    }

                    _pipeline.AnnounceNextStage();
                    ApplyProgress(_pipeline.Progress);
                    _phase = LoadPhase.Run;
                    break;

                case LoadPhase.Run:
                    _pipeline.RunAnnouncedStage();
                    ApplyProgress(_pipeline.Progress);
                    _phase = _pipeline.IsComplete ? LoadPhase.Done : LoadPhase.Announce;
                    if (_phase == LoadPhase.Done)
                        FinishWithSession();
                    break;
            }
        }
        catch (Exception exception)
        {
            _phase = LoadPhase.Failed;
            _viewModel.StageLabel = $"Failed: {exception.Message}";
            _view.Sync(_viewModel);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));

        var texture = _background?.Texture;
        if (texture is not null)
        {
            ViewportFit.DrawCentered(
                TinyGame.SharedSpriteBatch,
                texture,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                Color.White * 0.35f);
        }

        GumService.Default.Draw();
    }

    private void FinishWithSession()
    {
        var session = _pipeline?.Result
            ?? throw new InvalidOperationException("Match load finished without a session.");

        if (_campaignRun is not null)
        {
            _campaignRun.Composition ??= session.Composition;
            _campaignRun.PlayerSeats ??= session.PlayerSeats;
            _campaignRun.UnitCap ??= session.State.UnitCap;
            session.CampaignRun = _campaignRun;

            try
            {
                var service = new CampaignProgressService(TinyGame.UserDataPaths, TinyGame.Files);
                var campaign = service.TryLoadDefinition(_campaignRun.ScenarioModuleId);
                if (campaign is not null)
                    service.NotifyChapterStarted(_campaignRun, campaign);
                else
                    service.Persist(_campaignRun);
            }
            catch (Exception)
            {
                // Chapter start hooks are best-effort; match still opens.
            }
        }

        _phase = LoadPhase.Done;
        TinyGame.ClearSuspendedMatch(dispose: true);
        ScreenManager.ReplaceScreen(new GameplayScreen(TinyGame, _assets, session));
    }

    private void ApplyProgress(MatchLoadProgress progress)
    {
        _viewModel.StageLabel = progress.StageLabel;
        _viewModel.ProgressFraction = progress.Fraction;
        _view.Sync(_viewModel);
    }
}
