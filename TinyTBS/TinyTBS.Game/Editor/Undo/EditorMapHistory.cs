using TinyTBS.Game.Editor.Map;

namespace TinyTBS.Game.Editor.Undo;

/// <summary>Snapshot Undo/Redo for <see cref="EditableMapDocument"/> (capacity ~100).</summary>
public sealed class EditorMapHistory
{
    public const int DefaultCapacity = 100;

    private readonly int _capacity;
    private readonly List<EditorMapSnapshot> _undo = [];
    private readonly List<EditorMapSnapshot> _redo = [];

    public EditorMapHistory(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>Call immediately before a mutating edit.</summary>
    public void RecordBeforeChange(EditableMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _undo.Add(document.CaptureSnapshot());
        while (_undo.Count > _capacity)
            _undo.RemoveAt(0);
        _redo.Clear();
    }

    /// <summary>Drop the last <see cref="RecordBeforeChange"/> when the edit was a no-op.</summary>
    public void DiscardLastRecord()
    {
        if (_undo.Count > 0)
            _undo.RemoveAt(_undo.Count - 1);
    }

    public bool Undo(EditableMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_undo.Count == 0)
            return false;

        _redo.Add(document.CaptureSnapshot());
        var snapshot = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        document.RestoreSnapshot(snapshot);
        return true;
    }

    public bool Redo(EditableMapDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_redo.Count == 0)
            return false;

        _undo.Add(document.CaptureSnapshot());
        while (_undo.Count > _capacity)
            _undo.RemoveAt(0);

        var snapshot = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        document.RestoreSnapshot(snapshot);
        return true;
    }
}
