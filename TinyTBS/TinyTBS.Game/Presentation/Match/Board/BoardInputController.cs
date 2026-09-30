using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Board input for one screen: cursor navigation with held repeat, click-vs-drag on the primary button,
/// camera zoom and pan. Each screen owns its instance, so gestures never leak between screens.
/// </summary>
public sealed class BoardInputController
{
    /// <summary>Zoom change per second while a gamepad trigger is held.</summary>
    private const float TriggerZoomPerSecond = 1.25f;

    /// <summary>Screen pixels per second at full right-stick deflection.</summary>
    private const float StickPanPixelsPerSecond = 520f;

    /// <summary>Primary movement beyond this starts map pan instead of a click Confirm.</summary>
    private const float PointerPanThresholdPixels = 7f;

    private bool _pointerPressActive;
    private bool _pointerIsPanning;
    private Vector2 _pointerPressPosition;
    private Vector2 _pointerLastPosition;

    private float _navigateUpRepeatTimer;
    private float _navigateDownRepeatTimer;
    private float _navigateLeftRepeatTimer;
    private float _navigateRightRepeatTimer;

    /// <summary>True while primary is down and the gesture has crossed the pan threshold.</summary>
    public bool IsPrimaryGesturePanning => _pointerIsPanning;

    /// <summary>True while a primary press is being tracked (click or pan).</summary>
    public bool IsPrimaryGestureActive => _pointerPressActive;

    /// <summary>
    /// Starts click-vs-pan tracking for the current primary press (call on WasPrimaryPressed over the board).
    /// </summary>
    public void ArmPrimaryPointerGesture(IPointerSource pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        var position = new Vector2(pointer.Position.X, pointer.Position.Y);
        _pointerPressActive = true;
        _pointerIsPanning = false;
        _pointerPressPosition = position;
        _pointerLastPosition = position;
    }

    /// <summary>
    /// Ends the primary gesture on release. Returns true when it was a short click (not a pan).
    /// </summary>
    public bool TryConsumePrimaryClick(IPointerSource pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        if (pointer.WasPrimaryReleased)
        {
            var wasClick = _pointerPressActive && !_pointerIsPanning;
            ResetPointerGesture();
            return wasClick;
        }

        if (!pointer.IsPrimaryDown && _pointerPressActive)
            ResetPointerGesture();

        return false;
    }

    /// <summary>
    /// Clears an in-progress primary click (e.g. after opening the castle Move/Buy chooser on press
    /// so the matching release does not Confirm through a stale gesture).
    /// </summary>
    public void CancelPointerGesture() => ResetPointerGesture();

    /// <summary>
    /// Applies board commands when the match is not blocked by pause/shop UI.
    /// Leave-to-menu is only via the pause menu item, not a board command.
    /// </summary>
    public void ApplyBoardCommands(
        GameplaySession session,
        IGameCommandSource commands,
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
            session.Runtime.EndTurn();

        if (allowConfirm && commands.WasPressed(GameCommand.Confirm))
            session.Confirm();

        if (commands.WasPressed(GameCommand.Wait))
            session.Runtime.TryWaitSelectedUnit();

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var cursor = session.Cursor;
        var cursorMoved = false;

        if (TryNavigateStep(commands, GameCommand.NavigateUp, elapsedSeconds, ref _navigateUpRepeatTimer))
        {
            cursor.MoveBy(0, -1);
            cursorMoved = true;
        }

        if (TryNavigateStep(commands, GameCommand.NavigateDown, elapsedSeconds, ref _navigateDownRepeatTimer))
        {
            cursor.MoveBy(0, 1);
            cursorMoved = true;
        }

        if (TryNavigateStep(commands, GameCommand.NavigateLeft, elapsedSeconds, ref _navigateLeftRepeatTimer))
        {
            cursor.MoveBy(-1, 0);
            cursorMoved = true;
        }

        if (TryNavigateStep(commands, GameCommand.NavigateRight, elapsedSeconds, ref _navigateRightRepeatTimer))
        {
            cursor.MoveBy(1, 0);
            cursorMoved = true;
        }

        if (cursorMoved)
            session.Scene.Layout.KeepCellInCentralZone(cursor.Cell.X, cursor.Cell.Y);
    }

