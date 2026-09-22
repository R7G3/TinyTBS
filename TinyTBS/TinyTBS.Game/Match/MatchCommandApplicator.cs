using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Input;
using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Match;

/// <summary>
/// Applies logical game commands / pointer picks to a <see cref="GameplaySession"/> (no devices, no draw).
/// </summary>
public static class MatchCommandApplicator
{
    /// <summary>Zoom change per second while a gamepad trigger is held.</summary>
    private const float TriggerZoomPerSecond = 1.25f;

    /// <summary>Screen pixels per second at full right-stick deflection.</summary>
    private const float StickPanPixelsPerSecond = 520f;

    /// <summary>Primary movement beyond this starts map pan instead of a click Confirm.</summary>
    private const float PointerPanThresholdPixels = 7f;

    /// <summary>Delay before held Navigate starts repeating.</summary>
    private const float CursorRepeatInitialDelaySeconds = 0.32f;

    /// <summary>Interval between repeated cursor steps while Navigate is held.</summary>
    private const float CursorRepeatIntervalSeconds = 0.11f;

    private static bool _pointerPressActive;
    private static bool _pointerIsPanning;
    private static Vector2 _pointerPressPosition;
    private static Vector2 _pointerLastPosition;

    private static float _navigateUpRepeatTimer;
    private static float _navigateDownRepeatTimer;
    private static float _navigateLeftRepeatTimer;
    private static float _navigateRightRepeatTimer;

    /// <summary>
    /// Applies board commands when the match is not blocked by pause/shop UI.
    /// Leave-to-menu is only via the pause menu item, not a board command.
    /// </summary>
    public static void Apply(
        GameplaySession session,
        IGameCommandSource commands,
        MatchBoardLayout layout,
        GameTime gameTime,
        bool boardInputEnabled,
        bool allowConfirm = true)
    {
        if (!boardInputEnabled)
        {
            ResetCursorRepeatTimers();
            return;
        }

        if (commands.WasPressed(GameCommand.EndTurn))
            session.EndTurn();

        if (allowConfirm && commands.WasPressed(GameCommand.Confirm))
            session.Confirm();

        var match = session.State;
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var cursorMoved = false;

        if (TryNavigateStep(
                commands,
                GameCommand.NavigateUp,
                elapsedSeconds,
                ref _navigateUpRepeatTimer))
        {
            match.MoveCursor(0, -1);
            cursorMoved = true;
        }

        if (TryNavigateStep(
                commands,
                GameCommand.NavigateDown,
                elapsedSeconds,
                ref _navigateDownRepeatTimer))
        {
            match.MoveCursor(0, 1);
            cursorMoved = true;
        }

        if (TryNavigateStep(
                commands,
                GameCommand.NavigateLeft,
                elapsedSeconds,
                ref _navigateLeftRepeatTimer))
        {
            match.MoveCursor(-1, 0);
            cursorMoved = true;
        }

        if (TryNavigateStep(
                commands,
                GameCommand.NavigateRight,
                elapsedSeconds,
                ref _navigateRightRepeatTimer))
        {
            match.MoveCursor(1, 0);
            cursorMoved = true;
        }

        if (cursorMoved)
            layout.KeepCellInCentralZone(match.Cursor.X, match.Cursor.Y);
    }

    /// <summary>
    /// Board zoom: right trigger zoom in; left trigger zoom out.
    /// Wheel: up = zoom in, down = zoom out.
    /// </summary>
    public static void ApplyZoom(
        MatchBoardLayout layout,
        IGameCommandSource commands,
        IPointerSource pointer,
        GameTime gameTime,
        bool boardInputEnabled)
    {
        if (!boardInputEnabled)
            return;

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (commands.IsPressed(GameCommand.ZoomIn))
            layout.AdjustZoom(TriggerZoomPerSecond * elapsedSeconds);
        if (commands.IsPressed(GameCommand.ZoomOut))
            layout.AdjustZoom(-TriggerZoomPerSecond * elapsedSeconds);

        // MonoGame: positive ScrollWheelDelta = wheel up.
        var wheelDelta = pointer.ScrollWheelDelta;
        if (wheelDelta > 0)
            layout.ZoomBySteps(1);
        else if (wheelDelta < 0)
            layout.ZoomBySteps(-1);
    }

