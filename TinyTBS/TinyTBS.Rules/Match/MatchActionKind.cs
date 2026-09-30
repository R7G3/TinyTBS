namespace TinyTBS.Rules.Match;

/// <summary>Kinds of explicit match commands (see <see cref="MatchAction"/>).</summary>
public enum MatchActionKind
{
    SelectUnit,
    MoveUnit,
    AttackUnit,
    DestroyBuilding,
    CaptureBuilding,
    RepairBuilding,
    RaiseSkeleton,
    WaitUnit,
    RecruitUnit,
    EndTurn,
}
