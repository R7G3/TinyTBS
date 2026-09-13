using TinyTBS.Engine.Rendering;

namespace TinyTBS.Game.Match;

/// <summary>
/// Bundle for a running match: logic handle, scene, overlays, owned textures.
/// </summary>
public sealed class GameplaySession : IDisposable
{
    private readonly MatchTextureAtlas _textures;

    internal GameplaySession(
        MatchState state,
        MatchScene scene,
        CursorHighlightRenderer cursorHighlight,
        MatchTextureAtlas textures)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        _textures = textures;
    }

    public MatchState State { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        _textures.Dispose();
    }
}
