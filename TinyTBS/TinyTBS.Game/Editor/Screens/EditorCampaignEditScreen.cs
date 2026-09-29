using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create/edit Campaign/campaign.json for the open scenario.</summary>
public sealed class EditorCampaignEditScreen : EditorFormScreen
{
    private readonly EditorWorkspaceSession _session;
    private readonly EditorCampaignEditView _view = new();
    private CampaignDocumentWriter? _writer;

    public EditorCampaignEditScreen(GameMain game, IAssetResolver assets, EditorWorkspaceSession session)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    protected override bool IsOverlayOpen => _view.IsChoiceOpen;

    protected override bool IsTextEntryActive => _view.IsTextEntryActive;

    protected override void OnFormLoad()
    {
        _writer = new CampaignDocumentWriter(TinyGame.Files);

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
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
