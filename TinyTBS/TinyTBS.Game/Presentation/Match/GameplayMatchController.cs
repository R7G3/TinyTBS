using Gum;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Presentation.Match.Board;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.Input;
using TinyTBS.Rules.Match;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Saves;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Match screen logic: overlays, board input, HUD sync, draw.</summary>
public sealed class GameplayMatchController
{
    private readonly GameMain _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GameplayHudSync _hudSync;
    private readonly GameplayHudComposer _hudComposer;
    private readonly BotTurnDriver _botDriver = new();
    private readonly BoardInputController _boardInput = new();

    private GameplaySession? _session;
    private GridCell? _shopCastleCell;
    private GridCell? _cellActionChooserCell;
    private Action? _returnToMenu;
    private Action? _leaveMatch;
    private Action? _loadMatch;
    private Action? _nextChapter;
    private Action? _retryChapter;
    private readonly MatchEnemyThreatHold _enemyThreatHold = new();
    private float _lastBotFollowCursorX = float.NaN;
    private float _lastBotFollowCursorY = float.NaN;

    public GameplayMatchController(
        GameMain game,
        GraphicsDevice graphicsDevice,
        GameplayHudViewModel hud,
        GameplayHudComposer hudComposer)
    {
        _game = game;
        _graphicsDevice = graphicsDevice;
        _hudComposer = hudComposer;
        _hudSync = new GameplayHudSync(hud, hudComposer);
    }

    public void LoadContent(
        GameplaySession session,
        Action onSuspendToMenu,
        Action onLeaveMatch,
        Action onSaveMatch,
        Action onLoadMatch,
        Action? onNextChapter = null,
        Action? onRetryChapter = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        _returnToMenu = onSuspendToMenu;
        _leaveMatch = onLeaveMatch;
        _loadMatch = onLoadMatch;
        _nextChapter = onNextChapter;
        _retryChapter = onRetryChapter;
        _session = session;

        _hudSync.Hud.LevelTitle = _session.Runtime.LevelBrief.Title;
        _hudSync.SetGoalsFromLevel(_session.Runtime.LevelBrief);

        _hudComposer.Build(
            _hudSync.Hud,
            onEndTurn: () =>
            {
                if (_session?.Runtime.IsCurrentPlayerBot() == true)
                    return;
                if (_session?.Scene.IsMoveAnimating == true
                    || _session?.Scene.IsCursorAnimating == true)
                    return;
                _session?.EndTurn();
                CloseAllOverlays();
            },
            onOpenPause: OpenPause,
            onClosePause: CloseAllOverlays,
            onOpenMinimap: () => OpenOverlay(MatchOverlay.Minimap),
            onOpenGoals: () => OpenOverlay(MatchOverlay.Goals),
            onSuspendToMenu: SuspendToMenu,
            onLeaveMatch: LeaveMatch,
            onSaveMatch: SaveCurrentMatch,
            onLoadMatch: OpenLoadFromPause,
            onCloseShop: CloseTopOverlay,
            onBuyOffer: BuyShopOffer,
            onCellActionMove: OnCellActionMove,
            onCellActionBuy: OnCellActionBuy,
            onNextChapter: () => _nextChapter?.Invoke(),
            onRetryChapter: () => _retryChapter?.Invoke());

        SyncHud();
    }

    public void UnloadContent(bool disposeSession = true)
    {
        _enemyThreatHold.Clear();
        _hudComposer.Clear();
        if (disposeSession)
            _session?.Dispose();
        _session = null;
    }

    public void SaveCurrentMatch()
    {
        if (_session is null)
            return;

        try
        {
            var document = MatchSaveDocumentFactory.FromRuntime(
                _session.Runtime,
                _session.Cursor.Cell,
                _game.Files,
                _game.UserDataPaths);
            var path = new MatchSaveWriter(_game.Files, _game.UserDataPaths).Write(document);
            _hudSync.Hud.HintText = "Saved: " + Path.GetFileName(path);
            CloseAllOverlays();
        }
        catch (Exception exception)
        {
            GameLog.Error("Saving the match failed.", exception);
            _hudSync.Hud.HintText = "Save failed: " + exception.Message;
        }
    }

