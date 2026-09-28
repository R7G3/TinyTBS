using TinyTBS.Game.Saves;
using TinyTBS.Game.Saves.Models;

namespace TinyTBS.Game.Match;

/// <summary>Start a match by hydrating a saved runtime snapshot.</summary>
public sealed class ContinueMatchRequest
{
    public required string LevelId { get; init; }

    public required string ScenarioModuleId { get; init; }

    public required Modules.Models.MatchContentComposition Composition { get; init; }

    public required int PlayerCount { get; init; }

    public required int UnitCap { get; init; }

    public required IReadOnlyList<Ai.MatchPlayerSeat> PlayerSeats { get; init; }

    public required MatchRuntimeSnapshot RuntimeSnapshot { get; init; }

    /// <summary>Optional version mismatch note for the loading UI.</summary>
    public string? VersionWarning { get; init; }

    public static ContinueMatchRequest FromDocument(MatchSaveDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new ContinueMatchRequest
        {
            LevelId = document.LevelId,
            ScenarioModuleId = document.ContentSetup.ScenarioModuleId,
            Composition = MatchSaveDocumentFactory.ToComposition(document.ContentSetup),
            PlayerCount = document.Match.PlayerCount,
            UnitCap = document.Match.UnitCap,
            PlayerSeats = MatchSaveSeatCodec.FromSaveList(document.PlayerSeats),
            RuntimeSnapshot = document.Match,
        };
    }
}
