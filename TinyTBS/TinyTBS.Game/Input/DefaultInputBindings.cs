using Microsoft.Xna.Framework.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Default keyboard and gamepad bindings for <see cref="GameCommand"/> values.
/// </summary>
internal static class DefaultInputBindings
{
    public static bool IsPressed(GameCommand command, KeyboardState keyboard, GamePadState gamePad)
    {
        return command switch
        {
            GameCommand.Confirm => keyboard.IsKeyDown(Keys.Enter)
                || keyboard.IsKeyDown(Keys.Space)
                || gamePad.Buttons.A == ButtonState.Pressed,

            // Backspace — Escape opens Pause in match; face east (B) is Info.
            GameCommand.Cancel => keyboard.IsKeyDown(Keys.Back),

            GameCommand.Back => gamePad.Buttons.Back == ButtonState.Pressed,

            GameCommand.NavigateUp => keyboard.IsKeyDown(Keys.Up)
                || keyboard.IsKeyDown(Keys.W)
                || gamePad.DPad.Up == ButtonState.Pressed
                || gamePad.ThumbSticks.Left.Y > 0.5f,

            GameCommand.NavigateDown => keyboard.IsKeyDown(Keys.Down)
                || keyboard.IsKeyDown(Keys.S)
                || gamePad.DPad.Down == ButtonState.Pressed
                || gamePad.ThumbSticks.Left.Y < -0.5f,

            GameCommand.NavigateLeft => keyboard.IsKeyDown(Keys.Left)
                || keyboard.IsKeyDown(Keys.A)
                || gamePad.DPad.Left == ButtonState.Pressed
                || gamePad.ThumbSticks.Left.X < -0.5f,

            GameCommand.NavigateRight => keyboard.IsKeyDown(Keys.Right)
                || keyboard.IsKeyDown(Keys.D)
                || gamePad.DPad.Right == ButtonState.Pressed
                || gamePad.ThumbSticks.Left.X > 0.5f,

            GameCommand.Pause => keyboard.IsKeyDown(Keys.Escape)
                || gamePad.Buttons.Start == ButtonState.Pressed,

            GameCommand.Info => keyboard.IsKeyDown(Keys.I)
                || gamePad.Buttons.B == ButtonState.Pressed,

            GameCommand.EndTurn => keyboard.IsKeyDown(Keys.E),

            // Analog triggers: held while past deadzone (continuous zoom in ApplyZoom).
            GameCommand.ZoomIn => gamePad.Triggers.Right > 0.25f,

            GameCommand.ZoomOut => gamePad.Triggers.Left > 0.25f,

            _ => false,
        };
    }
}
