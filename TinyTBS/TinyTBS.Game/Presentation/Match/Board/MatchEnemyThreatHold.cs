using TinyTBS.Engine.Input;
using TinyTBS.Game.Input;
using TinyTBS.Rules.Match;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Hold Confirm / LMB on an enemy unit to preview its next-turn move / attack / capture / raise.
/// Short press still Confirms (e.g. attack with a selected unit).
/// While held, moving the cursor onto another enemy retargets the preview.
/// </summary>
public sealed class MatchEnemyThreatHold
{
    public const float HoldSeconds = 0.18f;

    private enum HoldSource
    {
        None,
        Command,
        Pointer,
    }

    private HoldSource _source;
    private int _unitId;
    private GridCell _cell;
    private float _heldSeconds;
    private bool _previewActive;
    private bool _sawPreview;

    public int? PreviewUnitId => _previewActive ? _unitId : null;

    /// <summary>
    /// When true, board Confirm (press or click-release) must not fire — this class owns short-press Confirm.
    /// </summary>
    public bool SuppressBoardConfirm { get; private set; }

    public void Clear()
    {
        _source = HoldSource.None;
        _unitId = 0;
        _cell = default;
        _heldSeconds = 0f;
        _previewActive = false;
        _sawPreview = false;
        SuppressBoardConfirm = false;
    }

    /// <summary>
    /// Call each frame while board input is enabled. May request a deferred Confirm on short release.
    /// </summary>
    public void Tick(
        GameplaySession session,
        BoardInputController boardInput,
        IGameCommandSource commands,
        IPointerSource pointer,
        float elapsedSeconds,
        bool boardInputEnabled,
        out bool deferredConfirm)
    {
        deferredConfirm = false;
        SuppressBoardConfirm = false;

        if (!boardInputEnabled)
        {
            Clear();
            return;
        }

        TickCommandHold(session, commands, elapsedSeconds, ref deferredConfirm);
        TickPointerHold(session, boardInput, pointer, elapsedSeconds);
    }

    private void TickCommandHold(
        GameplaySession session,
        IGameCommandSource commands,
        float elapsedSeconds,
        ref bool deferredConfirm)
    {
        if (_source == HoldSource.Pointer)
            return;

        if (commands.WasPressed(GameCommand.Confirm)
            && TryGetEnemyAtCursor(session, out var enemy))
        {
            BeginHold(HoldSource.Command, enemy);
            SuppressBoardConfirm = true;
            return;
        }

        if (_source != HoldSource.Command)
            return;

        SuppressBoardConfirm = true;

        if (!commands.IsPressed(GameCommand.Confirm))
        {
            if (!_sawPreview
                && TryGetEnemyAtCursor(session, out var releaseEnemy)
                && releaseEnemy.Id == _unitId)
            {
                deferredConfirm = true;
            }

            Clear();
            return;
        }

        UpdateHoldTargetFromCursor(session, elapsedSeconds);
    }

    private void TickPointerHold(
        GameplaySession session,
        BoardInputController boardInput,
        IPointerSource pointer,
        float elapsedSeconds)
    {
        if (_source == HoldSource.Command)
            return;

        var layout = session.Scene.Layout;
        if (pointer.WasPrimaryPressed
            && layout.TryScreenToCell(pointer.Position, out var pressX, out var pressY))
        {
            var pressCell = new GridCell(pressX, pressY);
            if (TryGetEnemyAt(session.State, pressCell, out var enemy))
            {
                BeginHold(HoldSource.Pointer, enemy);
                session.Cursor.MoveTo(pressCell);
            }
        }

        if (_source != HoldSource.Pointer)
            return;

        if (boardInput.IsPrimaryGesturePanning)
        {
            Clear();
            return;
        }

        if (!pointer.IsPrimaryDown)
        {
            if (_previewActive)
                boardInput.CancelPointerGesture();
            Clear();
            return;
        }

        // Follow cell under cursor/pointer so drag across enemies can retarget.
        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            session.Cursor.MoveTo(new GridCell(cellX, cellY));

        UpdateHoldTargetFromCursor(session, elapsedSeconds);

        if (_previewActive)
        {
            SuppressBoardConfirm = true;
            boardInput.CancelPointerGesture();
        }
    }

    /// <summary>
    /// Retarget when the cursor moves onto another enemy; hide preview on empty cells without ending the hold.
    /// </summary>
    private void UpdateHoldTargetFromCursor(GameplaySession session, float elapsedSeconds)
    {
        if (!TryGetEnemyAtCursor(session, out var underCursor))
        {
            _previewActive = false;
            return;
        }

        if (underCursor.Id != _unitId)
        {
            var keepPreview = _previewActive || _heldSeconds >= HoldSeconds || _sawPreview;
            _unitId = underCursor.Id;
            _cell = underCursor.Cell;
            if (keepPreview)
            {
                _previewActive = true;
                _sawPreview = true;
                _heldSeconds = HoldSeconds;
            }
            else
            {
                _heldSeconds = 0f;
                _previewActive = false;
            }

            return;
        }

        _cell = underCursor.Cell;
        _heldSeconds += elapsedSeconds;
        if (_heldSeconds < HoldSeconds)
            return;

        _previewActive = true;
        _sawPreview = true;
    }

    private void BeginHold(HoldSource source, MatchUnit enemy)
    {
        _source = source;
        _unitId = enemy.Id;
        _cell = enemy.Cell;
        _heldSeconds = 0f;
        _previewActive = false;
        _sawPreview = false;
    }

    private static bool TryGetEnemyAtCursor(GameplaySession session, out MatchUnit enemy) =>
        TryGetEnemyAt(session.State, session.Cursor.Cell, out enemy);

    private static bool TryGetEnemyAt(MatchState match, GridCell cell, out MatchUnit enemy)
    {
        if (!match.TryGetUnitAt(cell, out enemy))
            return false;
        return enemy.PlayerIndex != match.CurrentPlayer;
    }
}
