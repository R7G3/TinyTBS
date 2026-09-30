using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Editor.Units;

/// <summary>Mutable ability entry for the unit master.</summary>
public sealed class EditableUnitAbility
{
    public string Type { get; set; } = UnitAbilityTypes.CaptureBuilding;

    public int? Amount { get; set; }

    public int? MinRange { get; set; }

    public int? Value { get; set; }

    public int? Radius { get; set; }

    public List<string> Tags { get; set; } = [];

    public string SummaryLine
    {
        get
        {
            var parts = new List<string> { Type };
            if (Tags.Count > 0)
                parts.Add("tags:" + string.Join(',', Tags));
            if (Amount is int amount)
                parts.Add("amount:" + amount);
            if (MinRange is int minRange)
                parts.Add("minRange:" + minRange);
            if (Value is int value)
                parts.Add("value:" + value);
            if (Radius is int radius)
                parts.Add("radius:" + radius);
            return string.Join(" · ", parts);
        }
    }
}
