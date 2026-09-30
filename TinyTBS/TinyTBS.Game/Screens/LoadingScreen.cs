using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Presentation.Loading;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Loads a match session one pipeline stage per frame and shows progress, then opens gameplay.
/// </summary>
public sealed class LoadingScreen : MenuScreen
{
    private enum LoadPhase
    {
        Warmup,
        Announce,
        Run,
        Done,
        Failed,
    }

    private readonly MatchStartRequest _request;
    private readonly LoadingViewModel _viewModel = new();
    private readonly LoadingView _view = new();

    private MatchSessionLoadPipeline? _pipeline;
    private LoadPhase _phase = LoadPhase.Warmup;
    private int _warmupFrames;
    private bool _restoreFixedTimeStep;
    private bool _previousFixedTimeStep;
    private Task? _backgroundStageTask;
    private long _lastUiPumpTimestamp;

    public LoadingScreen(GameMain game, MatchStartRequest request)
        : base(game)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LevelId);
        _request = request;
    }

    protected override void OnLoad()
    {
        // Fixed timestep drops Draw while Update runs long load stages — background would freeze.
        _previousFixedTimeStep = TinyGame.IsFixedTimeStep;
        TinyGame.IsFixedTimeStep = false;
        _restoreFixedTimeStep = true;

        _viewModel.Title = _request.IsResume ? "Loading save" : "Loading match";
        _viewModel.Note = _request.LoadingNote ?? string.Empty;
        _viewModel.ProgressFraction = 0f;
        _view.Build(_viewModel);

        _pipeline = new MatchSessionLoadPipeline(
            GraphicsDevice,
            Content,
            TinyGame.SharedSpriteBatch,
            Assets,
            TinyGame.Files,
            TinyGame.UserDataPaths,
            _request);

        ApplyProgress(_pipeline.Progress);
        _phase = LoadPhase.Warmup;
        _warmupFrames = 0;
    }

    protected override void OnUnload()
    {
        if (_restoreFixedTimeStep)
        {
            TinyGame.IsFixedTimeStep = _previousFixedTimeStep;
            _restoreFixedTimeStep = false;
        }

        _view.Clear();
        _pipeline = null;
    }

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        if (_pipeline is null || _phase is LoadPhase.Done or LoadPhase.Failed)
            return;

        try
        {
            if (_backgroundStageTask is not null)
            {
                if (!_backgroundStageTask.IsCompleted)
                    return;

                var task = _backgroundStageTask;
                _backgroundStageTask = null;
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? new InvalidOperationException("Load stage failed.");
                task.GetAwaiter().GetResult();
                OnStageFinished();
                return;
            }

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
                    if (_pipeline.AnnouncedStageRequiresMainThread)
                    {
                        _pipeline.UiPump = PumpLoadingVisuals;
                        try
                        {
                            _pipeline.RunAnnouncedStage();
                        }
                        finally
                        {
                            _pipeline.UiPump = null;
                        }

                        OnStageFinished();
                    }
                    else
                    {
                        // Keep Draw pumping the flying background while CPU/IO stages run.
                        var pipeline = _pipeline;
                        _backgroundStageTask = Task.Run(pipeline.RunAnnouncedStage);
                    }

                    break;
            }
        }
        catch (Exception exception)
        {
            GameLog.Error($"Match load failed (level '{_request.LevelId}').", exception);
            _backgroundStageTask = null;
            _phase = LoadPhase.Failed;
            _viewModel.StageLabel = $"Failed: {exception.Message}";
            _view.Sync(_viewModel);
        }
    }

    private void OnStageFinished()
    {
        if (_pipeline is null)
            return;

        ApplyProgress(_pipeline.Progress);
        _phase = _pipeline.IsComplete ? LoadPhase.Done : LoadPhase.Announce;
        if (_phase == LoadPhase.Done)
            FinishWithSession();
    }

    /// <summary>
    /// Mid-stage redraw so the tiled background keeps drifting while textures load on the game thread.
    /// </summary>
    private void PumpLoadingVisuals()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var elapsed = (now - _lastUiPumpTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency;
        if (_lastUiPumpTimestamp != 0 && elapsed < 1.0 / 30.0)
            return;

        _lastUiPumpTimestamp = now;
        DrawBackdrop(new GameTime());
        GraphicsDevice.Present();
    }

    private void FinishWithSession()
    {
        var session = _pipeline?.Result
            ?? throw new InvalidOperationException("Match load finished without a session.");

        if (_request.CampaignRun is { } campaignRun)
        {
            campaignRun.Composition ??= session.Runtime.Composition;
            campaignRun.PlayerSeats ??= session.Runtime.PlayerSeats;
            campaignRun.UnitCap ??= session.State.UnitCap;
            session.Runtime.CampaignRun = campaignRun;

            try
            {
                App.Campaigns.NotifyMatchOpened(campaignRun);
            }
            catch (Exception exception)
            {
                // Chapter start hooks are best-effort; the match still opens.
                App.Campaigns.LogMatchOpenFailure(exception);
            }
        }

        _phase = LoadPhase.Done;
        TinyGame.ClearSuspendedMatch(dispose: true);
        Navigator.ShowMatch(session);
    }

    private void ApplyProgress(MatchLoadProgress progress)
    {
        _viewModel.StageLabel = progress.StageLabel;
        _viewModel.ProgressFraction = progress.Fraction;
        _view.Sync(_viewModel);
    }
}
