using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Units;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit Units/{id}.json in an open units module.</summary>
public sealed class EditorUnitEditScreen : EditorFormScreen
{
    private readonly EditorWorkspaceSession _session;
    private readonly EditableUnitDocument _document;
    private readonly bool _isNew;
    private readonly EditorUnitEditView _view = new();
    private UnitDocumentWriter? _writer;

    public EditorUnitEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession session,
        EditableUnitDocument document,
        bool isNew)
        : base(game, assets)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNew = isNew;
    }

    protected override bool IsOverlayOpen => _view.IsOverlayOpen;

    protected override bool IsTextEntryActive => _view.IsTextEntryActive;

    protected override void OnFormLoad()
    {
        _writer = new UnitDocumentWriter(TinyGame.Files);
        _view.Build(_document, Save, GoToHub);
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
            ContentModuleManifestParser.ValidateModuleId(id);

            var unitsDir = TinyGame.Files.Combine(_session.ModuleRootPath, "Units");
            var targetPath = TinyGame.Files.Combine(unitsDir, id + ".json");
            var renamed = !_isNew
                && !string.Equals(id, _document.OriginalId, StringComparison.Ordinal);
            if ((_isNew || renamed) && File.Exists(targetPath))
            {
                id = AllocateUniqueUnitId(unitsDir, id);
                _document.Id = id;
            }

            _writer.Write(_session.ModuleRootPath, _document);
            _view.SyncIdentityFromDocument();
            _view.SyncStatus("Saved Units/" + _document.Id + ".json");
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "unit" : raw.Trim();
        try
        {
            ContentModuleManifestParser.ValidateModuleId(trimmed);
            return trimmed;
        }
        catch (TinymodInstallException)
        {
            return "unit";
        }
    }

    private string AllocateUniqueUnitId(string unitsDir, string stem)
    {
        for (var suffix = 2; suffix < 10_000; suffix++)
        {
            var candidate = stem + "_" + suffix;
            var path = TinyGame.Files.Combine(unitsDir, candidate + ".json");
            if (!File.Exists(path))
                return candidate;
        }

        throw new EditorException("Could not allocate a unique unit id.");
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
