using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.Presentation.Match.Board;

namespace TinyTBS.Game.Match.Session;

/// <summary>
/// A running match on screen: the presentation-free <see cref="MatchRuntime"/> plus the board cursor,
/// scene, renderers and textures it owns.
/// </summary>
public sealed class GameplaySession : IDisposable
{
    internal GameplaySession(
        MatchRuntime runtime,
        MatchCursor cursor,
        MatchScene scene,
        CursorHighlightRenderer cursorHighlight,
        MatchTextureAtlas textures,
        MinimapRenderer minimap,
        UnitLevelLabelRenderer unitLevelLabels)
    {
        Runtime = runtime;
        Cursor = cursor;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        Textures = textures;
        Minimap = minimap;
        UnitLevelLabels = unitLevelLabels;
    }

    public MatchRuntime Runtime { get; }

    public MatchState State => Runtime.State;

    /// <summary>Board cursor shared by players and the bot's visible "hand".</summary>
    public MatchCursor Cursor { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public UnitLevelLabelRenderer UnitLevelLabels { get; }

    public MinimapRenderer Minimap { get; }

    public MatchTextureAtlas Textures { get; }

    /// <summary>Confirm (click / A) at the board cursor.</summary>
    public bool Confirm() => Runtime.ConfirmAt(Cursor.Cell);

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        Minimap.Dispose();
        Textures.Dispose();
        Runtime.Dispose();
    }
}
