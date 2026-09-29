namespace TinyTBS.Game.Editor.Validation;

/// <summary>Aggregate validation for the map editor indicator.</summary>
public sealed class EditorMapValidationResult
{
    public EditorMapValidationResult(IReadOnlyList<EditorValidationIssue> issues)
    {
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
    }

    public IReadOnlyList<EditorValidationIssue> Issues { get; }

    public bool HasErrors => Issues.Any(issue => issue.Severity == EditorValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(issue => issue.Severity == EditorValidationSeverity.Warning);

    /// <summary>Blue = not run / empty; green = ok; yellow via warning text; red = errors.</summary>
    public EditorValidationIndicator Indicator
    {
        get
        {
            if (Issues.Count == 0)
                return EditorValidationIndicator.Ok;
            if (HasErrors)
                return EditorValidationIndicator.Error;
            if (HasWarnings)
                return EditorValidationIndicator.Warning;
            return EditorValidationIndicator.Ok;
        }
    }

    public string SummaryLine
    {
        get
        {
            if (Issues.Count == 0)
                return "Validate: OK";

            var errors = Issues.Count(issue => issue.Severity == EditorValidationSeverity.Error);
            var warnings = Issues.Count(issue => issue.Severity == EditorValidationSeverity.Warning);
            var head = Issues[0].Message;
            if (Issues.Count == 1)
                return head;

            return $"{head} (+{Issues.Count - 1} more; {errors} err, {warnings} warn)";
        }
    }
}

public enum EditorValidationIndicator
{
    /// <summary>Not validated yet this session.</summary>
    Idle,
    Ok,
    Warning,
    Error,
}
