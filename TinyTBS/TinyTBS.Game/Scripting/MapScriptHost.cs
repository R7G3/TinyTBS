using TinyTBS.Engine.IO;
using TinyTBS.Engine.Scripting;
using TinyTBS.Game.Match;
using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Owns compiled map hooks and invokes them with timeout after match events.
/// </summary>
public sealed class MapScriptHost
{
    private readonly IMapScriptHooks _hooks;
    private readonly TimeSpan _hookTimeout;
    private readonly IMapScriptWorld _world;

    public MapScriptHost(IMapScriptHooks hooks, MatchState match, TimeSpan? hookTimeout = null)
    {
        _hooks = hooks;
        _world = new MatchMapScriptWorld(match);
        _hookTimeout = hookTimeout ?? TimeSpan.FromSeconds(2);
    }

    public static MapScriptHost LoadForMap(
        MatchState match,
        string? scriptPath,
        IFileContentProvider files,
        IScriptEngine scriptEngine,
        TimeSpan? hookTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(scriptEngine);

        IMapScriptHooks hooks;
        if (string.IsNullOrWhiteSpace(scriptPath) || !files.Exists(scriptPath))
        {
            hooks = new NoOpMapScriptHooks();
        }
        else
        {
            using var stream = files.OpenRead(scriptPath);
            using var reader = new StreamReader(stream);
            var sourceCode = reader.ReadToEnd();
            hooks = scriptEngine.LoadMapScript(sourceCode, Path.GetFileName(scriptPath));
        }

        return new MapScriptHost(hooks, match, hookTimeout);
    }

    public void NotifyMatchStarted(MatchState match) =>
        ScriptHookInvoker.Invoke("OnPlayerTurnStart", _hookTimeout, () =>
        {
            var context = MapScriptContextFactory.Create(match, _world);
            _hooks.OnPlayerTurnStart(context);
        });

    public void NotifyPlayerTurnStart(MatchState match) =>
        ScriptHookInvoker.Invoke("OnPlayerTurnStart", _hookTimeout, () =>
        {
            var context = MapScriptContextFactory.Create(match, _world);
            _hooks.OnPlayerTurnStart(context);
        });

    public void NotifyAfterPlayerAction(MatchState match, MatchPlayerAction action) =>
        ScriptHookInvoker.Invoke("OnAfterPlayerAction", _hookTimeout, () =>
        {
            var context = MapScriptContextFactory.Create(
                match,
                _world,
                MapScriptContextFactory.FromMatchAction(action));
            _hooks.OnAfterPlayerAction(context);
        });
}
