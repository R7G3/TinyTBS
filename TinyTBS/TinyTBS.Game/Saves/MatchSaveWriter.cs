using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Writes match save documents under <see cref="IUserDataPaths.Saves"/>.</summary>
public sealed class MatchSaveWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public MatchSaveWriter(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>Writes <paramref name="document"/> and returns the absolute file path.</summary>
    public string Write(MatchSaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _userDataPaths.EnsureCreated();
        var fileName = BuildFileName(document.WrittenAtUtc);
        var filePath = Path.Combine(_userDataPaths.Saves, fileName);

        try
        {
            var json = JsonSerializer.Serialize(document, JsonOptions);
            _files.WriteAllText(filePath, json);
            return filePath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new MatchSaveException($"Failed to write save '{filePath}'.", exception);
        }
    }

    private static string BuildFileName(DateTimeOffset writtenAtUtc)
    {
        var stamp = writtenAtUtc.ToUniversalTime().ToString("yyyyMMdd_HHmmss");
        var shortId = Guid.NewGuid().ToString("N")[..8];
        return $"match_{stamp}_{shortId}.json";
    }
}
