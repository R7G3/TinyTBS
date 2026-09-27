namespace TinyTBS.Game.Match;

/// <summary>Kind of the last successful player action (for map scripts / UI).</summary>
public enum MatchPlayerActionKind
{
    None,
    SelectUnit,
    MoveUnit,
    AttackUnit,
    DestroyBuilding,
    CaptureBuilding,
    RepairBuilding,
    RaiseSkeleton,
    WaitUnit,
    RecruitUnit,
}
