namespace TinyTBS.Game.Editor.Buildings;

/// <summary>Mutable heal block for building master.</summary>
public sealed class EditableBuildingHeal
{
    public int Amount { get; set; }

    public string Scope { get; set; } = BuildingHealScopeIds.Allied;
}
