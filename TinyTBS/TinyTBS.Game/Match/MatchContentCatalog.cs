using TinyTBS.Game.Buildings.Models;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Runtime content resolved for a match: unit/building definitions from modules.
/// Shop offers and display text are derived from these definitions (not hardcoded).
/// </summary>
public sealed class MatchContentCatalog
{
    private readonly Dictionary<UnitKind, UnitDefinition> _unitsByKind = new();
    private readonly Dictionary<BuildingKind, BuildingDefinition> _buildingsByKind = new();

    public MatchContentCatalog(
        UnitModuleDefinition unitsModule,
        BuildingModuleDefinition buildingsModule)
    {
        ArgumentNullException.ThrowIfNull(unitsModule);
        ArgumentNullException.ThrowIfNull(buildingsModule);

        UnitsModule = unitsModule;
        BuildingsModule = buildingsModule;

        foreach (var unitDefinition in unitsModule.UnitsById.Values)
        {
            var unitKind = VanillaContentIds.ParseUnit(unitDefinition.ContentId);
            _unitsByKind[unitKind] = unitDefinition;
        }

        foreach (var buildingDefinition in buildingsModule.BuildingsById.Values)
        {
            var buildingKind = VanillaContentIds.ParseBuilding(buildingDefinition.ContentId);
            _buildingsByKind[buildingKind] = buildingDefinition;
        }

        ShopOffers = BuildShopOffers(unitsModule);
    }

    public UnitModuleDefinition UnitsModule { get; }

    public BuildingModuleDefinition BuildingsModule { get; }

    /// <summary>Recruit offers in module pool order (cost from unit JSON).</summary>
    public IReadOnlyList<MatchShopOffer> ShopOffers { get; }

    public bool TryGetUnit(UnitKind kind, out UnitDefinition definition) =>
        _unitsByKind.TryGetValue(kind, out definition!);

    public bool TryGetUnit(ContentId contentId, out UnitDefinition definition) =>
        UnitsModule.UnitsById.TryGetValue(contentId, out definition!);

    public bool TryGetBuilding(BuildingKind kind, out BuildingDefinition definition) =>
        _buildingsByKind.TryGetValue(kind, out definition!);

    public bool TryGetBuilding(ContentId contentId, out BuildingDefinition definition) =>
        BuildingsModule.BuildingsById.TryGetValue(contentId, out definition!);

    public string DisplayName(UnitKind kind) =>
        TryGetUnit(kind, out var definition) ? DisplayName(definition) : kind.ToString();

    public string DisplayName(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return HumanizeLocalId(definition.ContentId.LocalId);
    }

    public string DisplayName(BuildingKind kind) =>
        TryGetBuilding(kind, out var definition) ? DisplayName(definition) : kind.ToString();

    public string DisplayName(BuildingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return HumanizeLocalId(definition.ContentId.LocalId);
    }

    public string FormatCombatStats(UnitKind kind) =>
        TryGetUnit(kind, out var definition) ? FormatCombatStats(definition) : string.Empty;

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

            var unitKind = VanillaContentIds.ParseUnit(contentId);
            offers.Add(new MatchShopOffer(unitKind, unitDefinition.Cost));
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
