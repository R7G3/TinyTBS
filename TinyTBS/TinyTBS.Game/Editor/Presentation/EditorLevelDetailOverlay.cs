using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Level row detail: Open / Delete / Back.</summary>
public sealed class EditorLevelDetailOverlay
{
    private readonly GumModalActionOverlay _overlay = new()
    {
        ScrimColor = UiColors.MenuDetailScrim,
        CardColor = UiColors.EditorPanel,
    };

    public bool IsOpen => _overlay.IsOpen;

    public void Open(
        Panel rootPanel,
        string levelId,
        Action onOpen,
        Action onDelete,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(onOpen);
        ArgumentNullException.ThrowIfNull(onDelete);

        _overlay.Open(
            rootPanel,
            "Level: " + levelId,
            metaLine: "Levels/" + levelId + "/",
            bodyLine: "Open to edit. Delete removes the level folder.",
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
