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
        EditorWorkspaceSession session,
        EditableLevelDocument document,
        bool isNew)
        : base(game)
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
        var maps = Workspace.ListMapIds(_session);
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
            var id = EditorIds.SanitizeOrDefault(_document.Id, "level");
            if (_isNew)
                id = Workspace.AllocateLevelId(_session, id);
            _document.Id = id;

            ContentModuleManifestParser.ValidateModuleId(_document.MapIdFromRef());
            _writer.Write(_session.ModuleRootPath, _document);
            _view.SyncStatus("Saved Levels/" + _document.Id);
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private void GoToHub() =>
        Navigator.ToEditorHub(_session);
}
