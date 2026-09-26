using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Themes.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Runtime content resolved for a match: unit/building/theme definitions from modules.
/// Shop offers and display text are derived from these definitions (not hardcoded).
/// </summary>
public sealed class MatchContentCatalog
{
    public MatchContentCatalog(
        UnitModuleDefinition unitsModule,
        BuildingModuleDefinition buildingsModule,
        ThemeModuleDefinition themeModule)
    {
        ArgumentNullException.ThrowIfNull(unitsModule);
        ArgumentNullException.ThrowIfNull(buildingsModule);
        ArgumentNullException.ThrowIfNull(themeModule);

        UnitsModule = unitsModule;
        BuildingsModule = buildingsModule;
        ThemeModule = themeModule;
        ShopOffers = BuildShopOffers(unitsModule);
    }

    public UnitModuleDefinition UnitsModule { get; }

    public BuildingModuleDefinition BuildingsModule { get; }

    public ThemeModuleDefinition ThemeModule { get; }

    /// <summary>Recruit offers in module pool order (cost from unit JSON).</summary>
    public IReadOnlyList<MatchShopOffer> ShopOffers { get; }

    public bool TryGetUnit(ContentId contentId, out UnitDefinition definition) =>
        UnitsModule.UnitsById.TryGetValue(contentId, out definition!);

    public bool TryGetBuilding(ContentId contentId, out BuildingDefinition definition) =>
        BuildingsModule.BuildingsById.TryGetValue(contentId, out definition!);

    public string DisplayName(ContentId contentId)
    {
        if (TryGetUnit(contentId, out var unitDefinition))
            return DisplayName(unitDefinition);
        if (TryGetBuilding(contentId, out var buildingDefinition))
            return DisplayName(buildingDefinition);
        return contentId.LocalId;
    }

    public string DisplayName(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return HumanizeLocalId(definition.ContentId.LocalId);
    }

    public string DisplayName(BuildingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return HumanizeLocalId(definition.ContentId.LocalId);
    }

    public string FormatCombatStats(ContentId unitTypeId) =>
        TryGetUnit(unitTypeId, out var definition) ? FormatCombatStats(definition) : string.Empty;

    public string FormatCombatStats(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var rangeText = definition.AttackRangeMin == definition.AttackRangeMax
            ? definition.AttackRangeMin.ToString()
            : $"{definition.AttackRangeMin}–{definition.AttackRangeMax}";

        return $"Atk {definition.Attack}  Def {definition.Defence}  HP {definition.MaxHealth}"
            + $"{Environment.NewLine}Range {rangeText}  Speed {definition.Speed}";
    }

    private static IReadOnlyList<MatchShopOffer> BuildShopOffers(UnitModuleDefinition unitsModule)
    {
        var offers = new List<MatchShopOffer>();
        IEnumerable<ContentId> poolIds = unitsModule.RecruitPool.Count > 0
            ? unitsModule.RecruitPool
            : unitsModule.UnitsById.Keys
                .Where(id => unitsModule.UnitsById[id].Recruitable)
                .OrderBy(id => unitsModule.UnitsById[id].Cost)
                .ThenBy(id => id.Full, StringComparer.Ordinal);

        foreach (var contentId in poolIds)
        {
            if (!unitsModule.UnitsById.TryGetValue(contentId, out var unitDefinition))
                continue;

            if (!unitDefinition.Recruitable)
                continue;

            offers.Add(new MatchShopOffer(contentId, unitDefinition.Cost));
        }

        return offers;
    }

    private static string HumanizeLocalId(string localId)
    {
        if (string.IsNullOrWhiteSpace(localId))
            return localId;

        return char.ToUpperInvariant(localId[0]) + localId[1..];
    }
}
