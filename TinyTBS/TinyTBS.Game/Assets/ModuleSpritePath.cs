namespace TinyTBS.Game.Assets;

/// <summary>
/// Converts module-relative sprite paths (<c>Resources/Images/…png</c>) to MonoGame content asset names.
/// </summary>
internal static class ModuleSpritePath
{
    /// <summary>
    /// <c>Resources/Images/units/king_base.png</c> → <c>Images/units/king_base</c>.
    /// </summary>
    public static string? ToContentAssetName(string moduleRelativePath)
    {
        if (string.IsNullOrWhiteSpace(moduleRelativePath))
            return null;

        var normalized = moduleRelativePath
            .Replace('\\', '/')
            .TrimStart('/');

        const string resourcesPrefix = "Resources/";
        if (normalized.StartsWith(resourcesPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[resourcesPrefix.Length..];

        if (normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4];

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public static string CombineModuleFile(string moduleRoot, string moduleRelativePath)
    {
        var relative = moduleRelativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(moduleRoot, relative);
    }
}
