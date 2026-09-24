namespace TinyTBS.Game.Match;

/// <summary>Demo shop entries until units modules drive the recruit pool.</summary>
public static class MatchShopCatalog
{
    public static IReadOnlyList<MatchShopOffer> Offers { get; } =
    [
        new MatchShopOffer(UnitKind.Swordsman, cost: 150),
        new MatchShopOffer(UnitKind.Archer, cost: 250),
        new MatchShopOffer(UnitKind.Lizard, cost: 300),
        new MatchShopOffer(UnitKind.King, cost: 400),
        new MatchShopOffer(UnitKind.Witch, cost: 400),
        new MatchShopOffer(UnitKind.Wisp, cost: 500),
        new MatchShopOffer(UnitKind.Golem, cost: 600),
        new MatchShopOffer(UnitKind.Catapult, cost: 800),
        new MatchShopOffer(UnitKind.Wyvern, cost: 1000),
    ];
}
