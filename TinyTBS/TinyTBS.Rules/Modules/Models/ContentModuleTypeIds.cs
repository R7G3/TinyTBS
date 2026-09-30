namespace TinyTBS.Rules.Modules.Models;

/// <summary>JSON <c>type</c> ids for <see cref="ContentModuleType"/>.</summary>
public static class ContentModuleTypeIds
{
    public const string Scenario = "scenario";

    public const string Units = "units";

    public const string Buildings = "buildings";

    public const string Theme = "theme";

    public static string ToId(ContentModuleType type) => type switch
    {
        ContentModuleType.Scenario => Scenario,
        ContentModuleType.Units => Units,
        ContentModuleType.Buildings => Buildings,
        ContentModuleType.Theme => Theme,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown module type."),
    };

    public static bool TryParse(string? typeValue, out ContentModuleType moduleType)
    {
        moduleType = default;
        if (string.IsNullOrWhiteSpace(typeValue))
            return false;

        if (string.Equals(typeValue, Scenario, StringComparison.OrdinalIgnoreCase))
        {
            moduleType = ContentModuleType.Scenario;
            return true;
        }

        if (string.Equals(typeValue, Units, StringComparison.OrdinalIgnoreCase))
        {
            moduleType = ContentModuleType.Units;
            return true;
        }

        if (string.Equals(typeValue, Buildings, StringComparison.OrdinalIgnoreCase))
        {
            moduleType = ContentModuleType.Buildings;
            return true;
        }

        if (string.Equals(typeValue, Theme, StringComparison.OrdinalIgnoreCase))
        {
            moduleType = ContentModuleType.Theme;
            return true;
        }

        return false;
    }
}
