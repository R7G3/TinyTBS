using Gum;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Bundles;
using TinyTBS.Game.Editor.Presentation;
using TinyTBS.Game.Editor.Workspace;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Input;
using TinyTBS.Game.Modules;
using MonoGame.Extended.Screens;

namespace TinyTBS.Game.Editor.Screens;

/// <summary>Create or edit a user <c>*.bundle.json</c> preset.</summary>
public sealed class EditorBundleEditScreen : EditorFormScreen
{
    private readonly EditorWorkspaceSession? _session;
    private readonly EditableBundleDocument _document;
    private readonly bool _isNew;
    private readonly EditorBundleEditView _view = new();
    private BundleDocumentWriter? _writer;
    private ContentModuleLibrary? _moduleLibrary;

    public EditorBundleEditScreen(
        GameMain game,
        IAssetResolver assets,
        EditorWorkspaceSession? session,
        EditableBundleDocument document,
        bool isNew)
        : base(game, assets)
    {
        _session = session;
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _isNew = isNew;
    }

    protected override bool IsOverlayOpen => _view.IsOverlayOpen;

    protected override bool IsTextEntryActive => _view.IsTextEntryActive;

    protected override void OnFormLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _writer = new BundleDocumentWriter(TinyGame.Files, TinyGame.UserDataPaths);
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        var modules = _moduleLibrary.ListEffectiveModules();
        _view.Build(_document, modules, _isNew, Save, GoToHub);
    }

    protected override void OnFormUnload()
    {
        _view.Clear();
        _writer = null;
        _moduleLibrary = null;
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
            _view.ApplyTextFields();
            var id = SanitizeId(_document.Id);
            _document.Id = id;

            if ((_isNew || !string.Equals(id, _document.OriginalId, StringComparison.Ordinal))
                && File.Exists(TinyGame.Files.Combine(
                    TinyGame.UserDataPaths.Bundles,
                    id + ContentBundleFiles.BundleJsonExtension)))
            {
                id = _writer.AllocateUniqueBundleId(id);
                _document.Id = id;
            }

            _writer.Write(_document);
            _view.SyncIdentityFromDocument();
            _view.SyncStatus("Saved Bundles/" + _document.Id + ContentBundleFiles.BundleJsonExtension);
        }
        catch (Exception exception)
        {
            _view.SyncStatus("Save failed: " + exception.Message);
        }
    }

    private static string SanitizeId(string raw)
    {
        var trimmed = string.IsNullOrWhiteSpace(raw) ? "user_bundle" : raw.Trim();
        if (trimmed.Contains("..", StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal)
            || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return "user_bundle";
        }

        return trimmed;
    }

    private void GoToHub() =>
        ScreenManager.ReplaceScreen(new EditorHubScreen(TinyGame, Assets, _session));
}
