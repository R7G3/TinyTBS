using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Presentation.LoadGame;

/// <summary>Detail card over Load Game: Load / Delete / Close.</summary>
internal sealed class LoadGameDetailOverlay
{
    private readonly GumModalActionOverlay _overlay = new()
    {
        ScrimColor = UiColors.MenuDetailScrim,
        CardColor = UiColors.MenuPanel,
    };

    public bool IsOpen => _overlay.IsOpen;

    public void Open(
        Panel rootPanel,
        string titleText,
        string metaText,
        Action onLoad,
        Action onDelete,
        Action onClosed)
    {
        ArgumentNullException.ThrowIfNull(onLoad);
        ArgumentNullException.ThrowIfNull(onDelete);

        _overlay.Open(
            rootPanel,
            titleText,
            metaText,
            bodyLine: null,
            [
                ("Load", onLoad),
                ("Delete", onDelete),
            ],
            dismissLabel: "Close",
            onClosed);
    }

    public bool TryClose() => _overlay.TryClose();

    public void Close(bool notifyClosed = true) => _overlay.Close(notifyClosed);

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds) =>
        _overlay.HandleInput(commands, elapsedSeconds);
}
