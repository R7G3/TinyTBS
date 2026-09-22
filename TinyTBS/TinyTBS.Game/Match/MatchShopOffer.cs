namespace TinyTBS.Game.Match;

public sealed class MatchShopOffer
{
    public MatchShopOffer(UnitKind unitKind, int cost)
    {
        UnitKind = unitKind;
        Cost = cost;
    }

    public UnitKind UnitKind { get; }

    public int Cost { get; }
}
