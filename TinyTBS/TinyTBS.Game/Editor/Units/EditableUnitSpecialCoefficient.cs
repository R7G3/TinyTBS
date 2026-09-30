namespace TinyTBS.Game.Editor.Units;

/// <summary>Mutable special-coefficient row for the unit master.</summary>
public sealed class EditableUnitSpecialCoefficient
{
    public bool WhenDefault { get; set; }

    public string? TargetHasTag { get; set; }

    public int? ManhattanRange { get; set; }

    public double Multiply { get; set; } = 1.0;

    public string SummaryLine
    {
        get
        {
            string when;
            if (WhenDefault)
                when = "default";
            else if (!string.IsNullOrWhiteSpace(TargetHasTag))
                when = "tag:" + TargetHasTag;
            else if (ManhattanRange is int range)
                when = "range:" + range;
            else
                when = "when:?";

            return when + " × " + Multiply.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
