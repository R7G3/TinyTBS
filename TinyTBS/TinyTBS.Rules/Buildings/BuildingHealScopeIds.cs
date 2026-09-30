namespace TinyTBS.Rules.Buildings;

/// <summary>Building <c>heal.scope</c> values (BUILDING_FORMAT).</summary>
public static class BuildingHealScopeIds
{
    public const string None = "none";

    public const string Allied = "allied";

    public const string Any = "any";

    /// <summary>Editor cycle order.</summary>
    public static IReadOnlyList<string> All { get; } = [None, Allied, Any];

    public static bool IsNone(string? scope) =>
        string.Equals(scope, None, StringComparison.OrdinalIgnoreCase);
}
