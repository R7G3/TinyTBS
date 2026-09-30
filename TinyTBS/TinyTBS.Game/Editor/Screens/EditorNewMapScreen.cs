using Microsoft.Xna.Framework;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Size picker before opening the map paint screen.</summary>
public sealed class EditorNewMapScreen : MenuScreen
{
    private const string DefaultMapId = "map";

    private readonly EditorWorkspaceSession _session;
    private readonly EditorWorkspaceService _workspace;
    private readonly EditorNewMapView _view = new();

    public EditorNewMapScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession session)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _workspace = new EditorWorkspaceService(game.Files, game.UserDataPaths);
    }

    protected override void OnLoad()
    {
        var defaultMapId = _workspace.AllocateMapId(_session, DefaultMapId);
        _view.Build(_session.ModuleId, defaultMapId, StartPaint, GoToHub);
    }

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.HandleInput(TinyGame.Commands, TinyGame.Pointer, elapsedSeconds);

        if (_view.IsTextEntryActive)
            return;

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel))
        {
            GoToHub();
        }
    }

    private void StartPaint()
    {
        var mapId = _workspace.AllocateMapId(_session, EditorIds.SanitizeOrDefault(_view.MapId, DefaultMapId));
        var mapTitle = string.IsNullOrWhiteSpace(_view.MapTitle) ? mapId : _view.MapTitle.Trim();

        var document = EditableMapDocument.CreateFilled(
            mapId,
            title: mapTitle,
            _view.Width,
            _view.Height,
            terrainType: "grass");
        document.IsDirty = true;

        ScreenManager.ReplaceScreen(
            new EditorMapPaintScreen(TinyGame, Assets, _session, document, isNewMap: true));
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