    private void SuspendToMenu() => _returnToMenu?.Invoke();

    private void LeaveMatch() => _leaveMatch?.Invoke();

    private void OpenLoadFromPause() => _loadMatch?.Invoke();

    public void Update(GameTime gameTime)
    {
        if (_session is null)
            return;

        var scene = _session.Scene;
        scene.TickMoveAnimation(gameTime);

        var botTurn = _session.Runtime.IsCurrentPlayerBot() && !_session.State.IsMatchOver;
        var overlaysBlockCamera = Overlays.IsAnyOpen;
        if (botTurn && !scene.IsMoveAnimating && !overlaysBlockCamera)
        {
            // Driver itself waits while the cursor is sliding to the aim cell.
            _botDriver.TryStep(_session, gameTime);
        }

        var boardInputEnabled = !overlaysBlockCamera
            && !botTurn
            && !scene.IsMoveAnimating
            && !scene.IsCursorAnimating;
        // Zoom / pan stay available on the bot's turn (watch and look around).
        var cameraControlsEnabled = !overlaysBlockCamera;

        // Zoom / pan before layout prepare so hit-tests and draws use the updated camera.
        _boardInput.ApplyZoom(
            scene.Layout,
            _game.Commands,
            _game.Pointer,
            gameTime,
            cameraControlsEnabled);
        _boardInput.ApplyCameraPan(
            _session.Cursor,
            scene.Layout,
            _game.Commands,
            _game.Pointer,
            gameTime,
            cameraControlsEnabled,
            clampCursor: boardInputEnabled);

        scene.PrepareFrame(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        if (botTurn)
            FollowBotCursorWithCamera(scene);
        else
        {
            _lastBotFollowCursorX = float.NaN;
            _lastBotFollowCursorY = float.NaN;
        }

        HandleOverlayCommands();

        if (Overlays.IsInfoOverlayOnTop && _game.Pointer.WasAnyButtonPressed)
        {
            CloseTopOverlay();
            GumService.Default.Update(gameTime);
            if (_session is null)
                return;

            scene.Update(gameTime);
            SyncHud();
            return;
        }

        var uiHeldConfirm = Overlays.IsAnyOpen;
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        GumService.Default.Update(gameTime);
        HandleOverlayGamepadNavigation(elapsedSeconds);

        if (_session is null)
            return;

        var openedCastleChooser = TryConsumeCastleUnitActionChooserOpen();
        var allowBoardConfirm = !uiHeldConfirm && !openedCastleChooser;

        _enemyThreatHold.Tick(
            _session,
            _boardInput,
            _game.Commands,
            _game.Pointer,
            elapsedSeconds,
            boardInputEnabled && !uiHeldConfirm && !openedCastleChooser,
            out var deferredThreatConfirm);

        var allowConfirm = allowBoardConfirm && !_enemyThreatHold.SuppressBoardConfirm;

        _boardInput.ApplyBoardCommands(
            _session,
            _game.Commands,
            gameTime,
            boardInputEnabled && !uiHeldConfirm,
            allowConfirm: allowConfirm);

        if (_session is null)
            return;

        if (boardInputEnabled && !uiHeldConfirm && !IsPointerOverInteractiveHud())
        {
            var pointer = _game.Pointer;
            _boardInput.ApplyPointer(_session, pointer, allowConfirm: allowConfirm);

            // Chooser opens on primary press; ApplyPointer would arm a click — drop it.
            if (openedCastleChooser)
                _boardInput.CancelPointerGesture();

            if (_boardInput.TryApplySecondaryPointer(_session.Cursor, pointer, scene.Layout))
                OpenOverlay(MatchOverlay.TileDetail);
        }
        else if (openedCastleChooser)
        {
            _boardInput.CancelPointerGesture();
        }

        if (_session is null)
            return;

        if (deferredThreatConfirm)
            _session.Confirm();

        if (allowConfirm)
            TryOpenShopFromAction();

        if (_session is null)
            return;

        scene.Update(gameTime);
        SyncHud();
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(UiColors.MatchSceneClear);

        if (_session is not null)
        {
            var match = _session.State;
            var scene = _session.Scene;
            scene.PrepareFrame(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
            scene.Draw(
                gameTime,
                afterEntities: (spriteBatch, layout) =>
                {
                    IReadOnlyList<GridCell>? moveRangeCells = null;
                    IReadOnlyList<GridCell>? attackTargetCells = null;
                    IReadOnlyList<GridCell>? raiseTargetCells = null;
                    IReadOnlyList<GridCell>? captureTargetCells = null;
                    IReadOnlyList<GridCell>? repairTargetCells = null;

                    if (_enemyThreatHold.PreviewUnitId is int threatUnitId
                        && match.TryGetUnitThreatPreview(threatUnitId, out var overlay))
                    {
                        ApplyThreatOverlayCells(
                            overlay,
                            out moveRangeCells,
                            out attackTargetCells,
                            out raiseTargetCells,
                            out captureTargetCells,
                            out repairTargetCells);
                    }
                    else if (match.TryGetSelectedUnitActionOverlay(_session.Cursor.SelectedUnitId, out overlay))
                    {
                        ApplyOverlayCells(
                            overlay,
                            out moveRangeCells,
                            out attackTargetCells,
                            out raiseTargetCells,
                            out captureTargetCells,
                            out repairTargetCells);
                    }

                    scene.GetVisualCursorCell(out var cursorCellX, out var cursorCellY);
                    MatchBoardHighlight.Draw(
                        _session.CursorHighlight,
                        spriteBatch,
                        layout,
                        cursorCellX,
                        cursorCellY,
                        hasSelection: _session.Cursor.SelectedUnitId is not null,
                        moveRangeCells,
                        attackTargetCells,
                        raiseTargetCells,
                        captureTargetCells,
                        repairTargetCells);

                    spriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        DepthStencilState.None,
                        RasterizerState.CullNone);
                    UnitCellLabels.Draw(
                        _session.CellLabels,
                        spriteBatch,
                        layout,
                        match,
                        unit => scene.GetUnitVisualTopLeft(unit));
                    spriteBatch.End();
                });
        }

        GumService.Default.Draw();

        if (_session is null)
            return;

        if (_hudSync.Hud.IsMinimapVisible)
        {
            MatchMinimapDraw.Draw(_session, _game.SharedSpriteBatch, _graphicsDevice.Viewport);
        }
    }

    private void FollowBotCursorWithCamera(MatchScene scene)
    {
        scene.GetVisualCursorCell(out var cursorX, out var cursorY);

        // Soft follow: while the bot aims / acts, keep the cursor in the central zone.
        // Between actions the spectator may pan freely until the cursor moves again.
        var cursorMoved = float.IsNaN(_lastBotFollowCursorX)
            || MathF.Abs(cursorX - _lastBotFollowCursorX) > 0.01f
            || MathF.Abs(cursorY - _lastBotFollowCursorY) > 0.01f;

        if (cursorMoved || scene.IsCursorAnimating || scene.IsMoveAnimating)
        {
            scene.Layout.KeepCellInCentralZone(cursorX, cursorY);
            _lastBotFollowCursorX = cursorX;
            _lastBotFollowCursorY = cursorY;
        }
    }

    private void SyncHud()
    {
        if (_session is null)
            return;

        _hudSync.SyncFromSession(_session, _cellActionChooserCell, _session.Runtime.CampaignChapterResult);
        if (_session.Runtime.ScriptHost.FailureMessage is { } scriptFailure)
        {
            _hudSync.Hud.HintText = "Map script disabled: " + scriptFailure;
            return;
        }

        _hudSync.Hud.HintText = _enemyThreatHold.PreviewUnitId is not null
            ? "Enemy threat · blue move · red attack · green capture · yellow repair · purple raise · release to close"
            : "WASD move · Enter/click select · Hold Enter/LMB on enemy = threat · Wheel zoom · RMB/I detail · E end · Esc pause";
    }

    private static void ApplyThreatOverlayCells(
        MatchUnitActionOverlay overlay,
        out IReadOnlyList<GridCell>? moveRangeCells,
        out IReadOnlyList<GridCell>? attackTargetCells,
        out IReadOnlyList<GridCell>? raiseTargetCells,
        out IReadOnlyList<GridCell>? captureTargetCells,
        out IReadOnlyList<GridCell>? repairTargetCells)
    {
        moveRangeCells = overlay.MoveCells;
        // Full potential attack footprint (empty tiles included), not only current targets.
        attackTargetCells = ExceptCells(overlay.AttackRangeCells, overlay.MoveCells);
        raiseTargetCells = overlay.RaiseCells;
        captureTargetCells = overlay.CaptureCells;
        repairTargetCells = overlay.RepairCells;
    }

    private static void ApplyOverlayCells(
        MatchUnitActionOverlay overlay,
        out IReadOnlyList<GridCell>? moveRangeCells,
        out IReadOnlyList<GridCell>? attackTargetCells,
        out IReadOnlyList<GridCell>? raiseTargetCells,
        out IReadOnlyList<GridCell>? captureTargetCells,
        out IReadOnlyList<GridCell>? repairTargetCells)
    {
        moveRangeCells = overlay.MoveCells;
        attackTargetCells = overlay.AttackCells;
        raiseTargetCells = overlay.RaiseCells;
        captureTargetCells = overlay.CaptureCells;
        repairTargetCells = overlay.RepairCells;
    }

    private static IReadOnlyList<GridCell> ExceptCells(
        IReadOnlyList<GridCell> source,
        IReadOnlyList<GridCell> excluded)
    {
        if (source.Count == 0 || excluded.Count == 0)
            return source;

        var exclude = excluded.ToHashSet();
        var filtered = source.Where(cell => !exclude.Contains(cell)).ToArray();
        return filtered.Length == source.Count ? source : filtered;
    }

    private static bool IsPointerOverInteractiveHud() =>
        GumService.Default.Cursor.FrameworkElementOver is Button;

    private MatchOverlayStack Overlays => _hudSync.Hud.Overlays;

    private void HandleOverlayGamepadNavigation(float elapsedSeconds)
    {
        var commands = _game.Commands;
        switch (Overlays.Top)
        {
            case MatchOverlay.MatchResult:
                _hudComposer.HandleMatchResultGamepadNavigation(commands, elapsedSeconds);
                break;
            case MatchOverlay.Shop:
                _hudComposer.HandleShopGamepadNavigation(commands, elapsedSeconds);
                break;
            case MatchOverlay.Pause:
                _hudComposer.HandlePauseGamepadNavigation(commands, elapsedSeconds);
                break;
            case MatchOverlay.CellActionChooser:
                _hudComposer.HandleCellActionChooserGamepadNavigation(commands, elapsedSeconds);
                break;
        }
    }

    private void HandleOverlayCommands()
    {
        var commands = _game.Commands;
        var top = Overlays.Top;

        if (commands.WasPressed(GameCommand.Pause))
        {
            if (top == MatchOverlay.MatchResult)
                return;

            if (top is not (MatchOverlay.None or MatchOverlay.Pause))
            {
                CloseTopOverlay();
                return;
            }

            // Esc / Start: first press clears unit selection; second toggles pause.
            if (_session is not null && _session.ClearSelection())
                return;

            if (top == MatchOverlay.Pause)
                CloseTopOverlay();
            else
                OpenPause();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm) && Overlays.IsInfoOverlayOnTop)
        {
            CloseTopOverlay();
            return;
        }

        if (top == MatchOverlay.CellActionChooser && _game.Pointer.WasSecondaryPressed)
        {
            CloseTopOverlay();
            return;
        }

        if (top != MatchOverlay.None)
        {
            if (commands.WasPressed(GameCommand.Cancel)
                || commands.WasPressed(GameCommand.Back)
                || commands.WasPressed(GameCommand.Info))
            {
                CloseTopOverlay();
            }

            return;
        }

        // Info (I / east): deselect (undo move if any) if a unit is selected; otherwise open tile detail.
        if (commands.WasPressed(GameCommand.Info))
        {
            if (_session is not null && _session.ClearSelection())
                return;

            OpenOverlay(MatchOverlay.TileDetail);
        }

        // Backspace / cancel: deselect and undo a post-move if the unit had moved.
        if (commands.WasPressed(GameCommand.Cancel))
            _session?.ClearSelection();
    }

