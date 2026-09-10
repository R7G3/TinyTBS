using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;

namespace TinyTBS.Game.Match;

/// <summary>
/// Engine bundle for a running match: logic handle, scene, overlays, owned textures.
/// </summary>
public sealed class GameplaySession : IDisposable
{
    private readonly LoadedTexture _unitTexture;

    internal GameplaySession(
        MatchState state,
        MatchScene scene,
        CursorHighlightRenderer cursorHighlight,
        LoadedTexture unitTexture)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        _unitTexture = unitTexture;
    }

    public MatchState State { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        _unitTexture.DisposeIfOwned();
    }
}
