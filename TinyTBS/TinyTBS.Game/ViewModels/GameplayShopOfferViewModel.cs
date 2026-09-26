using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.ViewModels;

public sealed class GameplayShopOfferViewModel
{
    public required ContentId UnitTypeId { get; init; }

    public required string Name { get; init; }

    /// <summary>Multiline combat stats (below name / cost).</summary>
    public required string StatsText { get; init; }

    public required int Cost { get; init; }

    public required bool CanAfford { get; init; }

    public required int OfferIndex { get; init; }
}
