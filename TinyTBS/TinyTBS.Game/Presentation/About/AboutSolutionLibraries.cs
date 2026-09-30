using System.Reflection;

namespace TinyTBS.Game.Presentation.About;

/// <summary>
/// NuGet packages used across the TinyTBS solution, embedded at build time from
/// <c>Directory.Packages.props</c> (see the <c>AddSolutionLibraryMetadata</c> target in TinyTBS.Game.csproj).
/// </summary>
public static class AboutSolutionLibraries
{
    private const string MetadataKey = "TinyTBS.SolutionLibrary";

    public static IReadOnlyList<string> Lines { get; } = typeof(AboutSolutionLibraries).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Where(attribute => attribute.Key == MetadataKey && !string.IsNullOrWhiteSpace(attribute.Value))
        .Select(attribute => attribute.Value!)
        .OrderBy(line => line, StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
