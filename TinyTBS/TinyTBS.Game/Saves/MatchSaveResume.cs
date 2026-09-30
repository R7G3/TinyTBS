using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;
using TinyTBS.Rules.Match;
using TinyTBS.Game.Modules;
using TinyTBS.Rules.Saves.Models;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Saves;

/// <summary>Builds a resume <see cref="MatchStartRequest"/> from a save, with optional version warning.</summary>
public static class MatchSaveResume
{
    public static MatchStartRequest CreateRequest(
        MatchSaveDocument document,
        IFileSystem files,
        IUserDataPaths userDataPaths)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        var campaignRun = CampaignRunRestorer.TryRestoreForMatch(document, files, userDataPaths);
        var warning = BuildVersionWarning(document, files, userDataPaths);
        var orphanNote = campaignRun is not null
            && string.IsNullOrWhiteSpace(campaignRun.ProgressFilePath)
            ? "Campaign progress was missing; will recreate on chapter end."
            : null;

        var combinedWarning = string.Join(
            " ",
            new[] { warning, orphanNote }.Where(text => !string.IsNullOrWhiteSpace(text)));

        return MatchStartRequest.FromSaveDocument(
            document,
            campaignRun,
            string.IsNullOrWhiteSpace(combinedWarning) ? null : combinedWarning);
    }

    public static string? BuildVersionWarning(
        MatchSaveDocument document,
        IFileSystem files,
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
