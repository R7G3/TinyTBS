namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state for the content library screen.</summary>
public sealed class ContentLibraryViewModel
{
    public string Title { get; set; } = "Content";

    public string StatusText { get; set; } = string.Empty;

    public ContentLibraryTab ActiveTab { get; set; } = ContentLibraryTab.Modules;

    public IReadOnlyList<ContentModuleRowViewModel> Modules { get; set; } = [];

    public IReadOnlyList<ContentBundleRowViewModel> Bundles { get; set; } = [];

    public IReadOnlyList<ContentPendingInstallViewModel> PendingInstalls { get; set; } = [];

    /// <summary>Online catalog install (not wired yet).</summary>
    public bool CanInstallFromCatalog { get; set; }

    public bool CanDownload { get; set; }

    public bool CanUpdate { get; set; }
}
