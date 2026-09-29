namespace TinyTBS.Game.Editor.ViewModels;

/// <summary>UI-state for the Editor hub screen.</summary>
public sealed class EditorHubViewModel
{
    public string Title { get; set; } = "Editor";

    public string StatusText { get; set; } = string.Empty;

    public IReadOnlyList<EditorModuleRowViewModel> Modules { get; set; } = [];

    public IReadOnlyList<string> Maps { get; set; } = [];

    public IReadOnlyList<string> Levels { get; set; } = [];

    public string? OpenModuleId { get; set; }

    public string? OpenModuleTitle { get; set; }

    public bool CanPublish { get; set; }

    public bool CanCreateMap { get; set; }

    public bool CanEditScenarioContent => CanCreateMap;

    public bool HasOpenModule => !string.IsNullOrWhiteSpace(OpenModuleId);
}
