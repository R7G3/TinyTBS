using Gum;
using Gum.Forms.Controls;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Modal detail card over the content library shell (module/bundle info, optional Remove).</summary>
internal sealed class ContentLibraryDetailOverlay
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
        string bodyText,
        Action? removeAction,
        Action onClosed)
    {
        var actions = new List<(string Label, Action Activate)>();
        if (removeAction is not null)
            actions.Add(("Remove", removeAction));

        _overlay.Open(
            rootPanel,
            titleText,
            metaText,
            bodyText,
            actions,
            dismissLabel: "Close",
            onClosed,
            initialFocusIndex: actions.Count);
    }

    public bool TryClose() => _overlay.TryClose();

    public void Close(bool notifyClosed = true) => _overlay.Close(notifyClosed);

    public void HandleInput(IGameCommandSource commands, float elapsedSeconds) =>
        _overlay.HandleInput(commands, elapsedSeconds);
}