    /// <summary>
    /// Camera pan: right stick (camera follows stick) and LMB drag.
    /// After pan, keeps the match cursor on a cell whose center is still on screen.
    /// </summary>
    public static void ApplyCameraPan(
        MatchState match,
        MatchBoardLayout layout,
        IGameCommandSource commands,
        IPointerSource pointer,
        GameTime gameTime,
        bool boardInputEnabled)
    {
        if (!boardInputEnabled)
        {
            ResetPointerGesture();
            return;
        }

        var offsetBefore = layout.CameraOffset;
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var stick = commands.CameraPanStick;
        if (stick != Vector2.Zero)
        {
            // Stick +X/+Y = camera right/up → map moves left/down on screen.
            layout.PanBy(new Vector2(-stick.X, stick.Y) * StickPanPixelsPerSecond * elapsedSeconds);
        }

        ApplyPointerPan(layout, pointer);

        if (layout.CameraOffset != offsetBefore)
            KeepCursorOnScreen(match, layout);
    }

    /// <summary>
    /// Click-vs-drag: short primary click Confirms; drag beyond threshold pans (handled in <see cref="ApplyCameraPan"/>).
    /// </summary>
    public static void ApplyPointer(
        GameplaySession session,
        IPointerSource pointer,
        MatchBoardLayout layout,
        bool allowConfirm = true)
    {
        var match = session.State;
        var position = new Vector2(pointer.Position.X, pointer.Position.Y);

        if (pointer.WasPrimaryPressed)
        {
            _pointerPressActive = true;
            _pointerIsPanning = false;
            _pointerPressPosition = position;
            _pointerLastPosition = position;
            if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
                match.HandlePointer(new GridCell(cellX, cellY));
            return;
        }

        if (_pointerPressActive && pointer.IsPrimaryDown && !_pointerIsPanning)
        {
            var fromPress = position - _pointerPressPosition;
            if (fromPress.LengthSquared() >= PointerPanThresholdPixels * PointerPanThresholdPixels)
                _pointerIsPanning = true;
        }

        if (pointer.WasPrimaryReleased)
        {
            var wasClick = _pointerPressActive && !_pointerIsPanning;
            ResetPointerGesture();
            if (!wasClick || !allowConfirm)
                return;

            if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            {
                match.HandlePointer(new GridCell(cellX, cellY));
                session.Confirm();
            }

            return;
        }

        if (!pointer.IsPrimaryDown && _pointerPressActive)
            ResetPointerGesture();
    }

    /// <summary>
    /// Moves the cursor under the secondary press (right mouse). Returns true when that frame had a secondary press.
    /// </summary>
    public static bool TryApplySecondaryPointer(
        MatchState match,
        IPointerSource pointer,
        MatchBoardLayout layout)
    {
        if (!pointer.WasSecondaryPressed)
            return false;

        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            match.HandlePointer(new GridCell(cellX, cellY));

        return true;
    }

    private static bool TryNavigateStep(
        IGameCommandSource commands,
        GameCommand command,
        float elapsedSeconds,
        ref float repeatTimer)
    {
        if (!commands.IsPressed(command))
        {
            repeatTimer = 0f;
            return false;
        }

        if (commands.WasPressed(command))
        {
            repeatTimer = CursorRepeatInitialDelaySeconds;
            return true;
        }

        repeatTimer -= elapsedSeconds;
        if (repeatTimer > 0f)
            return false;

        repeatTimer = CursorRepeatIntervalSeconds;
        return true;
    }

    private static void ResetCursorRepeatTimers()
    {
        _navigateUpRepeatTimer = 0f;
        _navigateDownRepeatTimer = 0f;
        _navigateLeftRepeatTimer = 0f;
        _navigateRightRepeatTimer = 0f;
    }

    private static void KeepCursorOnScreen(MatchState match, MatchBoardLayout layout)
    {
        var cellX = match.Cursor.X;
        var cellY = match.Cursor.Y;
        layout.ClampCellToViewport(ref cellX, ref cellY);
        if (cellX != match.Cursor.X || cellY != match.Cursor.Y)
            match.HandlePointer(new GridCell(cellX, cellY));
    }

    private static void ApplyPointerPan(MatchBoardLayout layout, IPointerSource pointer)
    {
        if (!_pointerPressActive || !pointer.IsPrimaryDown)
            return;

        var position = new Vector2(pointer.Position.X, pointer.Position.Y);
        if (!_pointerIsPanning)
        {
            var fromPress = position - _pointerPressPosition;
            if (fromPress.LengthSquared() < PointerPanThresholdPixels * PointerPanThresholdPixels)
            {
                _pointerLastPosition = position;
                return;
            }

            _pointerIsPanning = true;
        }

        var frameDelta = position - _pointerLastPosition;
        _pointerLastPosition = position;
        if (frameDelta != Vector2.Zero)
            layout.PanBy(frameDelta);
    }

    private static void ResetPointerGesture()
    {
        _pointerPressActive = false;
        _pointerIsPanning = false;
        _pointerPressPosition = Vector2.Zero;
        _pointerLastPosition = Vector2.Zero;
    }
}
