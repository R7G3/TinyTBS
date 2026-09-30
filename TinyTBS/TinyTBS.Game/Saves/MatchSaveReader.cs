using System.Text.Json;
using TinyTBS.Engine.IO;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Reads <c>match_*.json</c> save documents.</summary>
public static class MatchSaveReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static MatchSaveDocument ReadFile(IFileSystem files, string filePath)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!files.Exists(filePath))
            throw new MatchSaveException($"Save file not found: {filePath}");

        try
        {
            using var stream = files.OpenRead(filePath);
            return Read(stream);
        }
        catch (MatchSaveException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new MatchSaveException($"Failed to read save '{filePath}'.", exception);
        }
    }

    public static MatchSaveDocument Read(Stream jsonStream)
    {
        ArgumentNullException.ThrowIfNull(jsonStream);

        MatchSaveDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<MatchSaveDocument>(jsonStream, JsonOptions);
        }
        catch (JsonException jsonException)
        {
            throw new MatchSaveException("Failed to parse match save JSON.", jsonException);
        }

        if (document is null)
            throw new MatchSaveException("Match save deserialized to null.");

        return MatchSaveDocumentNormalizer.Normalize(document);
    }
}
