using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Levels;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit a level.json under the open scenario.</summary>
public sealed class EditorLevelEditScreen : EditorFormScreen
{
    private readonly EditorWorkspaceSession _session;
    private readonly EditableLevelDocument _document;
    private readonly bool _isNew;
    private readonly EditorLevelEditView _view = new();
    private LevelDocumentWriter? _writer;

    public EditorLevelEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        EditableLevelDocument document,
        bool isNew)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNew = isNew;
    }

    protected override bool IsOverlayOpen => false;

    protected override bool IsTextEntryActive => _view.IsTextEntryActive;

    protected override void OnFormLoad()
    {
        _writer = new LevelDocumentWriter(TinyGame.Files);
        var maps = ListMapIds(_session.ModuleRootPath);
        if (maps.Count > 0 && (_isNew || string.IsNullOrWhiteSpace(_document.MapIdFromRef())))
            _document.SetMapId(maps[0]);
        _view.Build(_document, maps, Save, GoToHub);
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
        _view.HandleInput(commands, pointer, elapsedSeconds);
    }

    protected override void OnBackRequested() => GoToHub();

    private void Save()
    {
        if (_writer is null)
            return;

        try
        {
            _view.ApplyTextFields();
            var id = SanitizeId(_document.Id);
            _document.Id = id;
            if (_isNew)
            {
                var existing = TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", id);
                if (Directory.Exists(existing))
                    id = AllocateLevelId(id);
                _document.Id = id;
            }

            ContentModuleManifestParser.ValidateModuleId(_document.MapIdFromRef());
            _writer.Write(_session.ModuleRootPath, _document);
            _view.SyncStatus("Saved Levels/" + _document.Id);
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "level" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "level";
        }
    }

    private string AllocateLevelId(string stem)
    {
        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!Directory.Exists(TinyGame.Files.Combine(_session.ModuleRootPath, "Levels", candidate)))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique level id.");
    }

    private static IReadOnlyList<string> ListMapIds(string moduleRoot)
    {
        var mapsRoot = Path.Combine(moduleRoot, "Maps");
        if (!Directory.Exists(mapsRoot))
            return [];

        return Directory.GetDirectories(mapsRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
