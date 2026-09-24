using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Scripting;

namespace TinyTBS.Game.Match;

/// <summary>
/// Bundle for a running match: logic handle, scene, overlays, scripts, owned textures, content catalog.
/// </summary>
public sealed class GameplaySession : IDisposable
{
    private readonly MatchTextureAtlas _textures;

    internal GameplaySession(
        MatchState state,
        MatchScene scene,
        CursorHighlightRenderer cursorHighlight,
        MatchTextureAtlas textures,
        MapScriptHost scriptHost,
        MatchLevelBrief levelBrief,
        MinimapRenderer minimap,
        MatchContentCatalog contentCatalog)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        ScriptHost = scriptHost;
        LevelBrief = levelBrief;
        Minimap = minimap;
        ContentCatalog = contentCatalog;
        _textures = textures;
    }

    public MatchState State { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public MapScriptHost ScriptHost { get; }

    public MatchLevelBrief LevelBrief { get; }

    public MinimapRenderer Minimap { get; }

    public MatchContentCatalog ContentCatalog { get; }

    public MatchTextureAtlas Textures => _textures;

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

    public bool TryBuyShopOffer(int offerIndex, GridCell castleCell)
    {
        if (offerIndex < 0 || offerIndex >= ContentCatalog.ShopOffers.Count)
            return false;

        var offer = ContentCatalog.ShopOffers[offerIndex];
        if (!ContentCatalog.TryGetUnit(offer.UnitKind, out var unitDefinition))
            return false;

        if (!State.TryRecruitAtCastle(
                offer.UnitKind,
                offer.Cost,
                unitDefinition.MaxHealth,
                castleCell))
            return false;

        if (State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
        return true;
    }

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        Minimap.Dispose();
        _textures.Dispose();
    }
}
