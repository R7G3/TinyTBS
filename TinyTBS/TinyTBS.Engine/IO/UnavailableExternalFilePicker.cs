namespace TinyTBS.Engine.IO;

/// <summary>No-op picker when the host has no file UI (tests, unsupported platforms).</summary>
public sealed class UnavailableExternalFilePicker : IExternalFilePicker
{
    public string? PickOpenFile(ExternalFilePickRequest request) => null;
}
