using TinyTBS.Game.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Hold-to-repeat for menu Up/Down (and optional L/R-as-vertical), same timing as lobby ± steppers.
/// </summary>
public sealed class MenuVerticalNavigateRepeat
{
    private float _upTimer;
    private float _downTimer;
    private float _leftAsUpTimer;
    private float _rightAsDownTimer;

    public void Reset()
    {
        _upTimer = 0f;
        _downTimer = 0f;
        _leftAsUpTimer = 0f;
        _rightAsDownTimer = 0f;
    }

    /// <summary>Returns -1 (up), +1 (down), or 0.</summary>
    public int TryGetDelta(
        IGameCommandSource commands,
        float elapsedSeconds,
        bool mapHorizontalToVertical = false)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (HeldCommandRepeat.TryTick(
                commands,
                GameCommand.NavigateDown,
                elapsedSeconds,
                ref _downTimer))
        {
            return 1;
        }

        if (mapHorizontalToVertical
            && HeldCommandRepeat.TryTick(
                commands,
                GameCommand.NavigateRight,
                elapsedSeconds,
                ref _rightAsDownTimer))
        {
            return 1;
        }

        if (HeldCommandRepeat.TryTick(
                commands,
                GameCommand.NavigateUp,
                elapsedSeconds,
                ref _upTimer))
        {
            return -1;
        }

        if (mapHorizontalToVertical
            && HeldCommandRepeat.TryTick(
                commands,
                GameCommand.NavigateLeft,
                elapsedSeconds,
                ref _leftAsUpTimer))
        {
            return -1;
        }

        return 0;
    }
}
