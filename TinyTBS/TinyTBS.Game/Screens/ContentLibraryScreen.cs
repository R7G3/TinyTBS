using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Editor.Writers;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Game.Presentation.Content;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Screens;

/// <summary>
/// Content library: modules/bundles, install from device (file picker) or queued Downloads,
/// uninstall user modules. Online catalog is reserved (greyed).
/// </summary>
public sealed class ContentLibraryScreen : MenuScreen
{
    private readonly ContentLibraryViewModel _viewModel = new();
    private readonly ContentLibraryView _view = new();

    private ContentModuleLibrary? _moduleLibrary;
    private ContentBundleLibrary? _bundleLibrary;
    private TinymodInstaller? _installer;

    public ContentLibraryScreen(GameMain game, IAssetResolver assets)
        : base(game, assets)
    {
    }

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _moduleLibrary = new ContentModuleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _bundleLibrary = new ContentBundleLibrary(TinyGame.Files, TinyGame.UserDataPaths);
        _installer = new TinymodInstaller(TinyGame.Files, TinyGame.UserDataPaths);

        _viewModel.ActiveTab = ContentLibraryTab.Modules;
        RefreshLibrary("Modules / Bundles / Install tabs. Confirm a row for details.");
    }

    protected override void OnUnload()
    {
        _view.Clear();
        _moduleLibrary = null;
        _bundleLibrary = null;
        _installer = null;
    }

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.ApplyResponsiveLayout();

        // If detail was open, HandleInput may close it on B/Esc — do not also run screen Back.
        var detailWasOpen = _view.IsDetailOpen;
        _view.HandleInput(TinyGame.Commands, elapsedSeconds);

        if (!WasLeavePressed())
            return;

        if (_view.IsDetailOpen)
            _view.TryCloseDetail();
        else if (!detailWasOpen)
            HandleBackCommand();
    }

    private void SelectTab(ContentLibraryTab tab)
    {
        _viewModel.ActiveTab = tab;
        var status = tab switch
        {
            ContentLibraryTab.Bundles => "Bundle presets. Confirm a row for modules inside.",
            ContentLibraryTab.Install =>
                "Install from device now; catalog later. Back returns to Modules.",
            _ => "Confirm a module for details. Remove only from the detail popup.",
        };
        RefreshLibrary(status);
    }

    private void RefreshLibrary(string statusText)
    {
        ArgumentNullException.ThrowIfNull(_moduleLibrary);
        ArgumentNullException.ThrowIfNull(_bundleLibrary);
        ArgumentNullException.ThrowIfNull(_installer);

        _viewModel.CanInstallFromCatalog = false;
        _viewModel.CanDownload = false;
        _viewModel.CanUpdate = false;
        _viewModel.StatusText = statusText;
        _viewModel.Modules = _moduleLibrary.ListEffectiveModules()
            .Select(ToModuleRow)
            .ToArray();
        _viewModel.Bundles = _bundleLibrary.ListEffectiveBundles()
            .Select(ToBundleRow)
            .ToArray();
        _viewModel.PendingInstalls = _installer.ListDownloadArchives()
            .Select(path => new ContentPendingInstallViewModel
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
            })
            .ToArray();

        _view.Build(
            _viewModel,
            onSelectTab: SelectTab,
            onUninstallModule: UninstallModule,
            onUninstallBundle: UninstallBundle,
            onPickInstallFromDevice: PickInstallFromDevice,
            onInstallArchive: InstallArchive,
            onBack: HandleBackCommand);
    }

    private void PickInstallFromDevice()
    {
        var pickedPath = TinyGame.FilePicker.PickOpenFile(new ExternalFilePickRequest
        {
            Title = "Install tinymod module",
            FilterName = "Tinymod modules",
            AllowedExtensions = [ContentModuleFiles.TinymodZipExtension],
        });

        if (string.IsNullOrWhiteSpace(pickedPath))
        {
            _view.SetStatus("Install cancelled.");
            return;
        }

        InstallArchive(pickedPath);
    }

    private void InstallArchive(string archivePath)
    {
        ArgumentNullException.ThrowIfNull(_installer);
        try
        {
            var installed = _installer.Install(archivePath);
            _viewModel.ActiveTab = ContentLibraryTab.Modules;
            RefreshLibrary($"Installed '{installed.ModuleId}' ({installed.Type}).");
        }
        catch (Exception exception)
        {
            GameLog.Error($"Installing '{archivePath}' failed.", exception);
            _view.SetStatus("Install failed: " + exception.Message);
        }
    }

    private void UninstallModule(string moduleId)
    {
        ArgumentNullException.ThrowIfNull(_installer);
        try
        {
            if (_installer.Uninstall(moduleId))
                RefreshLibrary($"Removed '{moduleId}'.");
            else
                _view.SetStatus($"Module '{moduleId}' is not in the user library.");
        }
        catch (Exception exception)
        {
            _view.SetStatus("Remove failed: " + exception.Message);
        }
    }

    private void UninstallBundle(string bundleId)
    {
        try
        {
            var writer = new BundleDocumentWriter(TinyGame.Files, TinyGame.UserDataPaths);
            if (writer.Delete(bundleId))
                RefreshLibrary($"Removed bundle '{bundleId}'.");
            else
                _view.SetStatus($"Bundle '{bundleId}' is not in the user library.");
        }
        catch (Exception exception)
        {
            _view.SetStatus("Remove bundle failed: " + exception.Message);
        }
    }

    private void HandleBackCommand()
    {
        if (_view.TryCloseDetail())
            return;

        // Install is a sub-view of Content: Back returns to Modules, not the main menu.
        if (_viewModel.ActiveTab == ContentLibraryTab.Install)
        {
            SelectTab(ContentLibraryTab.Modules);
            return;
        }

        GoToMainMenu();
    }

    private void GoToMainMenu() =>
        ScreenManager.ReplaceScreen(new MainMenuScreen(TinyGame, Assets));

    private static ContentModuleRowViewModel ToModuleRow(ContentModuleInfo module) =>
        new()
        {
            ModuleId = module.ModuleId,
            Title = module.Title,
            TypeLabel = module.Type.ToString().ToLowerInvariant(),
            Version = module.Version,
            Description = module.Description,
            SourceLabel = module.Source == ContentModuleSource.UserLibrary ? "user" : "bundled",
            CanUninstall = module.Source == ContentModuleSource.UserLibrary,
        };

    private static ContentBundleRowViewModel ToBundleRow(ContentBundleDefinition bundle) =>
        new()
        {
            BundleId = bundle.BundleId,
            Title = bundle.Title,
            SourceLabel = bundle.Source == ContentModuleSource.UserLibrary ? "user" : "bundled",
            ModulesSummary = string.Join(", ", bundle.ModuleIds),
            CanUninstall = bundle.Source == ContentModuleSource.UserLibrary,
        };
}
