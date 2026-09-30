using Microsoft.Xna.Framework;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Presentation.About;

namespace TinyTBS.Game.Screens;

/// <summary>Credits, project links, and solution library list.</summary>
public sealed class AboutScreen : MenuScreen
{
    private readonly AboutView _view = new();

    public AboutScreen(GameMain game, IAssetResolver assets)
        : base(game, assets)
    {
    }

    protected override void OnLoad() => _view.Build(GoToMainMenu, OpenUrl);

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (WasLeavePressed())
            GoToMainMenu();
    }

    private void OpenUrl(string url)
    {
        if (TinyGame.UriLauncher.TryOpen(url, out var error))
        {
            _view.SyncStatus(string.Empty);
            return;
        }

        _view.SyncStatus("Open link failed: " + (error ?? "unknown error"));
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));
}
