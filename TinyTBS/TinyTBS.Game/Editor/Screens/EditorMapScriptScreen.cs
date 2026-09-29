using System.Text;
using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Edit <c>Maps/{mapId}/script.cs</c> for the open map document.</summary>
public sealed class EditorMapScriptScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly string _mapId;
    private readonly bool _returnToPaint;
    private readonly EditorMapScriptView _view = new();
    private MainMenuBackground? _background;
    private string _scriptPath = string.Empty;

    public EditorMapScriptScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        string mapId,
        bool returnToPaint)
        : base(game)
    {
        _assets = assets;
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _mapId = string.IsNullOrWhiteSpace(mapId) ? "map" : mapId.Trim();
        _returnToPaint = returnToPaint;
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);
        var mapRoot = TinyGame.Files.Combine(_session.ModuleRootPath, "Maps", _mapId);
        Directory.CreateDirectory(mapRoot);
        _scriptPath = TinyGame.Files.Combine(mapRoot, "script.cs");
        var text = File.Exists(_scriptPath)
            ? File.ReadAllText(_scriptPath)
            : MapScriptTemplates.EmptyHooks;
        _view.Build(_mapId, text, Save, ApplyTemplate, GoBack);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_view.IsTextEntryActive)
            return;

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            GoBack();
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(24, 28, 38));
        var texture = _background?.Texture;
        if (texture is not null)
        {
            ViewportFit.DrawCentered(
                TinyGame.SharedSpriteBatch,
                texture,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                Color.White * 0.35f);
        }

        GumService.Default.Draw();
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
                var document = TinyTBS.Game.Editor.Map.EditableMapDocument.FromDefinition(definition);
                ScreenManager.ReplaceScreen(
                    new EditorMapPaintScreen(TinyGame, _assets, _session, document, isNewMap: false));
                return;
            }
            catch (Exception)
            {
                // Fall through to hub.
            }
        }

        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
    }
}
