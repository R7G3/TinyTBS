using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Scripting;

namespace TinyTBS.Game.Match;

/// <summary>
/// Bundle for a running match: logic handle, scene, overlays, scripts, owned textures.
/// </summary>
public sealed class GameplaySession : IDisposable
{
    private readonly MatchTextureAtlas _textures;

    internal GameplaySession(
        MatchState state,
        MatchScene scene,
        CursorHighlightRenderer cursorHighlight,
        MatchTextureAtlas textures,
        MapScriptHost scriptHost)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        ScriptHost = scriptHost;
        _textures = textures;
    }

    public MatchState State { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public MapScriptHost ScriptHost { get; }

    public void EndTurn()
    {
        State.EndTurn();
        ScriptHost.NotifyPlayerTurnStart(State);
    }

    public void Confirm()
    {
        State.HandleConfirm();
        if (State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
    }

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        _textures.Dispose();
    }
}
