using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>No-op hooks when a map has no script or only comments.</summary>
public sealed class NoOpMapScriptHooks : IMapScriptHooks
{
    public void OnPlayerTurnStart(MapScriptContext context)
    {
    }

    public void OnAfterPlayerAction(MapScriptContext context)
    {
    }
}
