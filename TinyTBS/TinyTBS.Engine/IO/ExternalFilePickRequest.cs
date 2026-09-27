namespace TinyTBS.Engine.IO;

/// <summary>Parameters for <see cref="IExternalFilePicker.PickOpenFile"/>.</summary>
public sealed class ExternalFilePickRequest
{
    public string Title { get; init; } = "Open file";

    /// <summary>Human-readable filter label, e.g. "Tinymod modules".</summary>
    public string FilterName { get; init; } = "All files";

    /// <summary>
    /// Allowed extensions including the dot (e.g. <c>.tinymod.zip</c>, <c>.zip</c>).
    /// Empty means any file.
    /// </summary>
    public IReadOnlyList<string> AllowedExtensions { get; init; } = [];
}
