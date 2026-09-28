using TinyTBS.Game.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Edge on press, then repeats while the command stays held (match cursor, lobby steppers, menus).
/// </summary>
public static class HeldCommandRepeat
{
    /// <summary>Delay before held input starts repeating.</summary>
    public const float DefaultInitialDelaySeconds = 0.32f;

    /// <summary>Interval between repeats while held.</summary>
    public const float DefaultIntervalSeconds = 0.11f;

    /// <summary>
    /// Returns true on the press frame and on each repeat tick while <paramref name="command"/> stays down.
    /// </summary>
    public static bool TryTick(
        IGameCommandSource commands,
        GameCommand command,
        float elapsedSeconds,
        ref float repeatTimer,
        float initialDelaySeconds = DefaultInitialDelaySeconds,
        float intervalSeconds = DefaultIntervalSeconds)
    {
        if (!commands.IsPressed(command))
        {
            repeatTimer = 0f;
            return false;
        }

        if (commands.WasPressed(command))
        {
            repeatTimer = initialDelaySeconds;
            return true;
        }

        repeatTimer -= elapsedSeconds;
        if (repeatTimer > 0f)
            return false;

        repeatTimer = intervalSeconds;
        return true;
    }
}
