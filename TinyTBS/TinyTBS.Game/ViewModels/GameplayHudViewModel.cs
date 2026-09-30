using Microsoft.Xna.Framework;

namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state bag for gameplay HUD (not an MVVM architecture layer).</summary>
public sealed class GameplayHudViewModel
{
    public string PlayerLabel { get; set; } = "Player 1 (local)";

    public string GoldText { get; set; } = "0g";

    public string IncomeText { get; set; } = "Income: 0g";

    public string UnitsText { get; set; } = "0/0";

    public string TurnText { get; set; } = "Turn 1";

    public Color StatusBarColor { get; set; } = Color.CornflowerBlue;

    public string HintText { get; set; } =
        "WASD move · Enter/click select · Hold Enter/LMB on enemy = move+attack threat · Wheel zoom · RMB/I detail · E end · Esc pause";

    /// <summary>Always-on corner summary for the cursor tile.</summary>
    public string CompactInfoText { get; set; } = string.Empty;

    public string DetailTerrainText { get; set; } = string.Empty;

    public string DetailBuildingText { get; set; } = string.Empty;

    public string DetailUnitText { get; set; } = string.Empty;

    public bool HasDetailBuilding { get; set; }

    public bool HasDetailUnit { get; set; }

    /// <summary>True when the compact widget should sit on the left (cursor is on the right half).</summary>
    public bool InfoPreferLeft { get; set; }

    /// <summary>True when the compact widget should sit near the top (cursor is on the bottom half).</summary>
    public bool InfoPreferTop { get; set; }

    public MatchOverlayStack Overlays { get; } = new();

    /// <summary>Detailed tile inspection overlay (like map/goals).</summary>
    public bool IsTileDetailVisible => Overlays.Top == MatchOverlay.TileDetail;

    public bool IsPauseVisible => Overlays.Top == MatchOverlay.Pause;

    public bool IsMinimapVisible => Overlays.Top == MatchOverlay.Minimap;

    public bool IsGoalsVisible => Overlays.Top == MatchOverlay.Goals;

    public bool IsShopVisible => Overlays.Top == MatchOverlay.Shop;

    /// <summary>Match ended (standard or script victory) — blocks board and other overlays.</summary>
    public bool IsMatchResultVisible => Overlays.Top == MatchOverlay.MatchResult;

    public string MatchResultText { get; set; } = string.Empty;

    /// <summary>Campaign: show Next chapter after a win with a remaining chapter.</summary>
    public bool ShowMatchResultNextChapter { get; set; }

    /// <summary>Campaign: show Retry after a loss (or win without next — unused).</summary>
    public bool ShowMatchResultRetry { get; set; }

    /// <summary>Compact Move / Buy chooser above an occupied own castle.</summary>
    public bool IsCellActionChooserVisible => Overlays.Top == MatchOverlay.CellActionChooser;

    /// <summary>Screen X of the cell top-center (chooser anchors just above).</summary>
    public float CellActionChooserAnchorX { get; set; }

    /// <summary>Screen Y of the cell top edge.</summary>
    public float CellActionChooserAnchorY { get; set; }

    public string GoalsText { get; set; } = string.Empty;

    public string LevelTitle { get; set; } = string.Empty;

    public string ShopStatusText { get; set; } = string.Empty;

    public IReadOnlyList<GameplayShopOfferViewModel> ShopOffers { get; set; } = [];
}
