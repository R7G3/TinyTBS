using TinyTBS.Engine.IO;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Match;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Builds a <see cref="MatchSaveDocument"/> from a live <see cref="GameplaySession"/>.</summary>
public static class MatchSaveDocumentFactory
{
    public static MatchSaveDocument FromSession(
        GameplaySession session,
        IFileContentProvider files,
        IUserDataPaths userDataPaths,
        DateTimeOffset? writtenAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(userDataPaths);

        var composition = session.Composition
            ?? throw new MatchSaveException("Session has no content composition to save.");

        var moduleVersions = session.ModuleVersions.Count > 0
            ? session.ModuleVersions
            : CollectModuleVersions(composition, files, userDataPaths);

        return new MatchSaveDocument
        {
            SaveVersion = MatchSaveDocument.CurrentSaveVersion,
            Kind = MatchSaveDocument.KindMatch,
            WrittenAtUtc = writtenAtUtc ?? DateTimeOffset.UtcNow,
            LevelId = session.LevelBrief.LevelId,
            CampaignId = session.CampaignRun?.CampaignId,
            CampaignLevelId = session.CampaignRun?.CurrentLevelId,
            UnitCap = session.State.UnitCap,
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
            PlayerSeats = session.PlayerSeats.Select(MatchSaveSeatCodec.ToSave).ToList(),
            Match = CaptureRuntime(session.State),
            Extensions = session.CampaignRun is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(session.CampaignRun.Extensions, StringComparer.Ordinal),
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
        IFileContentProvider files,
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
            catch (Exception)
            {
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

    private static MatchRuntimeSnapshot CaptureRuntime(MatchState state)
    {
        var playerCount = state.MoneyByPlayer.Count;
        var money = new int[playerCount];
        var turnStarts = new int[playerCount];
        var kingRehire = new int[playerCount];
        for (var i = 0; i < playerCount; i++)
        {
            money[i] = state.GetMoney(i);
            turnStarts[i] = state.TurnStarts.TryGetValue(i, out var starts) ? starts : 0;
            kingRehire[i] = state.KingRehireCounts.TryGetValue(i, out var count) ? count : 0;
        }

        var eliminated = new List<int>();
        for (var i = 0; i < playerCount; i++)
        {
            if (state.IsPlayerEliminated(i))
                eliminated.Add(i);
        }

        return new MatchRuntimeSnapshot
        {
            PlayerCount = playerCount,
            CurrentPlayer = state.CurrentPlayer,
            TurnNumber = state.TurnNumber,
            NextUnitId = state.PeekNextUnitId(),
            UnitCap = state.UnitCap,
            MoneyByPlayer = money.ToList(),
            TurnStartsByPlayer = turnStarts.ToList(),
            KingRehireCountByPlayer = kingRehire.ToList(),
            EliminatedPlayers = eliminated,
            Cursor = ToCell(state.Cursor),
            SelectedUnitId = state.SelectedUnitId,
            WinnerPlayerIndex = state.WinnerPlayerIndex,
            VictoryReason = state.VictoryReason,
            Units = state.Units.Select(CaptureUnit).ToList(),
            Buildings = state.Buildings.Select(CaptureBuilding).ToList(),
            Gravestones = state.Gravestones.Select(CaptureGravestone).ToList(),
        };
    }

    private static MatchSaveUnitSnapshot CaptureUnit(MatchUnit unit) =>
        new()
        {
            Id = unit.Id,
            TypeId = unit.TypeId.Full,
            Cell = ToCell(unit.Cell),
            PlayerIndex = unit.PlayerIndex,
            MaxHealth = unit.MaxHealth,
            HitPoints = unit.HitPoints,
            IsActive = unit.IsActive,
            HasMovedThisActivation = unit.HasMovedThisActivation,
            CellBeforeMove = ToCell(unit.CellBeforeMove),
            Experience = unit.Experience,
        };

    private static MatchSaveBuildingSnapshot CaptureBuilding(MatchBuilding building) =>
        new()
        {
            TypeId = building.TypeId.Full,
            Cell = ToCell(building.Cell),
            OwnerPlayerIndex = building.OwnerPlayerIndex,
            IsRuined = building.IsRuined,
            AllowsRecruit = building.AllowsRecruit,
            RepairedThisOwnerTurn = building.RepairedThisOwnerTurn,
        };

    private static MatchSaveGravestoneSnapshot CaptureGravestone(MatchGravestone stone) =>
        new()
        {
            Cell = ToCell(stone.Cell),
            SourcePlayerIndex = stone.SourcePlayerIndex,
            ExpiresWhenTurnStartsReaches = stone.ExpiresWhenTurnStartsReaches,
        };

    private static MatchSaveCell ToCell(GridCell cell) =>
        new() { X = cell.X, Y = cell.Y };
}
