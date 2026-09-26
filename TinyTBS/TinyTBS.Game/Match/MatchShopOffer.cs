using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

public sealed class MatchShopOffer
{
    public MatchShopOffer(ContentId unitTypeId, int cost)
    {
        UnitTypeId = unitTypeId;
        Cost = cost;
    }

    public ContentId UnitTypeId { get; }

    public int Cost { get; }
}
