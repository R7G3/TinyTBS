using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Presentation.Match.Board;
using TinyTBS.Rules.Match;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>
/// A running match on screen: the presentation-free <see cref="MatchRuntime"/> plus the board cursor,
/// scene, renderers and textures it owns. Unit selection lives on <see cref="MatchCursor"/>, not in the rules.
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
        CellLabelRenderer cellLabels)
    {
        Runtime = runtime;
        Cursor = cursor;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        Textures = textures;
        Minimap = minimap;
        CellLabels = cellLabels;
    }

    public MatchRuntime Runtime { get; }

    public MatchState State => Runtime.State;

    /// <summary>Board cursor and unit selection, shared by players and the bot's visible hand.</summary>
    public MatchCursor Cursor { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public CellLabelRenderer CellLabels { get; }

    public MinimapRenderer Minimap { get; }

    public MatchTextureAtlas Textures { get; }

    /// <summary>Confirm (click / A) at the board cursor.</summary>
    public bool Confirm() => Apply(Runtime.ConfirmAt(Cursor.Cell, Cursor.SelectedUnitId));

    public bool TryApply(MatchAction action) => Apply(Runtime.TryApply(action, Cursor.SelectedUnitId));

    public bool EndTurn() => Apply(Runtime.EndTurn(Cursor.SelectedUnitId));

    public bool ClearSelection() => Apply(Runtime.ClearSelection(Cursor.SelectedUnitId));

    public bool TryWaitSelectedUnit() => Apply(Runtime.TryWaitSelectedUnit(Cursor.SelectedUnitId));

    public bool TryBuyShopOffer(int offerIndex, GridCell castleCell) =>
        Apply(Runtime.TryBuyShopOffer(offerIndex, castleCell, Cursor.SelectedUnitId));

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        Minimap.Dispose();
        Textures.Dispose();
        Runtime.Dispose();
    }

    private bool Apply(MatchApplyResult result)
    {
        Cursor.SelectedUnitId = result.SelectedUnitId;
        return result.Applied;
    }
}