    private void OpenOverlay(MatchOverlay overlay)
    {
        Overlays.Open(overlay);
        DropStaleOverlayContext();
        _hudComposer.ClearUiFocus();
    }

    private void CloseTopOverlay()
    {
        if (!Overlays.CloseTop())
            return;

        DropStaleOverlayContext();
        _hudComposer.ClearUiFocus();
    }

    private void CloseAllOverlays()
    {
        Overlays.CloseAll();
        DropStaleOverlayContext();
        _hudComposer.ClearUiFocus();
    }

    /// <summary>The chooser cell and shop castle only mean something while their overlay is on top.</summary>
    private void DropStaleOverlayContext()
    {
        if (Overlays.Top != MatchOverlay.CellActionChooser)
            _cellActionChooserCell = null;

        if (Overlays.Top != MatchOverlay.Shop)
        {
            _shopCastleCell = null;
            _hudSync.Hud.ShopStatusText = string.Empty;
        }
    }

    private bool TryConsumeCastleUnitActionChooserOpen()
    {
        if (_session is null || Overlays.IsAnyOpen)
            return false;

        var match = _session.State;
        var commands = _game.Commands;
        var pointer = _game.Pointer;
        var layout = _session.Scene.Layout;
        var cursorCell = _session.Cursor.Cell;

        GridCell? targetCell = null;

        if (commands.WasPressed(GameCommand.Confirm)
            && match.NeedsCastleUnitActionChooser(cursorCell, _session.Cursor.SelectedUnitId))
        {
            targetCell = cursorCell;
        }
        else if (pointer.WasPrimaryPressed
            && !IsPointerOverInteractiveHud()
            && layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
        {
            var cell = new GridCell(cellX, cellY);
            if (match.NeedsCastleUnitActionChooser(cell, _session.Cursor.SelectedUnitId))
                targetCell = cell;
        }

        if (targetCell is not { } cellToOpen)
            return false;

        OpenCellActionChooser(cellToOpen);
        return true;
    }

    private void OpenCellActionChooser(GridCell cell)
    {
        if (_session is null)
            return;

        _session.Cursor.MoveTo(cell);
        OpenOverlay(MatchOverlay.CellActionChooser);
        _cellActionChooserCell = cell;
    }

    private void OnCellActionMove()
    {
        if (_session is null || _cellActionChooserCell is not { } cell)
            return;

        _session.Cursor.MoveTo(cell);
        CloseTopOverlay();
        _session.Confirm();
    }

    private void OnCellActionBuy()
    {
        if (_cellActionChooserCell is { } cell)
            OpenShopAt(cell);
    }

    private void TryOpenShopFromAction()
    {
        if (_session is null || Overlays.IsAnyOpen)
            return;

        var confirmPressed = _game.Commands.WasPressed(GameCommand.Confirm);
        var pointerPressed = _game.Pointer.WasPrimaryPressed;
        if (!confirmPressed && !pointerPressed)
            return;

        // Primary press on HUD (e.g. bottom Menu) must not open the shop just because
        // the board cursor sits on an own recruit building.
        if (pointerPressed && IsPointerOverInteractiveHud())
            return;

        var cursorCell = _session.Cursor.Cell;
        if (_session.Cursor.SelectedUnitId is not null)
            return;
        if (_session.State.LastAction?.Kind == MatchActionKind.MoveUnit)
            return;
        if (!_session.State.IsOwnCastleAt(cursorCell))
            return;
        if (_session.State.NeedsCastleUnitActionChooser(cursorCell, _session.Cursor.SelectedUnitId))
            return;

        OpenShopAt(cursorCell);
    }

    private void OpenShopAt(GridCell castleCell)
    {
        OpenOverlay(MatchOverlay.Shop);
        _shopCastleCell = castleCell;
    }

    private void BuyShopOffer(int offerIndex)
    {
        if (_session is null || _shopCastleCell is not { } castleCell)
            return;

        if (_session.TryBuyShopOffer(offerIndex, castleCell))
            CloseTopOverlay();
        else
            _hudSync.Hud.ShopStatusText = "Cannot recruit here (gold or cell occupied).";
    }

    private void OpenPause() => OpenOverlay(MatchOverlay.Pause);
}
