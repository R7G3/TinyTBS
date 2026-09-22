namespace TinyTBS.Game.Match;

/// <summary>Demo unit display names and combat stats until units modules exist.</summary>
public static class MatchUnitCatalog
{
    public static string DisplayName(UnitKind kind) => kind switch
    {
        UnitKind.King => "King",
        UnitKind.Swordsman => "Swordsman",
        _ => kind.ToString(),
    };

    /// <summary>Multiline combat summary for shop / inspect UI.</summary>
    public static string FormatCombatStats(UnitKind kind) => kind switch
    {
        UnitKind.King =>
            $"Atk 65  Def 20  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Swordsman =>
            $"Atk 45  Def 5  HP 100{Environment.NewLine}Range 1  Speed 5",
        _ => string.Empty,
    };
}
