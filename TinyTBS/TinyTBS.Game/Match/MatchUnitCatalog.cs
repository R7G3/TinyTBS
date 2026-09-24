namespace TinyTBS.Game.Match;

/// <summary>Demo unit display names and combat stats until units modules drive the catalog.</summary>
public static class MatchUnitCatalog
{
    public static string DisplayName(UnitKind kind) => kind switch
    {
        UnitKind.King => "King",
        UnitKind.Swordsman => "Swordsman",
        UnitKind.Archer => "Archer",
        UnitKind.Lizard => "Lizard",
        UnitKind.Witch => "Witch",
        UnitKind.Wisp => "Wisp",
        UnitKind.Golem => "Golem",
        UnitKind.Catapult => "Catapult",
        UnitKind.Wyvern => "Wyvern",
        UnitKind.Skeleton => "Skeleton",
        _ => kind.ToString(),
    };

    /// <summary>Multiline combat summary for shop / inspect UI.</summary>
    public static string FormatCombatStats(UnitKind kind) => kind switch
    {
        UnitKind.King =>
            $"Atk 65  Def 20  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Swordsman =>
            $"Atk 45  Def 5  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Archer =>
            $"Atk 45  Def 5  HP 100{Environment.NewLine}Range 1–3  Speed 5",
        UnitKind.Lizard =>
            $"Atk 50  Def 15  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Witch =>
            $"Atk 40  Def 20  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Wisp =>
            $"Atk 50  Def 20  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Golem =>
            $"Atk 65  Def 25  HP 100{Environment.NewLine}Range 1  Speed 5",
        UnitKind.Catapult =>
            $"Atk 75  Def 10  HP 100{Environment.NewLine}Range 3–5  Speed 3",
        UnitKind.Wyvern =>
            $"Atk 70  Def 25  HP 100{Environment.NewLine}Range 1  Speed 7",
        UnitKind.Skeleton =>
            $"Atk 45  Def 5  HP 100{Environment.NewLine}Range 1  Speed 4",
        _ => string.Empty,
    };
}
