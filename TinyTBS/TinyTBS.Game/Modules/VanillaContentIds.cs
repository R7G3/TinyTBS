using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Ids of the bundled vanilla content used as defaults by menus and the editor.</summary>
public static class VanillaContentIds
{
    /// <summary>
    /// Shared vanilla prefix: module ids are <c>vanilla_units</c>, content ids are <c>vanilla/king</c>.
    /// </summary>
    public const string ContentNamespace = "vanilla";

    public const string ScenarioModuleId = ContentNamespace + "_" + ContentModuleTypeIds.Scenario;

    public const string UnitsModuleId = ContentNamespace + "_" + ContentModuleTypeIds.Units;

    public const string BuildingsModuleId = ContentNamespace + "_" + ContentModuleTypeIds.Buildings;

    public const string ThemeModuleId = ContentNamespace + "_" + ContentModuleTypeIds.Theme;

    public const string CastleId = ContentNamespace + "/castle";

    public const string VillageId = ContentNamespace + "/village";

    public const string SwordsmanId = ContentNamespace + "/swordsman";

    /// <summary>Full-roster level preselected for Skirmish.</summary>
    public const string DefaultSkirmishLevelId = "proving-grounds";
}
