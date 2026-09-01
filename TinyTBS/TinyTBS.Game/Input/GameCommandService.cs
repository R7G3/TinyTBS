using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyTBS.Core.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Polls MonoGame devices each frame and exposes edge-triggered logical commands.
/// </summary>
public sealed class GameCommandService : IGameCommandSource
{
    private readonly bool[] _current = new bool[CommandCount];
    private readonly bool[] _previous = new bool[CommandCount];

    private static int CommandCount => Enum.GetValues<GameCommand>().Length;

    private static int Index(GameCommand command) => (int)command;

    public void Update()
    {
        Array.Copy(_current, _previous, _current.Length);

        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);

        foreach (var command in Enum.GetValues<GameCommand>())
            _current[Index(command)] = DefaultInputBindings.IsPressed(command, keyboard, gamePad);
    }

    public bool IsPressed(GameCommand command) => _current[Index(command)];

    public bool WasPressed(GameCommand command) =>
        _current[Index(command)] && !_previous[Index(command)];

    public bool WasReleased(GameCommand command) =>
        !_current[Index(command)] && _previous[Index(command)];
}
