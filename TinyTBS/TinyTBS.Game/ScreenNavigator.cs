using MonoGame.Extended.Screens;
using TinyTBS.Game.Editor.Buildings;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Screens;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.Screens;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game;

/// <summary>
/// The only place that constructs screens. Callers say where to go; they do not <c>new</c> a screen.
/// </summary>
public sealed class ScreenNavigator
{
    private readonly GameMain _game;
    private readonly ScreenManager _screens;

    public ScreenNavigator(GameMain game, ScreenManager screens)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
    }

    public void ShowMainMenuFirst() => _screens.ShowScreen(new MainMenuScreen(_game));

    public void ToMainMenu() => _screens.ReplaceScreen(new MainMenuScreen(_game));

    public void ToNewGame() => _screens.ReplaceScreen(new NewGameScreen(_game));

    public void ToLoadGame() => _screens.ReplaceScreen(new LoadGameScreen(_game));

    public void ToContentLibrary() => _screens.ReplaceScreen(new ContentLibraryScreen(_game));

    public void ToAbout() => _screens.ReplaceScreen(new AboutScreen(_game));

    public void StartMatch(MatchStartRequest request) =>
        _screens.ReplaceScreen(new LoadingScreen(_game, request));

    public void ShowMatch(GameplaySession session) =>
        _screens.ReplaceScreen(new GameplayScreen(_game, session));

    public void ToEditorHub(EditorWorkspaceSession? session = null) =>
        _screens.ReplaceScreen(new EditorHubScreen(_game, session));

    public void ToEditorNewScenario(EditorWorkspaceSession? session) =>
        _screens.ReplaceScreen(new EditorNewScenarioScreen(_game, session));

    public void ToEditorNewContentType(EditorWorkspaceSession? session, ContentModuleType type) =>
        _screens.ReplaceScreen(new EditorNewContentTypeModuleScreen(_game, session, type));

    public void ToEditorTheme(EditorWorkspaceSession session) =>
        _screens.ReplaceScreen(new EditorThemeEditScreen(_game, session));

    public void ToEditorBundle(EditorWorkspaceSession? session, EditableBundleDocument document, bool isNew) =>
        _screens.ReplaceScreen(new EditorBundleEditScreen(_game, session, document, isNew));

    public void ToEditorNewMap(EditorWorkspaceSession session) =>
        _screens.ReplaceScreen(new EditorNewMapScreen(_game, session));

    public void ToEditorMapPaint(EditorWorkspaceSession session, EditableMapDocument document, bool isNewMap) =>
        _screens.ReplaceScreen(new EditorMapPaintScreen(_game, session, document, isNewMap));

    public void ToEditorMapScript(EditorWorkspaceSession session, string mapId, bool returnToPaint) =>
        _screens.ReplaceScreen(new EditorMapScriptScreen(_game, session, mapId, returnToPaint));

    public void ToEditorLevel(EditorWorkspaceSession session, EditableLevelDocument document, bool isNew) =>
        _screens.ReplaceScreen(new EditorLevelEditScreen(_game, session, document, isNew));

    public void ToEditorCampaign(EditorWorkspaceSession session) =>
        _screens.ReplaceScreen(new EditorCampaignEditScreen(_game, session));

    public void ToEditorUnit(EditorWorkspaceSession session, EditableUnitDocument document, bool isNew) =>
        _screens.ReplaceScreen(new EditorUnitEditScreen(_game, session, document, isNew));

    public void ToEditorBuilding(EditorWorkspaceSession session, EditableBuildingDocument document, bool isNew) =>
        _screens.ReplaceScreen(new EditorBuildingEditScreen(_game, session, document, isNew));
}
