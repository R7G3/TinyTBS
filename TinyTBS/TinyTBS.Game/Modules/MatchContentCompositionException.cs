namespace TinyTBS.Game.Modules;

/// <summary>Thrown when match content composition or module resolution fails.</summary>
public sealed class MatchContentCompositionException : Exception
{
    public MatchContentCompositionException(string message)
        : base(message)
    {
    }

    public MatchContentCompositionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MatchContentCompositionException(IReadOnlyList<string> errors)
        : base(FormatErrors(errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; } = [];

    private static string FormatErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
            return "Match content composition failed.";

        return "Match content composition failed:" + Environment.NewLine
            + string.Join(Environment.NewLine, errors.Select(error => "- " + error));
    }
}
