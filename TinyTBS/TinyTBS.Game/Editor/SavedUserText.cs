namespace TinyTBS.Game.Editor;

/// <summary>
/// Trims text the user typed. Call once, from the method that writes that text.
/// Loaded content is kept as stored.
/// </summary>
public static class SavedUserText
{
    /// <summary>Trimmed <paramref name="value"/>, or <paramref name="fallback"/> when it is blank.</summary>
    public static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    /// <summary>Trimmed <paramref name="value"/>, or null when it is blank.</summary>
    public static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Trimmed <paramref name="value"/>, or empty when it is blank.</summary>
    public static string Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    /// <summary>Drops blank entries and trims the rest, preserving order.</summary>
    public static List<string> List(IEnumerable<string>? values)
    {
        var result = new List<string>();
        if (values is null)
            return result;

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            result.Add(value.Trim());
        }

        return result;
    }
}
