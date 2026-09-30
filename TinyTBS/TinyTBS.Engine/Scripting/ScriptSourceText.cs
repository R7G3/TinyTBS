using System.Text.RegularExpressions;

namespace TinyTBS.Engine.Scripting;

/// <summary>Text-level helpers for script sources.</summary>
public static partial class ScriptSourceText
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    /// <summary>True when the source has nothing but whitespace and comments.</summary>
    public static bool IsEffectivelyEmpty(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        var withoutBlockComments = BlockCommentRegex().Replace(sourceCode, string.Empty);
        foreach (var line in withoutBlockComments.Split(LineSeparators, StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
                continue;
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();
}
