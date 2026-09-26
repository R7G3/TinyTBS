namespace TinyTBS.Game.Themes.Models;

/// <summary>Theme override of base/mask paths for a logical content id.</summary>
public sealed class ThemeSpriteRemap
{
    public required string BasePath { get; init; }

    public required string MaskPath { get; init; }
}
