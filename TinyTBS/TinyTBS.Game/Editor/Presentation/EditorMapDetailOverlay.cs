using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Map row detail: Open / Delete / Back (same pattern as Content / Load Game).</summary>
public sealed class EditorMapDetailOverlay
{
    private readonly GumModalActionOverlay _overlay = new()
    {
        ScrimColor = UiColors.MenuDetailScrim,
        CardColor = UiColors.EditorPanel,
    };

    public bool IsOpen => _overlay.IsOpen;

    public void Open(
        Panel rootPanel,
        string mapId,
        Action onOpen,
        Action onDelete,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(onOpen);
        ArgumentNullException.ThrowIfNull(onDelete);

        _overlay.Open(
            rootPanel,
            "Map: " + mapId,
            metaLine: "Maps/" + mapId + "/",
            bodyLine: "Open to paint. Delete removes the map folder (and matching level stub if any).",
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
