using TinyTBS.Rules.Maps.Models;

namespace TinyTBS.Rules.Match;

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
