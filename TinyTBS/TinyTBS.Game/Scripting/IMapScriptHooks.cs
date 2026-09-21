using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>Named hooks invoked by the match host (only these entry points).</summary>
public interface IMapScriptHooks
{
    void OnPlayerTurnStart(MapScriptContext context);

    void OnAfterPlayerAction(MapScriptContext context);
}
