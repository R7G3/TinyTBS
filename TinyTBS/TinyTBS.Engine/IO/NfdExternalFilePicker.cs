using NativeFileDialogNET;
using NfdDialogResult = NativeFileDialogNET.DialogResult;

namespace TinyTBS.Engine.IO;

/// <summary>Open-file via nativefiledialog-extended (Windows / macOS / Linux).</summary>
public sealed class NfdExternalFilePicker : IExternalFilePicker
{
    public string? PickOpenFile(ExternalFilePickRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var dialog = new NativeFileDialog().SelectFile();

        var filterName = string.IsNullOrWhiteSpace(request.FilterName)
            ? "Files"
            : request.FilterName.Trim();
        var extensions = request.AllowedExtensions
            .Select(NormalizeNfdExtension)
            .Where(extension => extension.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (extensions.Length > 0)
            dialog.AddFilter(filterName, string.Join(',', extensions));

        var result = dialog.Open(out string[]? paths, defaultPath: null);
        if (result != NfdDialogResult.Okay || paths is null || paths.Length == 0)
            return null;

        return paths[0];
    }

    private static string NormalizeNfdExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var trimmed = extension.Trim();
        if (trimmed.StartsWith("*.", StringComparison.Ordinal))
            trimmed = trimmed[2..];
        else if (trimmed.StartsWith(".", StringComparison.Ordinal))
            trimmed = trimmed[1..];

        return trimmed;
    }
}
