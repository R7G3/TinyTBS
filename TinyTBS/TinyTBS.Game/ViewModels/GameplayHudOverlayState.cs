namespace TinyTBS.Game.ViewModels;

/// <summary>Single place for “which overlay is open” rules used by input and HUD focus.</summary>
public static class GameplayHudOverlayState
{
    public static bool BlocksBoardInput(GameplayHudViewModel hud) =>
        hud.IsMatchResultVisible
        || hud.IsPauseVisible
        || hud.IsShopVisible
        || hud.IsGoalsVisible
        || hud.IsMinimapVisible
        || hud.IsTileDetailVisible
        || hud.IsCellActionChooserVisible;

    public static bool CapturesGamepadConfirm(GameplayHudViewModel hud) =>
        BlocksBoardInput(hud);

    public static bool IsDismissibleInfoOverlayVisible(GameplayHudViewModel hud) =>
        !hud.IsMatchResultVisible
        && (hud.IsMinimapVisible || hud.IsGoalsVisible || hud.IsTileDetailVisible);

    public static bool MenuFocusTrapActive(GameplayHudViewModel hud) =>
        hud.IsMatchResultVisible
        || hud.IsPauseVisible
        || hud.IsShopVisible
        || hud.IsCellActionChooserVisible
        || hud.IsGoalsVisible
        || hud.IsMinimapVisible
        || hud.IsTileDetailVisible;

    public static bool ShouldClearUiFocus(GameplayHudViewModel hud) =>
        !hud.IsMatchResultVisible
        && !hud.IsPauseVisible
        && !hud.IsShopVisible
        && !hud.IsGoalsVisible
        && !hud.IsTileDetailVisible
        && !hud.IsMinimapVisible
        && !hud.IsCellActionChooserVisible;

    public static void CloseAllExceptPause(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsShopVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsGoalsVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void ClosePauseAndInfoOverlays(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsPauseVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsGoalsVisible = false;
    }

    public static void PrepareForMatchResult(GameplayHudViewModel hud)
    {
        hud.IsMatchResultVisible = true;
        hud.IsPauseVisible = false;
        hud.IsShopVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsGoalsVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void PrepareForTileDetail(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsTileDetailVisible = true;
        hud.IsPauseVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsGoalsVisible = false;
        hud.IsShopVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void PrepareForCellActionChooser(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsCellActionChooserVisible = true;
        hud.IsPauseVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsGoalsVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsShopVisible = false;
    }

    public static void PrepareForShop(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsShopVisible = true;
        hud.IsPauseVisible = false;
        hud.IsMinimapVisible = false;
        hud.IsGoalsVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void PrepareForPause(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsPauseVisible = true;
        hud.IsShopVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void PrepareForMinimap(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsMinimapVisible = true;
        hud.IsGoalsVisible = false;
        hud.IsPauseVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsCellActionChooserVisible = false;
    }

    public static void PrepareForGoals(GameplayHudViewModel hud)
    {
        if (hud.IsMatchResultVisible)
            return;

        hud.IsGoalsVisible = true;
        hud.IsMinimapVisible = false;
        hud.IsPauseVisible = false;
        hud.IsTileDetailVisible = false;
        hud.IsCellActionChooserVisible = false;
    }
}
