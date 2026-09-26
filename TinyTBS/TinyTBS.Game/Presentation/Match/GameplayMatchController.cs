using Gum;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Match screen logic: overlays, board input, HUD sync, draw.</summary>
public sealed class GameplayMatchController
{
    private readonly GameMain _game;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GameplayHudSync _hudSync;
    private readonly GameplayHudComposer _hudComposer;

    private GameplaySession? _session;
    private GridCell? _shopCastleCell;
    private GridCell? _cellActionChooserCell;
    private bool _pendingPauseMenuFocus;
    private Action? _returnToMenu;

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

    public void LoadContent(GameplaySession session, Action returnToMenu)
    {
        ArgumentNullException.ThrowIfNull(session);

        _returnToMenu = returnToMenu;
        _session = session;

        _hudSync.Hud.LevelTitle = _session.LevelBrief.Title;
        _hudSync.SetGoalsFromLevel(_session.LevelBrief);

        _hudComposer.Build(
            _hudSync.Hud,
            onEndTurn: () =>
            {
                _session?.EndTurn();
                GameplayHudOverlayState.CloseAllExceptPause(_hudSync.Hud);
                ClosePause();
                _hudComposer.ClearUiFocus();
            },
            onOpenPause: OpenPause,
            onClosePause: ClosePause,
            onOpenMinimap: OpenMinimap,
            onOpenGoals: OpenGoals,
            onReturnToMenu: ReturnToMenu,
            onCloseShop: CloseShop,
            onBuyOffer: BuyShopOffer,
            onCellActionMove: OnCellActionMove,
            onCellActionBuy: OnCellActionBuy);

        SyncHud();
    }

    public void UnloadContent()
    {
        _hudComposer.Clear();
        _session?.Dispose();
        _session = null;
    }

