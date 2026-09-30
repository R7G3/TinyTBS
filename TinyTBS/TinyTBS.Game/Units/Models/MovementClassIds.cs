namespace TinyTBS.Game.Units.Models;

/// <summary>JSON ids for <see cref="MovementClass"/>; unknown ids fall back to foot.</summary>
public static class MovementClassIds
{
    public const string Foot = "foot";
    public const string Water = "water";
    public const string Fly = "fly";

    /// <summary>Canonical ids in editor order.</summary>
    public static IReadOnlyList<string> All { get; } = [Foot, Water, Fly];

    public static MovementClass Parse(string? movementClass) =>
        movementClass?.Trim().ToLowerInvariant() switch
        {
            Water or "aquatic" => MovementClass.Water,
            Fly or "flying" => MovementClass.Fly,
            _ => MovementClass.Foot,
        };

    public static string ToId(MovementClass movementClass) =>
        movementClass switch
        {
            MovementClass.Water => Water,
            MovementClass.Fly => Fly,
            _ => Foot,
        };

    /// <summary>Canonical id for any accepted spelling (for writers).</summary>
    public static string Normalize(string? movementClass) => ToId(Parse(movementClass));
}
