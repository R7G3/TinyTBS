using TinyTBS.Engine.Input;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Match;

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
        MatchState match,
        IGameCommandSource commands,
        IPointerSource pointer,
        MatchBoardLayout layout,
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

        TickCommandHold(match, commands, elapsedSeconds, ref deferredConfirm);
        TickPointerHold(match, pointer, layout, elapsedSeconds);
    }

    private void TickCommandHold(
        MatchState match,
        IGameCommandSource commands,
        float elapsedSeconds,
        ref bool deferredConfirm)
    {
        if (_source == HoldSource.Pointer)
            return;

        if (commands.WasPressed(GameCommand.Confirm)
            && TryGetEnemyAtCursor(match, out var enemy))
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
                && TryGetEnemyAtCursor(match, out var releaseEnemy)
                && releaseEnemy.Id == _unitId)
            {
                deferredConfirm = true;
            }

            Clear();
            return;
        }

        UpdateHoldTargetFromCursor(match, elapsedSeconds);
    }

    private void TickPointerHold(
        MatchState match,
        IPointerSource pointer,
        MatchBoardLayout layout,
        float elapsedSeconds)
    {
        if (_source == HoldSource.Command)
            return;

        if (pointer.WasPrimaryPressed
            && layout.TryScreenToCell(pointer.Position, out var pressX, out var pressY))
        {
            var pressCell = new GridCell(pressX, pressY);
            if (TryGetEnemyAt(match, pressCell, out var enemy))
            {
                BeginHold(HoldSource.Pointer, enemy);
                match.HandlePointer(pressCell);
            }
        }

        if (_source != HoldSource.Pointer)
            return;

        if (MatchCommandApplicator.IsPrimaryGesturePanning)
        {
            Clear();
            return;
        }

        if (!pointer.IsPrimaryDown)
        {
            if (_previewActive)
                MatchCommandApplicator.CancelPointerGesture();
            Clear();
            return;
        }

        // Follow cell under cursor/pointer so drag across enemies can retarget.
        if (layout.TryScreenToCell(pointer.Position, out var cellX, out var cellY))
            match.HandlePointer(new GridCell(cellX, cellY));

        UpdateHoldTargetFromCursor(match, elapsedSeconds);

        if (_previewActive)
        {
            SuppressBoardConfirm = true;
            MatchCommandApplicator.CancelPointerGesture();
        }
    }

    /// <summary>
    /// Retarget when the cursor moves onto another enemy; hide preview on empty cells without ending the hold.
    /// </summary>
    private void UpdateHoldTargetFromCursor(MatchState match, float elapsedSeconds)
    {
        if (!TryGetEnemyAtCursor(match, out var underCursor))
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

    private static bool TryGetEnemyAtCursor(MatchState match, out MatchUnit enemy) =>
        TryGetEnemyAt(match, match.Cursor, out enemy);

    private static bool TryGetEnemyAt(MatchState match, GridCell cell, out MatchUnit enemy)
    {
        if (!match.TryGetUnitAt(cell, out enemy))
            return false;
        return enemy.PlayerIndex != match.CurrentPlayer;
    }
}
