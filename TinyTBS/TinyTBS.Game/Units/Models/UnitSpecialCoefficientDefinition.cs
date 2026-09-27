namespace TinyTBS.Game.Units.Models;

/// <summary>One ordered <c>specialCoefficients</c> entry; first matching <see cref="When"/> wins.</summary>
public sealed class UnitSpecialCoefficientDefinition
{
    public required UnitSpecialWhenDefinition When { get; init; }

    public required double Multiply { get; init; }
}
