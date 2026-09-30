using Microsoft.Xna.Framework;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Input;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>
/// Shared editor form screen: menu background plus overlay / text-entry aware Back handling.
/// </summary>
public abstract class EditorFormScreen : MenuScreen
{
    protected EditorFormScreen(GameMain game)
        : base(game)
    {
        Workspace = new EditorWorkspaceService(game.Files, game.UserDataPaths);
    }

    protected EditorWorkspaceService Workspace { get; }

    protected abstract bool IsOverlayOpen { get; }

    protected abstract bool IsTextEntryActive { get; }

    protected sealed override void OnLoad() => OnFormLoad();

    protected sealed override void OnUnload() => OnFormUnload();

    protected sealed override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        var overlayWasOpen = IsOverlayOpen;
        HandleFormInput(TinyGame.Commands, TinyGame.Pointer, elapsedSeconds);

        if (overlayWasOpen || IsOverlayOpen || IsTextEntryActive)
            return;

        if (WasLeavePressed())
            OnBackRequested();
    }

    protected abstract void OnFormLoad();

    protected abstract void OnFormUnload();

    protected abstract void HandleFormInput(
        IGameCommandSource commands,
        IPointerSource pointer,
        float elapsedSeconds);

    protected abstract void OnBackRequested();
}
