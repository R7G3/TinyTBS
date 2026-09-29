namespace TinyTBS.Game.Editor.Validation;

public enum EditorValidationSeverity
{
    Info,
    Warning,
    Error,
}

public sealed class EditorValidationIssue
{
    public EditorValidationIssue(EditorValidationSeverity severity, string message)
    {
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public EditorValidationSeverity Severity { get; }

    public string Message { get; }
}