    public void Update(GameTime gameTime)
    {
        if (_session is null)
            return;

        var scene = _session.Scene;
        var boardInputEnabled = !GameplayHudOverlayState.BlocksBoardInput(_hudSync.Hud);

        // Zoom / pan before layout prepare so hit-tests and draws use the updated camera.
        MatchCommandApplicator.ApplyZoom(
            scene.Layout,
            _game.Commands,
            _game.Pointer,
            gameTime,
            boardInputEnabled);
        MatchCommandApplicator.ApplyCameraPan(
            _session.State,
            scene.Layout,
            _game.Commands,
            _game.Pointer,
            gameTime,
            boardInputEnabled);

        scene.PrepareFrame(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        HandleOverlayCommands();

        if (GameplayHudOverlayState.IsDismissibleInfoOverlayVisible(_hudSync.Hud)
            && _game.Pointer.WasAnyButtonPressed)
        {
            DismissInfoOverlay();
            GumService.Default.Update(gameTime);
            if (_pendingPauseMenuFocus)
            {
                _pendingPauseMenuFocus = false;
                _hudComposer.ClearUiFocus();
            }

            if (_session is null)
                return;

            scene.Update(gameTime);
            SyncHud();
            return;
        }

        var uiHeldConfirm = GameplayHudOverlayState.CapturesGamepadConfirm(_hudSync.Hud);

        GumService.Default.Update(gameTime);

        if (_hudSync.Hud.IsShopVisible)
            _hudComposer.HandleShopGamepadNavigation(_game.Commands);

        if (_pendingPauseMenuFocus)
        {
            _pendingPauseMenuFocus = false;
            _hudComposer.ClearUiFocus();
        }

        if (_session is null)
            return;

        var allowBoardConfirm = !uiHeldConfirm && !TryConsumeCastleUnitActionChooserOpen();

        MatchCommandApplicator.Apply(
            _session,
            _game.Commands,
            scene.Layout,
            gameTime,
            boardInputEnabled && !uiHeldConfirm,
            allowConfirm: allowBoardConfirm);

        if (_session is null)
            return;

        if (boardInputEnabled && !uiHeldConfirm && !IsPointerOverInteractiveHud())
        {
            var pointer = _game.Pointer;
            var layout = scene.Layout;
            MatchCommandApplicator.ApplyPointer(
                _session,
                pointer,
                layout,
                allowConfirm: allowBoardConfirm);

            if (MatchCommandApplicator.TryApplySecondaryPointer(_session.State, pointer, layout))
                OpenTileDetail();
        }

        if (_session is null)
            return;

        if (allowBoardConfirm)
            TryOpenShopFromAction();

        if (_session is null)
            return;

        scene.Update(gameTime);
        SyncHud();
    }

    public void Draw(GameTime gameTime)
    {
        _graphicsDevice.Clear(MatchUiColors.SceneClear);

        if (_session is not null)
        {
            var match = _session.State;
            var scene = _session.Scene;
            scene.PrepareFrame(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
            scene.Draw(
                gameTime,
                afterEntities: (spriteBatch, layout) =>
                    _session.CursorHighlight.Draw(
                        spriteBatch,
                        layout,
                        match.Cursor.X,
                        match.Cursor.Y,
                        hasSelection: match.SelectedUnitId is not null,
                        manageBatch: false));
        }

        GumService.Default.Draw();

        if (_session is null)
            return;

        if (_hudSync.Hud.IsMinimapVisible)
        {
            MatchMinimapDraw.Draw(_session, _game.SharedSpriteBatch, _graphicsDevice.Viewport);
        }
    }

    private void SyncHud()
    {
        if (_session is null)
            return;

        _hudSync.SyncFromSession(_session, _cellActionChooserCell);
    }

    private void DismissInfoOverlay()
    {
        if (_hudSync.Hud.IsTileDetailVisible)
        {
            CloseTileDetail();
            return;
        }

        if (_hudSync.Hud.IsGoalsVisible)
        {
            CloseGoals();
            return;
        }

        if (_hudSync.Hud.IsMinimapVisible)
            CloseMinimap();
    }

    private static bool IsPointerOverInteractiveHud() =>
        GumService.Default.Cursor.FrameworkElementOver is Button;

    private void HandleOverlayCommands()
    {
        var commands = _game.Commands;
        var hud = _hudSync.Hud;

        if (commands.WasPressed(GameCommand.Pause))
        {
            if (hud.IsShopVisible
                || hud.IsGoalsVisible
                || hud.IsMinimapVisible
                || hud.IsTileDetailVisible
                || hud.IsCellActionChooserVisible)
            {
                TryGoBackFromOverlay();
                return;
            }

            // Esc / Start: first press clears unit selection; second opens pause.
            if (_session is not null && _session.State.ClearSelection())
                return;

            if (hud.IsPauseVisible)
                ClosePause();
            else
                OpenPause();
            return;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            if (hud.IsMinimapVisible)
            {
                CloseMinimap(deferFocusClearUntilAfterGum: true);
                return;
            }

            if (hud.IsGoalsVisible)
            {
                CloseGoals(deferFocusClearUntilAfterGum: true);
                return;
            }

            if (hud.IsTileDetailVisible)
            {
                CloseTileDetail(deferFocusClearUntilAfterGum: true);
                return;
            }
        }

        var wantsBack = commands.WasPressed(GameCommand.Cancel)
            || commands.WasPressed(GameCommand.Back)
            || commands.WasPressed(GameCommand.Info);

        if (hud.IsCellActionChooserVisible && _game.Pointer.WasSecondaryPressed)
        {
            CloseCellActionChooser();
            return;
        }

        if (wantsBack && TryGoBackFromOverlay())
            return;

        // Info (I / east): deselect if a unit is selected; otherwise open tile detail.
        if (commands.WasPressed(GameCommand.Info)
            && !GameplayHudOverlayState.BlocksBoardInput(hud))
        {
            if (_session is not null && _session.State.ClearSelection())
                return;

            OpenTileDetail();
        }

        // Backspace cancel on the board: deselect only.
        if (commands.WasPressed(GameCommand.Cancel)
            && !GameplayHudOverlayState.BlocksBoardInput(hud))
        {
            _session?.State.ClearSelection();
        }
    }

    private bool TryGoBackFromOverlay()
    {
        var hud = _hudSync.Hud;

        if (hud.IsCellActionChooserVisible)
        {
            CloseCellActionChooser();
            return true;
        }

        if (hud.IsShopVisible)
        {
            CloseShop();
            return true;
        }

        if (hud.IsGoalsVisible)
        {
            CloseGoals();
            return true;
        }

        if (hud.IsMinimapVisible)
        {
            CloseMinimap();
            return true;
        }

        if (hud.IsTileDetailVisible)
        {
            CloseTileDetail();
            return true;
        }

        if (hud.IsPauseVisible)
        {
            ClosePause();
            return true;
        }

        return false;
    }

    private void CloseMinimap(bool deferFocusClearUntilAfterGum = false)
    {
        _hudSync.Hud.IsMinimapVisible = false;
        _hudComposer.ClearUiFocus();
        if (deferFocusClearUntilAfterGum)
            _pendingPauseMenuFocus = false;
    }

    private void CloseGoals(bool deferFocusClearUntilAfterGum = false)
    {
        _hudSync.Hud.IsGoalsVisible = false;
        _hudComposer.ClearUiFocus();
        if (deferFocusClearUntilAfterGum)
            _pendingPauseMenuFocus = false;
    }

    private void CloseTileDetail(bool deferFocusClearUntilAfterGum = false)
    {
        _hudSync.Hud.IsTileDetailVisible = false;
        _hudComposer.ClearUiFocus();
        if (deferFocusClearUntilAfterGum)
            _pendingPauseMenuFocus = false;
    }

    private void OpenTileDetail()
    {
        GameplayHudOverlayState.PrepareForTileDetail(_hudSync.Hud);
        _cellActionChooserCell = null;
        _hudComposer.ClearUiFocus();
    }

    private bool TryConsumeCastleUnitActionChooserOpen()
    {
        if (_session is null || GameplayHudOverlayState.BlocksBoardInput(_hudSync.Hud))
            return false;

        var match = _session.State;
        var commands = _game.Commands;
        var pointer = _game.Pointer;
        var layout = _session.Scene.Layout;

        GridCell? targetCell = null;

        if (commands.WasPressed(GameCommand.Confirm)
            && match.NeedsCastleUnitActionChooser(match.Cursor))
        {
            targetCell = match.Cursor;
        }
        else if (pointer.WasPrimaryPressed
            && !IsPointerOverInteractiveHud()
            && layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
        {
            var cell = new GridCell(cellX, cellY);
            if (match.NeedsCastleUnitActionChooser(cell))
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

        _session.State.HandlePointer(cell);
        _cellActionChooserCell = cell;
        GameplayHudOverlayState.PrepareForCellActionChooser(_hudSync.Hud);
    }

    private void CloseCellActionChooser()
    {
        _hudSync.Hud.IsCellActionChooserVisible = false;
        _cellActionChooserCell = null;
        _hudComposer.ClearUiFocus();
    }

    private void OnCellActionMove()
    {
        if (_session is null || _cellActionChooserCell is not { } cell)
            return;

        _session.State.HandlePointer(cell);
        CloseCellActionChooser();
        _session.Confirm();
    }

    private void OnCellActionBuy()
    {
        if (_cellActionChooserCell is not { } cell)
            return;

        CloseCellActionChooser();
        OpenShopAt(cell);
    }

    private void TryOpenShopFromAction()
    {
        if (_session is null || GameplayHudOverlayState.BlocksBoardInput(_hudSync.Hud))
            return;
        if (_hudSync.Hud.IsCellActionChooserVisible)
            return;

        var actionPressed = _game.Commands.WasPressed(GameCommand.Confirm)
            || _game.Pointer.WasPrimaryPressed;
        if (!actionPressed)
            return;
        if (_session.State.SelectedUnitId is not null)
            return;
        if (_session.State.LastAction?.Kind == MatchPlayerActionKind.MoveUnit)
            return;
        if (!_session.State.IsOwnCastleAt(_session.State.Cursor))
            return;
        if (_session.State.NeedsCastleUnitActionChooser(_session.State.Cursor))
            return;

        OpenShopAt(_session.State.Cursor);
    }

    private void OpenShopAt(GridCell castleCell)
    {
        _shopCastleCell = castleCell;
        GameplayHudOverlayState.PrepareForShop(_hudSync.Hud);
        _cellActionChooserCell = null;
    }

    private void BuyShopOffer(int offerIndex)
    {
        if (_session is null || _shopCastleCell is not { } castleCell)
            return;

        if (_session.TryBuyShopOffer(offerIndex, castleCell))
            CloseShop();
        else
            _hudSync.Hud.ShopStatusText = "Cannot recruit here (gold or cell occupied).";
    }

    private void OpenPause()
    {
        GameplayHudOverlayState.PrepareForPause(_hudSync.Hud);
        CloseCellActionChooser();
    }

    private void ClosePause()
    {
        GameplayHudOverlayState.ClosePauseAndInfoOverlays(_hudSync.Hud);
        _hudComposer.ClearUiFocus();
    }

    private void OpenMinimap()
    {
        GameplayHudOverlayState.PrepareForMinimap(_hudSync.Hud);
        _hudComposer.ClearUiFocus();
    }

    private void OpenGoals()
    {
        GameplayHudOverlayState.PrepareForGoals(_hudSync.Hud);
        _hudComposer.ClearUiFocus();
    }

    private void CloseShop()
    {
        _hudSync.Hud.IsShopVisible = false;
        _shopCastleCell = null;
        _hudSync.Hud.ShopStatusText = string.Empty;
        _hudComposer.ClearUiFocus();
    }

    private void ReturnToMenu() => _returnToMenu?.Invoke();
}
