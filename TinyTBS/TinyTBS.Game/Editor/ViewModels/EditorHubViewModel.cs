using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.ViewModels;

/// <summary>UI-state for the Editor hub screen.</summary>
public sealed class EditorHubViewModel
{
    public string Title { get; set; } = "Editor";

    public string StatusText { get; set; } = string.Empty;

    public IReadOnlyList<EditorModuleRowViewModel> Modules { get; set; } = [];

    public IReadOnlyList<string> Maps { get; set; } = [];

    public IReadOnlyList<string> Levels { get; set; } = [];

    public IReadOnlyList<string> Units { get; set; } = [];

    public IReadOnlyList<string> Buildings { get; set; } = [];

    public IReadOnlyList<EditorBundleRowViewModel> Bundles { get; set; } = [];

    public string? OpenModuleId { get; set; }

    public string? OpenModuleTitle { get; set; }

    public ContentModuleType? OpenModuleType { get; set; }

    public bool CanPublish { get; set; }

    public bool CanCreateMap { get; set; }

    public bool CanEditScenarioContent => CanCreateMap;

    public bool CanEditUnits => OpenModuleType == ContentModuleType.Units;

    public bool CanEditBuildings => OpenModuleType == ContentModuleType.Buildings;

    public bool CanEditTheme => OpenModuleType == ContentModuleType.Theme;

    /// <summary>Bundles are edited outside an open module session (root hub / scenario view).</summary>
    public bool CanEditBundles =>
        !CanEditUnits && !CanEditBuildings && !CanEditTheme;

    public bool CanExportModule => HasOpenModule;

    public bool HasOpenModule => !string.IsNullOrWhiteSpace(OpenModuleId);
}
