using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Small action menu (Add / Move / Cancel) over editor forms.</summary>
public sealed class EditorChoiceOverlay
{
    private readonly GumModalActionOverlay _overlay = new()
    {
        ScrimColor = UiColors.MenuDetailScrim,
        CardColor = UiColors.EditorPanel,
    };

    public bool IsOpen => _overlay.IsOpen;

    public void Open(
        Panel rootPanel,
        string title,
        IReadOnlyList<(string Label, Action Activate)> actions,
        Action onClosed)
    {
        _overlay.Open(
            rootPanel,
            title,
            metaLine: null,
            bodyLine: null,
            actions,
            dismissLabel: "Cancel",
            onClosed,
            preferVerticalActions: actions.Count > 4);
    }

    public bool TryClose() => _overlay.TryClose();

    public void Close(bool notifyClosed = true) => _overlay.Close(notifyClosed);

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds) =>
        _overlay.HandleInput(commands, elapsedSeconds);
}