    /// <summary>
    /// Board zoom: right trigger zoom in; left trigger zoom out.
    /// Wheel: up = zoom in, down = zoom out.
    /// </summary>
    public void ApplyZoom(
        MatchBoardLayout layout,
        IGameCommandSource commands,
        IPointerSource pointer,
        GameTime gameTime,
        bool cameraControlsEnabled)
    {
        if (!cameraControlsEnabled)
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
    /// With <paramref name="clampCursor"/> the board cursor stays on screen after a pan (human turn);
    /// during a bot turn leave it false so the viewer can look around freely.
    /// </summary>
    public void ApplyCameraPan(
        MatchCursor cursor,
        MatchBoardLayout layout,
        IGameCommandSource commands,
        IPointerSource pointer,
        GameTime gameTime,
        bool cameraControlsEnabled,
        bool clampCursor)
    {
        var offsetBefore = layout.CameraOffset;
        ApplyCameraPan(layout, commands, pointer, gameTime, cameraControlsEnabled);
        if (clampCursor && cameraControlsEnabled && layout.CameraOffset != offsetBefore)
            KeepCursorOnScreen(cursor, layout);
    }

    /// <summary>Camera pan without a board cursor (editor / free look).</summary>
    public void ApplyCameraPan(
        MatchBoardLayout layout,
        IGameCommandSource commands,
        IPointerSource pointer,
        GameTime gameTime,
        bool cameraControlsEnabled)
    {
        if (!cameraControlsEnabled)
        {
            ResetPointerGesture();
            return;
        }

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var stick = commands.CameraPanStick;
        if (stick != Vector2.Zero)
        {
            // Stick +X/+Y = camera right/up → map moves left/down on screen.
            layout.PanBy(new Vector2(-stick.X, stick.Y) * StickPanPixelsPerSecond * elapsedSeconds);
        }

        ApplyPointerPan(layout, pointer);
    }

    /// <summary>
    /// Click-vs-drag: a short primary click moves the cursor and Confirms; a drag beyond the threshold pans
    /// (handled in <see cref="ApplyCameraPan(MatchBoardLayout, IGameCommandSource, IPointerSource, GameTime, bool)"/>).
    /// </summary>
    public void ApplyPointer(GameplaySession session, IPointerSource pointer, bool allowConfirm = true)
    {
        var layout = session.Scene.Layout;

        if (pointer.WasPrimaryPressed)
        {
            ArmPrimaryPointerGesture(pointer);
            if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
                session.Cursor.MoveTo(new GridCell(cellX, cellY));
            return;
        }

        if (_pointerPressActive && pointer.IsPrimaryDown && !_pointerIsPanning)
        {
            var position = new Vector2(pointer.Position.X, pointer.Position.Y);
            var fromPress = position - _pointerPressPosition;
            if (fromPress.LengthSquared() >= PointerPanThresholdPixels * PointerPanThresholdPixels)
                _pointerIsPanning = true;
        }

        if (pointer.WasPrimaryReleased)
        {
            var wasClick = TryConsumePrimaryClick(pointer);
            if (!wasClick || !allowConfirm)
                return;

            if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            {
                session.Cursor.MoveTo(new GridCell(cellX, cellY));
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
    public bool TryApplySecondaryPointer(MatchCursor cursor, IPointerSource pointer, MatchBoardLayout layout)
    {
        if (!pointer.WasSecondaryPressed)
            return false;

        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            cursor.MoveTo(new GridCell(cellX, cellY));

        return true;
    }

    private static bool TryNavigateStep(
        IGameCommandSource commands,
        GameCommand command,
        float elapsedSeconds,
        ref float repeatTimer) =>
        HeldCommandRepeat.TryTick(
            commands,
            command,
            elapsedSeconds,
            ref repeatTimer,
            HeldCommandRepeat.DefaultInitialDelaySeconds,
            HeldCommandRepeat.DefaultIntervalSeconds);

    private static void KeepCursorOnScreen(MatchCursor cursor, MatchBoardLayout layout)
    {
        var cellX = cursor.Cell.X;
        var cellY = cursor.Cell.Y;
        layout.ClampCellToViewport(ref cellX, ref cellY);
        if (cellX != cursor.Cell.X || cellY != cursor.Cell.Y)
            cursor.MoveTo(new GridCell(cellX, cellY));
    }

    private void ResetCursorRepeatTimers()
    {
        _navigateUpRepeatTimer = 0f;
        _navigateDownRepeatTimer = 0f;
        _navigateLeftRepeatTimer = 0f;
        _navigateRightRepeatTimer = 0f;
    }

    private void ApplyPointerPan(MatchBoardLayout layout, IPointerSource pointer)
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

    private void ResetPointerGesture()
    {
        _pointerPressActive = false;
        _pointerIsPanning = false;
        _pointerPressPosition = Vector2.Zero;
        _pointerLastPosition = Vector2.Zero;
    }
}
