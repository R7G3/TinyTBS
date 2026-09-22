namespace TinyTBS.Game.Match;

/// <summary>Demo shop entries until units modules exist.</summary>
public static class MatchShopCatalog
{
    public static IReadOnlyList<MatchShopOffer> Offers { get; } =
    [
        new MatchShopOffer(UnitKind.Swordsman, cost: 100),
        new MatchShopOffer(UnitKind.King, cost: 400),
        // TEMP: duplicate rows to exercise shop list scrolling — remove after scroll QA.
        new MatchShopOffer(UnitKind.Swordsman, cost: 100),
        new MatchShopOffer(UnitKind.King, cost: 400),
        new MatchShopOffer(UnitKind.Swordsman, cost: 100),
        new MatchShopOffer(UnitKind.King, cost: 400),
    ];
}
