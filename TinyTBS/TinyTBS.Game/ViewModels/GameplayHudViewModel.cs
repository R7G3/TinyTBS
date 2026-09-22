using Microsoft.Xna.Framework;

namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state bag for gameplay HUD (not an MVVM architecture layer).</summary>
public sealed class GameplayHudViewModel
{
    public string PlayerLabel { get; set; } = "P1";

    public string GoldText { get; set; } = "0g";

    public string TurnText { get; set; } = "Turn 1";

    public Color StatusBarColor { get; set; } = Color.CornflowerBlue;

    public string HintText { get; set; } =
        "WASD move · Enter/click select · Wheel/triggers zoom · RMB/I detail · E end turn · Esc pause";

    /// <summary>Always-on corner summary for the cursor tile.</summary>
    public string CompactInfoText { get; set; } = string.Empty;

    public string DetailHeaderText { get; set; } = string.Empty;

    public string DetailTerrainText { get; set; } = string.Empty;

    public string DetailBuildingText { get; set; } = string.Empty;

    public string DetailUnitText { get; set; } = string.Empty;

    public bool HasDetailBuilding { get; set; }

    public bool HasDetailUnit { get; set; }

    /// <summary>True when the compact widget should sit on the left (cursor is on the right half).</summary>
    public bool InfoPreferLeft { get; set; }

    /// <summary>True when the compact widget should sit near the top (cursor is on the bottom half).</summary>
    public bool InfoPreferTop { get; set; }

    /// <summary>Detailed tile inspection overlay (like map/goals).</summary>
    public bool IsTileDetailVisible { get; set; }

    public bool IsPauseVisible { get; set; }

    public bool IsMinimapVisible { get; set; }

    public bool IsGoalsVisible { get; set; }

    public bool IsShopVisible { get; set; }

    /// <summary>Compact Move / Buy chooser above an occupied own castle.</summary>
    public bool IsCellActionChooserVisible { get; set; }

    /// <summary>Screen X of the cell top-center (chooser anchors just above).</summary>
    public float CellActionChooserAnchorX { get; set; }

    /// <summary>Screen Y of the cell top edge.</summary>
    public float CellActionChooserAnchorY { get; set; }

    public string GoalsText { get; set; } = string.Empty;

    public string LevelTitle { get; set; } = string.Empty;

    public string ShopStatusText { get; set; } = string.Empty;

    public IReadOnlyList<GameplayShopOfferViewModel> ShopOffers { get; set; } = [];
}
