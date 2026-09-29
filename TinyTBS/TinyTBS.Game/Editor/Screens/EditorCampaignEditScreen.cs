using Gum;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Screens;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.Menu;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create/edit Campaign/campaign.json for the open scenario.</summary>
public sealed class EditorCampaignEditScreen : GameScreen
{
    private readonly IAssetResolver _assets;
    private readonly EditorWorkspaceSession _session;
    private readonly EditorCampaignEditView _view = new();
    private MainMenuBackground? _background;
    private CampaignDocumentWriter? _writer;

    public EditorCampaignEditScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession session)
        : base(game)
    {
        _assets = assets;
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    private GameMain TinyGame => (GameMain)Game;

    public override void LoadContent()
    {
        base.LoadContent();
        _writer = new CampaignDocumentWriter(TinyGame.Files);
        _background = MainMenuBackground.Load(GraphicsDevice, Content, _assets);

        var existing = CampaignLoader.TryLoadFromScenario(
            _session.ModuleRootPath,
            campaignRelativePath: null,
            TinyGame.Files);

        var campaignId = existing?.CampaignId ?? SanitizeId(_session.ModuleId + "_campaign");
        var title = existing?.Title ?? _session.Title + " Campaign";
        var chapters = existing?.Chapters.Select(chapter => chapter.LevelId).ToArray() ?? [];
        var levels = ListLevelIds(_session.ModuleRootPath);

        _view.Build(campaignId, title, chapters, levels, Save, GoToHub);
    }

    public override void UnloadContent()
    {
        _view.Clear();
        _background?.Dispose();
        _background = null;
        _writer = null;
        base.UnloadContent();
    }

    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _view.HandleInput(TinyGame.Commands, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_view.IsChoiceOpen)
        {
            if (TinyGame.Commands.WasPressed(GameCommand.Back)
                || TinyGame.Commands.WasPressed(GameCommand.Cancel)
                || TinyGame.Commands.WasPressed(GameCommand.Info)
                || TinyGame.Commands.WasPressed(GameCommand.Pause))
            {
                _view.TryCloseChoice();
            }

            return;
        }

        if (_view.IsTextEntryActive)
            return;

        if (TinyGame.Commands.WasPressed(GameCommand.Back)
            || TinyGame.Commands.WasPressed(GameCommand.Cancel)
            || TinyGame.Commands.WasPressed(GameCommand.Info)
            || TinyGame.Commands.WasPressed(GameCommand.Pause))
        {
            GoToHub();
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

    private void Save()
    {
        if (_writer is null)
            return;

        try
        {
            if (_view.Chapters.Count == 0)
            {
                _view.SyncStatus("Add at least one chapter level before Save.");
                return;
            }

            var id = SanitizeId(_view.CampaignId);
            _writer.Write(
                _session.ModuleRootPath,
                id,
                _view.CampaignTitle,
                _view.Chapters);
            _view.SyncStatus("Saved Campaign/campaign.json (" + _view.Chapters.Count + " chapters).");
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "campaign" : raw.Trim().Replace(' ', '_');
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "campaign";
        }
    }

    private static IReadOnlyList<string> ListLevelIds(string moduleRoot)
    {
        var levelsRoot = Path.Combine(moduleRoot, "Levels");
        if (!Directory.Exists(levelsRoot))
            return [];

        return Directory.GetDirectories(levelsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, _assets, _session));
}
