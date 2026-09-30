namespace TinyTBS.Scripting.Api;

/// <summary>Stops a script that ran out of budget. Internal so scripts cannot catch it by name.</summary>
internal sealed class ScriptBudgetExceededException : Exception
{
    public ScriptBudgetExceededException(string message)
        : base(message)
    {
    }
}
