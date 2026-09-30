using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Builds a <see cref="MatchSaveDocument"/> from a running <see cref="MatchRuntime"/>.</summary>
public static class MatchSaveDocumentFactory
{
    public static MatchSaveDocument FromRuntime(
        MatchRuntime runtime,
        GridCell cursor,
        IFileSystem files,
        IUserDataPaths userDataPaths,
        DateTimeOffset? writtenAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        var composition = runtime.Composition;
        var moduleVersions = runtime.ModuleVersions.Count > 0
            ? runtime.ModuleVersions
            : CollectModuleVersions(composition, files, userDataPaths);

        return new MatchSaveDocument
        {
            SaveVersion = MatchSaveDocument.CurrentSaveVersion,
            Kind = MatchSaveDocument.KindMatch,
            WrittenAtUtc = writtenAtUtc ?? DateTimeOffset.UtcNow,
            LevelId = runtime.LevelBrief.LevelId,
            CampaignId = runtime.CampaignRun?.CampaignId,
            CampaignLevelId = runtime.CampaignRun?.CurrentLevelId,
            UnitCap = runtime.State.UnitCap,
            ContentSetup = new MatchSaveContentSetup
            {
                ScenarioModuleId = composition.ScenarioModuleId,
                UnitsModuleIds = composition.UnitsModuleIds.ToList(),
                BuildingsModuleIds = composition.BuildingsModuleIds.ToList(),
                ThemeModuleId = composition.ThemeModuleId,
                ModuleVersions = new Dictionary<string, string>(moduleVersions, StringComparer.Ordinal),
                Replaces = composition.Replaces
                    .Select(replace => new MatchSaveReplace
                    {
                        From = replace.From.Full,
                        To = replace.To.Full,
                    })
                    .ToList(),
            },
            PlayerSeats = runtime.PlayerSeats.Select(MatchSaveSeatCodec.ToSave).ToList(),
            Match = runtime.State.ToSnapshot(cursor),
            Extensions = runtime.CampaignRun is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(runtime.CampaignRun.Extensions, StringComparer.Ordinal),
        };
    }

    public static MatchContentComposition ToComposition(MatchSaveContentSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        var replaces = new List<ContentIdReplace>();
        foreach (var replace in setup.Replaces)
        {
            replaces.Add(new ContentIdReplace
            {
                From = ContentId.Parse(replace.From),
                To = ContentId.Parse(replace.To),
            });
        }

        return new MatchContentComposition
        {
            ScenarioModuleId = setup.ScenarioModuleId,
            UnitsModuleIds = setup.UnitsModuleIds.ToList(),
            BuildingsModuleIds = setup.BuildingsModuleIds.ToList(),
            ThemeModuleId = setup.ThemeModuleId,
            Replaces = replaces,
        };
    }

    public static IReadOnlyDictionary<string, string> CollectModuleVersions(
        MatchContentComposition composition,
        IFileSystem files,
        IUserDataPaths userDataPaths)
    {
        var locator = new ContentModuleLocator(files, userDataPaths);
        var versions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var moduleId in EnumerateModuleIds(composition))
        {
            try
            {
                var root = locator.ResolveModuleRoot(moduleId);
                var manifestPath = files.Combine(root, ContentModuleFiles.ModuleJsonFileName);
                using var stream = files.OpenRead(manifestPath);
                var info = ContentModuleManifestParser.Parse(stream, root, ContentModuleSource.Bundled);
                versions[moduleId] = info.Version;
            }
            catch (Exception exception)
            {
                GameLog.Warning($"Module '{moduleId}' version could not be read.", exception);
                versions[moduleId] = "unknown";
            }
        }

        return versions;
    }

    private static IEnumerable<string> EnumerateModuleIds(MatchContentComposition composition)
    {
        yield return composition.ScenarioModuleId;
        foreach (var id in composition.UnitsModuleIds)
            yield return id;
        foreach (var id in composition.BuildingsModuleIds)
            yield return id;
        yield return composition.ThemeModuleId;
    }
}
