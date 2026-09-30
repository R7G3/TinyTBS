namespace TinyTBS.Game.Editor.Workspace;

/// <summary>
/// Content id helpers shared by editor wizards and forms: sanitising a typed id and picking a free
/// <c>stem</c>, <c>stem_2</c>, <c>stem_3</c>, … name.
/// </summary>
public static class EditorIds
{
    private const int MaxSuffix = 10_000;

    /// <summary>
    /// <paramref name="raw"/> trimmed once when it is a valid content id; otherwise <paramref name="fallback"/>.
    /// This is the only whitespace trim for a typed id.
    /// </summary>
    public static string SanitizeOrDefault(string? raw, string fallback)
    {
        var candidate = SavedUserText.Or(raw, fallback);
        try
        {
            ContentModuleManifestParser.ValidateModuleId(candidate);
            return candidate;
        }
        catch (TinymodInstallException)
        {
            return fallback;
        }
    }

    /// <summary><paramref name="stem"/> when it is free, otherwise the first free <c>{stem}_2</c>, <c>{stem}_3</c>, …</summary>
    public static string AllocateUnique(string stem, Func<string, bool> isTaken, string idKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);
        ArgumentNullException.ThrowIfNull(isTaken);

        if (!isTaken(stem))
            return stem;

        for (var suffix = 2; suffix < MaxSuffix; suffix++)
        {
            var candidate = stem + "_" + suffix;
            if (!isTaken(candidate))
                return candidate;
        }

        throw new EditorException($"Could not allocate a unique {idKind} id.");
    }
}
