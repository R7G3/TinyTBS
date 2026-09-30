using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Themes;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Engine.Input;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Edit theme module.json (paths + sprite remaps).</summary>
public sealed class EditorThemeEditScreen : EditorFormScreen
{
    private readonly EditorWorkspaceSession _session;
    private readonly EditorThemeEditView _view = new();
    private ThemeDocumentWriter? _writer;

    public EditorThemeEditScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession session)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        if (_session.Type != ContentModuleType.Theme)
            throw new ArgumentException("Session must be a theme module.", nameof(session));
    }

    protected override bool IsOverlayOpen => _view.IsOverlayOpen;

    protected override bool IsTextEntryActive => _view.IsTextEntryActive;

    protected override void OnFormLoad()
    {
        _writer = new ThemeDocumentWriter(TinyGame.Files);
        var document = EditableThemeDocument.Load(_session.ModuleRootPath, TinyGame.Files);
        _view.Build(document, Save, GoToHub);
    }

    protected override void OnFormUnload()
    {
        _view.Clear();
        _writer = null;
    }

    protected override void HandleFormInput(
        IGameCommandSource commands,
        IPointerSource pointer,
        float elapsedSeconds)
    {
        _view.HandleInput(commands, elapsedSeconds);
    }

    protected override void OnBackRequested() => GoToHub();

    private void Save()
    {
        if (_writer is null)
            return;

        try
        {
            _view.ApplyTextFields();
            _writer.Write(_session.ModuleRootPath, _view.Document);
            _view.SyncStatus("Saved module.json");
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
