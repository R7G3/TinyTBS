using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Polls MonoGame devices each frame and exposes edge-triggered logical commands.
/// </summary>
public sealed class GameCommandService : IGameCommandSource
{
    private const float CameraPanStickDeadzone = 0.2f;

    private readonly bool[] _current = new bool[CommandCount];
    private readonly bool[] _previous = new bool[CommandCount];

    private static int CommandCount => Enum.GetValues<GameCommand>().Length;

    private static int Index(GameCommand command) => (int)command;

    public Vector2 CameraPanStick { get; private set; }

    public void Update()
    {
        Array.Copy(_current, _previous, _current.Length);

        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);

        foreach (var command in Enum.GetValues<GameCommand>())
            _current[Index(command)] = DefaultInputBindings.IsPressed(command, keyboard, gamePad);

        CameraPanStick = ReadCameraPanStick(gamePad);
    }

    public bool IsPressed(GameCommand command) => _current[Index(command)];

    public bool WasPressed(GameCommand command) =>
        _current[Index(command)] && !_previous[Index(command)];

    public bool WasReleased(GameCommand command) =>
        !_current[Index(command)] && _previous[Index(command)];

    private static Vector2 ReadCameraPanStick(GamePadState gamePad)
    {
        if (!gamePad.IsConnected)
            return Vector2.Zero;

        var stick = gamePad.ThumbSticks.Right;
        if (stick.LengthSquared() < CameraPanStickDeadzone * CameraPanStickDeadzone)
            return Vector2.Zero;

        return stick;
    }
}
