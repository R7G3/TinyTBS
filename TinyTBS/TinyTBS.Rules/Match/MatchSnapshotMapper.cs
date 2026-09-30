using TinyTBS.Rules.Maps.Models;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Rules.Match;

/// <summary>
/// Maps match entities to save snapshots and back. Add a new persisted field in both methods of the
/// same pair here; <see cref="MatchState.ToSnapshot"/> and <see cref="MatchState.HydrateFromSnapshot"/>
/// only call these.
/// </summary>
public static class MatchSnapshotMapper
{
    public static MatchSaveUnitSnapshot ToSnapshot(MatchUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new MatchSaveUnitSnapshot
        {
            Id = unit.Id,
            TypeId = unit.TypeId.Full,
            Cell = ToCell(unit.Cell),
            PlayerIndex = unit.PlayerIndex,
            MaxHealth = unit.MaxHealth,
            HitPoints = unit.HitPoints,
            IsActive = unit.IsActive,
            HasMovedThisActivation = unit.HasMovedThisActivation,
            CellBeforeMove = ToCell(unit.CellBeforeMove),
            Experience = unit.Experience,
        };
    }

    public static MatchUnit FromSnapshot(MatchSaveUnitSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Cell);
        ArgumentNullException.ThrowIfNull(snapshot.CellBeforeMove);

        var maxHealth = Math.Max(1, snapshot.MaxHealth);
        return new MatchUnit(
            snapshot.Id,
            ContentId.Parse(snapshot.TypeId),
            ToCell(snapshot.Cell),
            snapshot.PlayerIndex,
            maxHealth,
            Math.Clamp(snapshot.HitPoints, 1, maxHealth))
        {
            IsActive = snapshot.IsActive,
            HasMovedThisActivation = snapshot.HasMovedThisActivation,
            CellBeforeMove = ToCell(snapshot.CellBeforeMove),
            Experience = Math.Clamp(snapshot.Experience, 0, MatchUnit.MaxExperience),
        };
    }

    public static MatchSaveBuildingSnapshot ToSnapshot(MatchBuilding building)
    {
        ArgumentNullException.ThrowIfNull(building);
        return new MatchSaveBuildingSnapshot
        {
            TypeId = building.TypeId.Full,
            Cell = ToCell(building.Cell),
            OwnerPlayerIndex = building.OwnerPlayerIndex,
            IsRuined = building.IsRuined,
            AllowsRecruit = building.AllowsRecruit,
            RepairedThisOwnerTurn = building.RepairedThisOwnerTurn,
        };
    }

    public static MatchBuilding FromSnapshot(MatchSaveBuildingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Cell);

        return new MatchBuilding(
            ContentId.Parse(snapshot.TypeId),
            ToCell(snapshot.Cell),
            snapshot.OwnerPlayerIndex,
            snapshot.IsRuined,
            snapshot.AllowsRecruit)
        {
            RepairedThisOwnerTurn = snapshot.RepairedThisOwnerTurn,
        };
    }

    public static MatchSaveGravestoneSnapshot ToSnapshot(MatchGravestone stone)
    {
        ArgumentNullException.ThrowIfNull(stone);
        return new MatchSaveGravestoneSnapshot
        {
            Cell = ToCell(stone.Cell),
            SourcePlayerIndex = stone.SourcePlayerIndex,
            ExpiresWhenTurnStartsReaches = stone.ExpiresWhenTurnStartsReaches,
        };
    }

    public static MatchGravestone FromSnapshot(MatchSaveGravestoneSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Cell);

        return new MatchGravestone(
            ToCell(snapshot.Cell),
            snapshot.SourcePlayerIndex,
            snapshot.ExpiresWhenTurnStartsReaches);
    }

    public static MatchSaveCell ToCell(GridCell cell) => new() { X = cell.X, Y = cell.Y };

    public static GridCell ToCell(MatchSaveCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        return new GridCell(cell.X, cell.Y);
    }
}
