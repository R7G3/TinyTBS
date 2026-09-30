namespace TinyTBS.Game.ViewModels;

/// <summary>Modal overlays of the match screen; only the top one of <see cref="MatchOverlayStack"/> is shown.</summary>
public enum MatchOverlay
{
    None,
    Pause,
    Minimap,
    Goals,
    TileDetail,
    CellActionChooser,
    Shop,
    MatchResult,
}
