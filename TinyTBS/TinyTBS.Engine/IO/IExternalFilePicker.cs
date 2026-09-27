namespace TinyTBS.Engine.IO;

/// <summary>
/// Platform file-open UI (desktop dialog now; Android document picker later).
/// Game code must not open OS dialogs directly.
/// </summary>
public interface IExternalFilePicker
{
    /// <summary>
    /// Shows a native open-file UI. Returns the selected path, or <see langword="null"/> if cancelled
    /// or the platform cannot pick files yet.
    /// </summary>
    string? PickOpenFile(ExternalFilePickRequest request);
}
