using Microsoft.Xna.Framework;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Id/Title wizard before creating a user units, buildings or theme module.</summary>
public sealed class EditorNewContentTypeModuleScreen : MenuScreen
{
    private readonly EditorWorkspaceSession? _session;
    private readonly ContentModuleType _moduleType;
    private readonly string _defaultModuleId;
    private readonly string _defaultTitle;
    private readonly EditorWorkspaceService _workspace;
    private readonly EditorNewContentTypeModuleView _view = new();
    private ContentTypeModuleWriter? _moduleWriter;

    private bool _pendingGoToHub;
    private bool _pendingCreate;

    public EditorNewContentTypeModuleScreen(
        GameMain game,
        EditorWorkspaceSession? session,
        ContentModuleType moduleType)
        : base(game)
    {
        _session = session;
        (_defaultModuleId, _defaultTitle) = moduleType switch
        {
            ContentModuleType.Units => ("user_units", "User Units"),
            ContentModuleType.Buildings => ("user_buildings", "User Buildings"),
            ContentModuleType.Theme => ("user_theme", "User Theme"),
            _ => throw new ArgumentOutOfRangeException(nameof(moduleType), "Unsupported module type for this wizard."),
        };
        _moduleType = moduleType;
        _workspace = new EditorWorkspaceService(game.Files, game.UserDataPaths);
    }

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _moduleWriter = new ContentTypeModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);

        var defaultId = _workspace.AllocateModuleId(_defaultModuleId);
        var defaultTitle = defaultId == _defaultModuleId ? _defaultTitle : _defaultTitle + " (" + defaultId + ")";
        _view.Build(_moduleType, defaultId, defaultTitle, RequestCreate, RequestGoToHub);
    }

    protected override void OnUnload()
    {
        _view.Clear();
        _moduleWriter = null;
        _pendingGoToHub = false;
        _pendingCreate = false;
    }

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (_view.IsTextEntryActive)
            return;

        if (WasLeavePressed())
            RequestGoToHub();

        if (_pendingCreate)
        {
            _pendingCreate = false;
            CreateModule();
            return;
        }

        if (_pendingGoToHub)
        {
            _pendingGoToHub = false;
            GoToHub();
        }
    }

    private void RequestGoToHub() => _pendingGoToHub = true;

    private void RequestCreate() => _pendingCreate = true;

    private void CreateModule()
    {
        ArgumentNullException.ThrowIfNull(_moduleWriter);

        try
        {
            var moduleId = _workspace.AllocateModuleId(EditorIds.SanitizeOrDefault(_view.ModuleId, _defaultModuleId));
            var title = string.IsNullOrWhiteSpace(_view.ModuleTitle) ? moduleId : _view.ModuleTitle.Trim();

            var root = _moduleWriter.CreateNew(
                _moduleType,
                moduleId,
                title,
                description: "Created in TinyTBS Editor.");

            var session = new EditorWorkspaceSession(
                moduleId,
                root,
                _moduleType,
                title);
            Navigator.ToEditorHub(session);
        }
        catch (Exception exception) when (exception is EditorException or TinymodInstallException or IOException)
        {
            _view.SyncStatus("Create failed: " + exception.Message);
        }
    }

    private void GoToHub() =>
        Navigator.ToEditorHub(_session);
}
