using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match.Hud;
using TinyTBS.Game.Presentation.Match.Overlays;
using TinyTBS.Game.Presentation.Match.World;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Builds and syncs all Gum match HUD sections.</summary>
public sealed class GameplayHudComposer
{
    private Panel? _rootPanel;
    private readonly MatchStatusBarView _statusBar = new();
    private readonly MatchBottomBarView _bottomBar = new();
    private readonly MatchCompactInfoView _compactInfo = new();
    private readonly MatchTileDetailOverlayView _tileDetail = new();
    private readonly MatchPauseOverlayView _pause = new();
    private readonly MatchGoalsOverlayView _goals = new();
    private readonly MatchShopOverlayView _shop = new();
    private readonly MatchCellActionChooserView _cellActionChooser = new();
    private readonly MatchResultOverlayView _matchResult = new();

    private bool _pauseWasVisible;
    private bool _shopWasVisible;
    private bool _cellActionChooserWasVisible;
    private bool _matchResultWasVisible;

    public void Build(
        GameplayHudViewModel hud,
        Action onEndTurn,
        Action onOpenPause,
        Action onClosePause,
        Action onOpenMinimap,
        Action onOpenGoals,
        Action onReturnToMenu,
        Action onCloseShop,
        Action<int> onBuyOffer,
        Action onCellActionMove,
        Action onCellActionBuy)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.Visual.HasEvents = false;
        _rootPanel.AddToRoot();

        _statusBar.Build(_rootPanel, hud);
        _bottomBar.Build(_rootPanel, hud, onOpenPause);
        _compactInfo.Build(_rootPanel, hud);
        _tileDetail.Build(_rootPanel, hud);
        _pause.Build(_rootPanel, onEndTurn, onOpenMinimap, onOpenGoals, onClosePause, onReturnToMenu);
        _goals.Build(_rootPanel, hud);
        _shop.Build(_rootPanel, hud, onCloseShop, onBuyOffer);
        _cellActionChooser.Build(_rootPanel, onCellActionMove, onCellActionBuy);
        _matchResult.Build(_rootPanel, onReturnToMenu);

        Sync(hud);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        _statusBar.Sync(hud);
        _bottomBar.Sync(hud);
        _compactInfo.Sync(hud);
        _tileDetail.Sync(hud);
        _goals.Sync(hud);
        _pause.Sync(hud);
        _shop.Sync(hud);
        _cellActionChooser.Sync(hud);
        _matchResult.Sync(hud);

        UpdateOverlayFocusTrap(hud);

        if (hud.IsMatchResultVisible && !_matchResultWasVisible)
            _matchResult.FocusFirst();
        else if (hud.IsPauseVisible && !_pauseWasVisible)
            _pause.FocusFirst();
        else if (hud.IsShopVisible && !_shopWasVisible)
            _shop.FocusFirstOffer();
        else if (hud.IsCellActionChooserVisible && !_cellActionChooserWasVisible)
            _cellActionChooser.FocusMove();
        else if (GameplayHudOverlayState.ShouldClearUiFocus(hud))
            ClearUiFocus();

        _matchResultWasVisible = hud.IsMatchResultVisible;
        _pauseWasVisible = hud.IsPauseVisible;
        _shopWasVisible = hud.IsShopVisible;
        _cellActionChooserWasVisible = hud.IsCellActionChooserVisible;
    }

    public void SyncDetailIcons(MatchTextureAtlas textures, MatchState match) =>
        _tileDetail.SyncIcons(textures, match);

    public void SyncShopIcons(MatchTextureAtlas textures, int currentPlayerIndex) =>
        _shop.SyncIcons(textures, currentPlayerIndex);

    public void HandleShopGamepadNavigation(IGameCommandSource commands, float elapsedSeconds) =>
        _shop.HandleGamepadNavigation(commands, elapsedSeconds);

    public void HandlePauseGamepadNavigation(IGameCommandSource commands, float elapsedSeconds) =>
        _pause.HandleGamepadNavigation(commands, elapsedSeconds);

    public void HandleCellActionChooserGamepadNavigation(IGameCommandSource commands, float elapsedSeconds) =>
        _cellActionChooser.HandleGamepadNavigation(commands, elapsedSeconds);

    public void HandleMatchResultGamepadNavigation(IGameCommandSource commands, float elapsedSeconds) =>
        _matchResult.HandleGamepadNavigation(commands, elapsedSeconds);

    public void ClearUiFocus()
    {
        _matchResult.ClearFocus();
        _pause.ClearFocus();
        _shop.ClearFocus();
        _cellActionChooser.ClearFocus();
        _bottomBar.ClearMenuFocus();
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _pauseWasVisible = false;
        _shopWasVisible = false;
        _cellActionChooserWasVisible = false;
        _matchResultWasVisible = false;
    }

    private void UpdateOverlayFocusTrap(GameplayHudViewModel hud)
    {
        var trapActive = GameplayHudOverlayState.MenuFocusTrapActive(hud);
        _bottomBar.SetMenuEnabled(!trapActive);

        if (!trapActive || _bottomBar.MenuButton is not { IsFocused: true })
            return;

        _bottomBar.ClearMenuFocus();
        if (hud.IsMatchResultVisible)
            _matchResult.FocusFirst();
        else if (hud.IsPauseVisible)
            _pause.FocusFirst();
        else if (hud.IsShopVisible)
            _shop.FocusFirstOffer();
        else if (hud.IsCellActionChooserVisible)
            _cellActionChooser.FocusMove();
    }
}
