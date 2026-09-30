using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TinyTBS.Rules;

/// <summary>
/// Serializer settings for content JSON. Parsers and the editor share them
/// so a format is not read or written two different ways.
/// </summary>
public static class ContentJson
{
    public static JsonSerializerOptions Read { get; } = CreateRead();

    public static JsonSerializerOptions Write { get; } = CreateWrite();

    private static JsonSerializerOptions CreateRead()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.MakeReadOnly();
        return options;
    }

    private static JsonSerializerOptions CreateWrite()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.MakeReadOnly();
        return options;
    }
}
