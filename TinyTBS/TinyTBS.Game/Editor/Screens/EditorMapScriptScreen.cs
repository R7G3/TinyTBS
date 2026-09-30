using System.Text;
using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Map;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Edit <c>Maps/{mapId}/script.cs</c> for the open map document.</summary>
public sealed class EditorMapScriptScreen : MenuScreen
{
    private readonly EditorWorkspaceSession _session;
    private readonly string _mapId;
    private readonly bool _returnToPaint;
    private readonly EditorMapScriptView _view = new();
    private string _scriptPath = string.Empty;

    public EditorMapScriptScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        string mapId,
        bool returnToPaint)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _mapId = string.IsNullOrWhiteSpace(mapId) ? "map" : mapId.Trim();
        _returnToPaint = returnToPaint;
    }

    protected override void OnLoad()
    {
        var mapRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps", _mapId);
        Directory.CreateDirectory(mapRoot);
        _scriptPath = TinyGame.Files.Combine(mapRoot, "script.cs");
        var text = File.Exists(_scriptPath)
            ? File.ReadAllText(_scriptPath)
            : MapScriptTemplates.EmptyHooks;
        _view.Build(_mapId, text, Save, ApplyTemplate, GoBack);
    }

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);
        if (_view.IsTextEntryActive)
            return;

        if (WasLeavePressed())
            GoBack();
    }

    private void ApplyTemplate()
    {
        _view.SetScriptText(MapScriptTemplates.EmptyHooks);
        _view.SyncStatus("Template applied (not saved yet).");
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_scriptPath, _view.ScriptText, Encoding.UTF8);
            _view.SyncStatus("Saved script.cs");
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private void GoBack()
    {
        if (_returnToPaint)
        {
            try
            {
                var mapRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps", _mapId);
                var definition = MapFolderLoader.Load(mapRoot, TinyGame.Files);
                var document = EditableMapDocument.FromDefinition(definition);
                ScreenManager.ReplaceScreen(
                    new EditorMapPaintScreen(TinyGame, Assets, _session, document, isNewMap: false));
                return;
            }
            catch (Exception exception)
            {
                GameLog.Warning($"Map '{_mapId}' could not be reopened for painting; returning to the hub.", exception);
            }
        }

        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
    }
}
