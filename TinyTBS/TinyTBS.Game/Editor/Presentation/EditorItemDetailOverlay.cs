using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Generic Open / Delete / Back detail for hub library rows (unit/building types).</summary>
public sealed class EditorItemDetailOverlay
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
        string metaLine,
        Action onOpen,
        Action onDelete,
        Action onClosed)
    {
        _overlay.Open(
            rootPanel,
            title,
            metaLine,
            bodyLine: null,
            [
                ("Open", onOpen),
                ("Delete", onDelete),
            ],
            dismissLabel: "Back",
            onClosed);
    }

    public bool TryClose() => _overlay.TryClose();

    public void Close(bool notifyClosed = true) => _overlay.Close(notifyClosed);

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds) =>
        _overlay.HandleInput(commands, elapsedSeconds);
}
