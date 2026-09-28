using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Validates and normalizes a deserialized <see cref="MatchSaveDocument"/>.</summary>
internal static class MatchSaveDocumentNormalizer
{
    public static MatchSaveDocument Normalize(MatchSaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.SaveVersion < 1)
            throw new MatchSaveException($"Unsupported saveVersion '{document.SaveVersion}'.");

        if (!string.Equals(document.Kind, MatchSaveDocument.KindMatch, StringComparison.OrdinalIgnoreCase))
        {
            throw new MatchSaveException(
                $"Unsupported save kind '{document.Kind}'. Expected '{MatchSaveDocument.KindMatch}'.");
        }

        if (string.IsNullOrWhiteSpace(document.LevelId))
            throw new MatchSaveException("Match save requires non-empty 'levelId'.");

        if (document.ContentSetup is null)
            throw new MatchSaveException("Match save requires 'contentSetup'.");

        if (document.Match is null)
            throw new MatchSaveException("Match save requires 'match'.");

        var contentSetup = NormalizeContentSetup(document.ContentSetup);
        var match = NormalizeMatch(document.Match);
        var seats = NormalizeSeats(document.PlayerSeats);

        return new MatchSaveDocument
        {
            SaveVersion = document.SaveVersion,
            Kind = MatchSaveDocument.KindMatch,
            WrittenAtUtc = document.WrittenAtUtc == default
                ? DateTimeOffset.UtcNow
                : document.WrittenAtUtc.ToUniversalTime(),
            LevelId = document.LevelId.Trim(),
            UnitCap = Math.Max(1, document.UnitCap),
            ContentSetup = contentSetup,
            PlayerSeats = seats,
            Match = match,
            Extensions = document.Extensions is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(document.Extensions, StringComparer.Ordinal),
        };
    }

    private static MatchSaveContentSetup NormalizeContentSetup(MatchSaveContentSetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.ScenarioModuleId))
            throw new MatchSaveException("contentSetup.scenarioModuleId is missing.");
        if (string.IsNullOrWhiteSpace(setup.ThemeModuleId))
            throw new MatchSaveException("contentSetup.themeModuleId is missing.");

        var units = NormalizeIds(setup.UnitsModuleIds);
        if (units.Count == 0)
            throw new MatchSaveException("contentSetup.unitsModuleIds is empty.");

        var buildings = NormalizeIds(setup.BuildingsModuleIds);
        if (buildings.Count == 0)
            throw new MatchSaveException("contentSetup.buildingsModuleIds is empty.");

        var versions = setup.ModuleVersions is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(setup.ModuleVersions, StringComparer.Ordinal);

        var replaces = new List<MatchSaveReplace>();
        if (setup.Replaces is not null)
        {
            foreach (var replace in setup.Replaces)
            {
                if (string.IsNullOrWhiteSpace(replace.From) || string.IsNullOrWhiteSpace(replace.To))
                    continue;
                replaces.Add(new MatchSaveReplace
                {
                    From = replace.From.Trim(),
                    To = replace.To.Trim(),
                });
            }
        }

        return new MatchSaveContentSetup
        {
            ScenarioModuleId = setup.ScenarioModuleId.Trim(),
            UnitsModuleIds = units,
            BuildingsModuleIds = buildings,
            ThemeModuleId = setup.ThemeModuleId.Trim(),
            ModuleVersions = versions,
            Replaces = replaces,
        };
    }

    private static List<MatchSaveSeat> NormalizeSeats(List<MatchSaveSeat>? seats)
    {
        if (seats is null || seats.Count == 0)
            return [];

        var result = new List<MatchSaveSeat>(seats.Count);
        foreach (var seat in seats)
        {
            var kind = string.IsNullOrWhiteSpace(seat.Kind) ? "local" : seat.Kind.Trim().ToLowerInvariant();
            result.Add(new MatchSaveSeat
            {
                Kind = kind,
                BotDifficulty = string.IsNullOrWhiteSpace(seat.BotDifficulty)
                    ? null
                    : seat.BotDifficulty.Trim().ToLowerInvariant(),
            });
        }

        return result;
    }

    private static MatchRuntimeSnapshot NormalizeMatch(MatchRuntimeSnapshot match)
    {
        if (match.PlayerCount < 1)
            throw new MatchSaveException("match.playerCount must be >= 1.");
        if (match.Cursor is null)
            throw new MatchSaveException("match.cursor is missing.");

        var money = match.MoneyByPlayer ?? [];
        var turnStarts = match.TurnStartsByPlayer ?? [];
        var kingRehire = match.KingRehireCountByPlayer ?? [];
        if (money.Count != match.PlayerCount
            || turnStarts.Count != match.PlayerCount
            || kingRehire.Count != match.PlayerCount)
        {
            throw new MatchSaveException(
                "match money/turnStarts/kingRehire arrays must match playerCount.");
        }

        foreach (var unit in match.Units ?? [])
        {
            if (string.IsNullOrWhiteSpace(unit.TypeId) || unit.Cell is null || unit.CellBeforeMove is null)
                throw new MatchSaveException("match.units entry is incomplete.");
        }

        foreach (var building in match.Buildings ?? [])
        {
            if (string.IsNullOrWhiteSpace(building.TypeId) || building.Cell is null)
                throw new MatchSaveException("match.buildings entry is incomplete.");
        }

        foreach (var stone in match.Gravestones ?? [])
        {
            if (stone.Cell is null)
                throw new MatchSaveException("match.gravestones entry is incomplete.");
        }

        return new MatchRuntimeSnapshot
        {
            PlayerCount = match.PlayerCount,
            CurrentPlayer = match.CurrentPlayer,
            TurnNumber = Math.Max(1, match.TurnNumber),
            NextUnitId = Math.Max(0, match.NextUnitId),
            UnitCap = Math.Max(1, match.UnitCap),
            MoneyByPlayer = money.ToList(),
            TurnStartsByPlayer = turnStarts.ToList(),
            KingRehireCountByPlayer = kingRehire.ToList(),
            EliminatedPlayers = (match.EliminatedPlayers ?? []).ToList(),
            Cursor = match.Cursor,
            SelectedUnitId = match.SelectedUnitId,
            WinnerPlayerIndex = match.WinnerPlayerIndex,
            VictoryReason = match.VictoryReason,
            Units = (match.Units ?? [])
                .Select(unit => new MatchSaveUnitSnapshot
                {
                    Id = unit.Id,
                    TypeId = unit.TypeId.Trim(),
                    Cell = unit.Cell,
                    PlayerIndex = unit.PlayerIndex,
                    MaxHealth = unit.MaxHealth,
                    HitPoints = unit.HitPoints,
                    IsActive = unit.IsActive,
                    HasMovedThisActivation = unit.HasMovedThisActivation,
                    CellBeforeMove = unit.CellBeforeMove,
                    Experience = unit.Experience,
                })
                .ToList(),
            Buildings = (match.Buildings ?? [])
                .Select(building => new MatchSaveBuildingSnapshot
                {
                    TypeId = building.TypeId.Trim(),
                    Cell = building.Cell,
                    OwnerPlayerIndex = building.OwnerPlayerIndex,
                    IsRuined = building.IsRuined,
                    AllowsRecruit = building.AllowsRecruit,
                    RepairedThisOwnerTurn = building.RepairedThisOwnerTurn,
                })
                .ToList(),
            Gravestones = (match.Gravestones ?? []).ToList(),
        };
    }

    private static List<string> NormalizeIds(List<string>? values)
    {
        if (values is null || values.Count == 0)
            return [];

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            var trimmed = value.Trim();
            if (!seen.Add(trimmed))
                continue;
            result.Add(trimmed);
        }

        return result;
    }
}
