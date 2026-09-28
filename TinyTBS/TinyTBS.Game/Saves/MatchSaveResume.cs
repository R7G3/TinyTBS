using TinyTBS.Engine.IO;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Builds a <see cref="ContinueMatchRequest"/> from a save, with optional version warning.</summary>
public static class MatchSaveResume
{
    public static ContinueMatchRequest CreateRequest(
        MatchSaveDocument document,
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        var request = ContinueMatchRequest.FromDocument(document);
        var warning = BuildVersionWarning(document, files, userDataPaths);
        if (string.IsNullOrWhiteSpace(warning))
            return request;

        return new ContinueMatchRequest
        {
            LevelId = request.LevelId,
            ScenarioModuleId = request.ScenarioModuleId,
            Composition = request.Composition,
            PlayerCount = request.PlayerCount,
            UnitCap = request.UnitCap,
            PlayerSeats = request.PlayerSeats,
            RuntimeSnapshot = request.RuntimeSnapshot,
            VersionWarning = warning,
        };
    }

    public static string? BuildVersionWarning(
        MatchSaveDocument document,
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            var current = MatchSaveDocumentFactory.CollectModuleVersions(
                MatchSaveDocumentFactory.ToComposition(document.ContentSetup),
                files,
                userDataPaths);

            foreach (var pair in document.ContentSetup.ModuleVersions)
            {
                if (!current.TryGetValue(pair.Key, out var now))
                    return $"Module '{pair.Key}' missing — attempting load…";
                if (!string.Equals(now, pair.Value, StringComparison.Ordinal))
                    return $"Module '{pair.Key}' version {pair.Value} → {now} — attempting load…";
            }
        }
        catch (MatchContentCompositionException)
        {
            return "Some modules changed — attempting load…";
        }

        return null;
    }
}
