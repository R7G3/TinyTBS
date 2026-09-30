namespace TinyTBS.Scripting.Api;

/// <summary>Player action kinds reported to map scripts. New values are only appended.</summary>
public enum MapScriptActionKind
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
}
