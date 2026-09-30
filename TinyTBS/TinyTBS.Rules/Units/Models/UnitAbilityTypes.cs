namespace TinyTBS.Rules.Units.Models;

/// <summary>Ability <c>type</c> ids understood by match rules (UNIT_FORMAT <c>abilities</c>).</summary>
public static class UnitAbilityTypes
{
    public const string CaptureBuilding = "captureBuilding";
    public const string RepairBuilding = "repairBuilding";
    public const string RaiseSkeleton = "raiseSkeleton";
    public const string AttackAura = "attackAura";
    public const string DestroyBuilding = "destroyBuilding";
    public const string NoCounterattack = "noCounterattack";
    public const string NoCounterattackWhenRangeAtLeast = "noCounterattackWhenRangeAtLeast";
    public const string MoveOrAttackExclusive = "moveOrAttackExclusive";
    public const string UniquePerPlayer = "uniquePerPlayer";
    public const string RehireCostIncrement = "rehireCostIncrement";

    /// <summary>Local id (in the raiser's namespace) of the unit <see cref="RaiseSkeleton"/> spawns.</summary>
    public const string RaisedUnitLocalId = "skeleton";

    /// <summary>All known ability types in editor order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        CaptureBuilding,
        RepairBuilding,
        RaiseSkeleton,
        AttackAura,
        DestroyBuilding,
        NoCounterattack,
        NoCounterattackWhenRangeAtLeast,
        MoveOrAttackExclusive,
        UniquePerPlayer,
        RehireCostIncrement,
    ];
}
