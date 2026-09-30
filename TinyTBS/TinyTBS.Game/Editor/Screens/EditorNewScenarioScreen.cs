using Microsoft.Xna.Framework;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Screens;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Id/Title wizard before creating a user scenario module.</summary>
public sealed class EditorNewScenarioScreen : MenuScreen
{
    private const string DefaultModuleId = "user_scenario";

    private readonly EditorWorkspaceSession? _session;
    private readonly EditorWorkspaceService _workspace;
    private readonly EditorNewScenarioView _view = new();
    private ScenarioModuleWriter? _scenarioWriter;

    /// <summary>
    /// Navigate only after Gum.Update finishes — building Hub mid-Click would place
    /// Hub's Back under the cursor and fire GoToMainMenu (same for leftover Confirm).
    /// </summary>
    private bool _pendingGoToHub;
    private bool _pendingCreate;

    public EditorNewScenarioScreen(
        GameMain game,
        EditorWorkspaceSession? session)
        : base(game)
    {
        _session = session;
        _workspace = new EditorWorkspaceService(game.Files, game.UserDataPaths);
    }

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _scenarioWriter = new ScenarioModuleWriter(TinyGame.Files, TinyGame.UserDataPaths);

        var defaultId = _workspace.AllocateModuleId(DefaultModuleId);
        var defaultTitle = defaultId == DefaultModuleId
            ? "User Scenario"
            : "User Scenario (" + defaultId + ")";
        _view.Build(defaultId, defaultTitle, RequestCreate, RequestGoToHub);
    }

    protected override void OnUnload()
    {
        _view.Clear();
        _scenarioWriter = null;
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
            CreateScenario();
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

    private void CreateScenario()
    {
        ArgumentNullException.ThrowIfNull(_scenarioWriter);

        try
        {
            var moduleId = _workspace.AllocateModuleId(EditorIds.SanitizeOrDefault(_view.ModuleId, DefaultModuleId));
            var title = _view.ModuleTitle;

            var root = _scenarioWriter.CreateNew(
                moduleId,
                ref title,
                description: "Created in TinyTBS Editor.");

            var session = new EditorWorkspaceSession(
                moduleId,
                root,
                ContentModuleType.Scenario,
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
