using Gum.Forms.Controls;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Widgets and focus indices for a Lobby gold / unit-cap stepper.</summary>
internal sealed class NewGameValueStepperWidgets
{
    public required Label ValueLabel { get; init; }

    public required Button DecreaseButton { get; init; }

    public required Button IncreaseButton { get; init; }

    public required int DecreaseFocusIndex { get; init; }

    public required int IncreaseFocusIndex { get; init; }
}
