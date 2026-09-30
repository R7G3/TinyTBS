using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Scripting;
using TinyTBS.Game.Match;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Owns compiled map hooks and invokes them after match events. Each hook sees a snapshot; its requested
/// changes are applied only when it finishes within budget. A failing hook disables the script for the
/// rest of the match instead of breaking it.
/// </summary>
public sealed class MapScriptHost : IDisposable
{
    private readonly LoadedScript<IMapScriptHooks> _script;
    private readonly TimeSpan _hookTimeout;

    public MapScriptHost(LoadedScript<IMapScriptHooks> script, TimeSpan? hookTimeout = null)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        _hookTimeout = hookTimeout ?? ScriptExecution.DefaultTimeout;
    }

    /// <summary>Why the map script was switched off for this match; null while it runs normally.</summary>
    public string? FailureMessage { get; private set; }

    public static MapScriptHost LoadForMap(
        string? scriptPath,
        IFileContentProvider files,
        IScriptEngine scriptEngine,
        TimeSpan? hookTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(scriptEngine);

        if (string.IsNullOrWhiteSpace(scriptPath) || !files.Exists(scriptPath))
            return new MapScriptHost(new LoadedScript<IMapScriptHooks>(new NoOpMapScriptHooks()), hookTimeout);

        using var stream = files.OpenRead(scriptPath);
        using var reader = new StreamReader(stream);
        var sourceCode = reader.ReadToEnd();
        return new MapScriptHost(scriptEngine.LoadMapScript(sourceCode, Path.GetFileName(scriptPath)), hookTimeout);
    }

    public void NotifyMatchStarted(MatchState match) =>
        Invoke("OnPlayerTurnStart", match, lastAction: null, static (hooks, context) => hooks.OnPlayerTurnStart(context));

    public void NotifyPlayerTurnStart(MatchState match) =>
        Invoke("OnPlayerTurnStart", match, lastAction: null, static (hooks, context) => hooks.OnPlayerTurnStart(context));

    public void NotifyAfterPlayerAction(MatchState match, MatchPlayerAction action) =>
        Invoke(
            "OnAfterPlayerAction",
            match,
            MapScriptContextFactory.FromMatchAction(action),
            static (hooks, context) => hooks.OnAfterPlayerAction(context));

    public void Dispose() => _script.Dispose();

    private void Invoke(
        string hookName,
        MatchState match,
        MapScriptLastAction? lastAction,
        Action<IMapScriptHooks, MapScriptContext> call)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (FailureMessage is not null)
            return;

        var hooks = _script.Hooks;
        var context = MapScriptContextFactory.Create(match, lastAction);
        try
        {
            ScriptExecution.Run(hookName, _hookTimeout, () => call(hooks, context));
        }
        catch (ScriptHostException exception)
        {
            FailureMessage = exception.Message;
            GameLog.Error($"Map script disabled after '{hookName}' failed.", exception);
            return;
        }

        foreach (var command in context.Commands)
        {
            switch (command.Kind)
            {
                case MapScriptCommandKind.AddMoney:
                    match.AddMoney(command.PlayerId, command.Amount);
                    break;
                case MapScriptCommandKind.SetVictory:
                    match.SetVictory(command.PlayerId, command.Reason ?? string.Empty);
                    break;
            }
        }
    }
}
