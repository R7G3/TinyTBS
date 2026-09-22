using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Input;

/// <summary>
/// Read-only view of logical game commands for screens and gameplay systems.
/// </summary>
public interface IGameCommandSource
{
    bool IsPressed(GameCommand command);

    /// <summary>True only on the frame the command became pressed.</summary>
    bool WasPressed(GameCommand command);

    /// <summary>True only on the frame the command was released.</summary>
    bool WasReleased(GameCommand command);

    /// <summary>
    /// Right stick after deadzone (MonoGame: +X right, +Y up). Zero when idle or disconnected.
    /// Used for board camera pan.
    /// </summary>
    Vector2 CameraPanStick { get; }
}
