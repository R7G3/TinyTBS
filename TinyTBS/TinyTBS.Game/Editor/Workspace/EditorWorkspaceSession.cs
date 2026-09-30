using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Editor.Workspace;

/// <summary>
/// In-place edit session for one module folder under user <c>Content/Modules</c>.
/// </summary>
public sealed class EditorWorkspaceSession
{
    public EditorWorkspaceSession(
        string moduleId,
        string moduleRootPath,
        ContentModuleType type,
        string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        ModuleId = moduleId.Trim();
        ModuleRootPath = moduleRootPath;
        Type = type;
        Title = title.Trim();
    }

    public string ModuleId { get; }

    public string ModuleRootPath { get; }

    public ContentModuleType Type { get; }

    public string Title { get; }

    /// <summary>True after local edits not yet written (slice 2+).</summary>
    public bool IsDirty { get; set; }
}
