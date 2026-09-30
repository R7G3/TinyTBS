namespace TinyTBS.Rules.Match;

/// <summary>Level victory / defeat condition types (LEVEL_FORMAT); other ids are resolved by map scripts.</summary>
public static class MatchConditionTypes
{
    public const string Standard = "standard";

    public static bool IsStandard(string? type) =>
        string.Equals(type, Standard, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? type) =>
        string.IsNullOrWhiteSpace(type) ? Standard : type.Trim();
}
