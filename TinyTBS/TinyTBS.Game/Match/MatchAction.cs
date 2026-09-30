using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// One explicit command for the match rules. Clicks, the bot and (later) network peers all produce these;
/// <see cref="MatchActionRules"/> decides legality in one place and <see cref="MatchState.TryApply"/> applies them.
/// </summary>
public sealed class MatchAction
{
    private MatchAction(MatchActionKind kind, int unitId, GridCell target, ContentId unitTypeId)
    {
        Kind = kind;
        UnitId = unitId;
        Target = target;
        UnitTypeId = unitTypeId;
    }

    public static MatchAction EndTurn { get; } = new(MatchActionKind.EndTurn, unitId: 0, default, default);

    public MatchActionKind Kind { get; }

    /// <summary>Acting unit (for <see cref="MatchActionKind.SelectUnit"/>: the unit to select).</summary>
    public int UnitId { get; }

    /// <summary>
    /// Cell the command points at: destination, target, the acting unit's own cell,
    /// or the castle for <see cref="MatchActionKind.RecruitUnit"/>. This is where a player would click.
    /// </summary>
    public GridCell Target { get; }

    /// <summary>Unit type for <see cref="MatchActionKind.RecruitUnit"/>.</summary>
    public ContentId UnitTypeId { get; }

    public static MatchAction SelectUnit(int unitId, GridCell unitCell) =>
        new(MatchActionKind.SelectUnit, unitId, unitCell, default);

    public static MatchAction MoveUnit(int unitId, GridCell destination) =>
        new(MatchActionKind.MoveUnit, unitId, destination, default);

    public static MatchAction AttackUnit(int unitId, GridCell targetCell) =>
        new(MatchActionKind.AttackUnit, unitId, targetCell, default);

    public static MatchAction DestroyBuilding(int unitId, GridCell buildingCell) =>
        new(MatchActionKind.DestroyBuilding, unitId, buildingCell, default);

    public static MatchAction CaptureBuilding(int unitId, GridCell unitCell) =>
        new(MatchActionKind.CaptureBuilding, unitId, unitCell, default);

    public static MatchAction RepairBuilding(int unitId, GridCell unitCell) =>
        new(MatchActionKind.RepairBuilding, unitId, unitCell, default);

    public static MatchAction RaiseSkeleton(int unitId, GridCell gravestoneCell) =>
        new(MatchActionKind.RaiseSkeleton, unitId, gravestoneCell, default);

    public static MatchAction WaitUnit(int unitId, GridCell unitCell) =>
        new(MatchActionKind.WaitUnit, unitId, unitCell, default);

    public static MatchAction RecruitUnit(ContentId unitTypeId, GridCell castleCell) =>
        new(MatchActionKind.RecruitUnit, unitId: 0, castleCell, unitTypeId);

    public override string ToString() =>
        Kind switch
        {
            MatchActionKind.EndTurn => "EndTurn",
            MatchActionKind.RecruitUnit => $"RecruitUnit {UnitTypeId.Full} {Target}",
            _ => $"{Kind} u{UnitId} {Target}",
        };
}
