namespace TinyTBS.Game.ViewModels;

/// <summary>
/// Which match overlay is open. Minimap and goals opened from the pause menu return to it when closed;
/// any other overlay replaces the whole stack. The match result replaces everything and cannot be closed.
/// </summary>
public sealed class MatchOverlayStack
{
    private readonly Stack<MatchOverlay> _stack = new();

    public MatchOverlay Top => _stack.Count == 0 ? MatchOverlay.None : _stack.Peek();

    /// <summary>Any overlay (including the match result) blocks board input and captures gamepad Confirm.</summary>
    public bool IsAnyOpen => _stack.Count > 0;

    /// <summary>Read-only overlays closed by Confirm, Back or any click.</summary>
    public bool IsInfoOverlayOnTop => Top is MatchOverlay.Minimap or MatchOverlay.Goals or MatchOverlay.TileDetail;

    public void Open(MatchOverlay overlay)
    {
        if (overlay == MatchOverlay.None || Top == MatchOverlay.MatchResult || Top == overlay)
            return;

        var opensOverPause = Top == MatchOverlay.Pause && overlay is (MatchOverlay.Minimap or MatchOverlay.Goals);
        if (!opensOverPause)
            _stack.Clear();

        _stack.Push(overlay);
    }

    /// <summary>Closes the top overlay and reveals the one below it; false when there is nothing closable.</summary>
    public bool CloseTop()
    {
        if (Top is MatchOverlay.None or MatchOverlay.MatchResult)
            return false;

        _stack.Pop();
        return true;
    }

    /// <summary>Closes every overlay except the match result.</summary>
    public void CloseAll()
    {
        if (Top != MatchOverlay.MatchResult)
            _stack.Clear();
    }
}
