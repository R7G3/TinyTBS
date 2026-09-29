namespace TinyTBS.Game.Editor.Buildings;

/// <summary>Mutable ruined stats for building master.</summary>
public sealed class EditableBuildingRuined
{
    public int Income { get; set; }

    public int DefenceBonus { get; set; }

    public EditableBuildingHeal? Heal { get; set; }

    public bool Capturable { get; set; }
}
