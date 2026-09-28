using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Presentation.Match;
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
        MatchContentCatalog contentCatalog,
        UnitLevelLabelRenderer unitLevelLabels)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        ScriptHost = scriptHost;
        LevelBrief = levelBrief;
        Minimap = minimap;
        ContentCatalog = contentCatalog;
        UnitLevelLabels = unitLevelLabels;
        _textures = textures;
    }

    public MatchState State { get; }

    public MatchScene Scene { get; }

    public CursorHighlightRenderer CursorHighlight { get; }

    public UnitLevelLabelRenderer UnitLevelLabels { get; }

    public MapScriptHost ScriptHost { get; }

    public MatchLevelBrief LevelBrief { get; }

    public MinimapRenderer Minimap { get; }

    public MatchContentCatalog ContentCatalog { get; }

    public MatchTextureAtlas Textures => _textures;

    public void EndTurn()
    {
        if (State.IsMatchOver)
            return;

        State.EndTurn();
        if (!State.IsMatchOver)
            ScriptHost.NotifyPlayerTurnStart(State);
        State.EvaluateStandardOutcome();
    }

    public void Confirm()
    {
        if (State.IsMatchOver)
            return;

        State.HandleConfirm();
        if (State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
        State.EvaluateStandardOutcome();
    }

    /// <summary>Finish selected unit without attack/capture (face north / Y).</summary>
    public bool TryWaitSelectedUnit()
    {
        if (State.IsMatchOver)
            return false;
        if (!State.TryWaitSelectedUnit())
            return false;

        if (State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
        State.EvaluateStandardOutcome();
        return true;
    }

    public bool TryBuyShopOffer(int offerIndex, GridCell castleCell)
    {
        if (State.IsMatchOver)
            return false;
        if (offerIndex < 0 || offerIndex >= ContentCatalog.ShopOffers.Count)
            return false;

        var offer = ContentCatalog.ShopOffers[offerIndex];
        if (!ContentCatalog.TryGetUnit(offer.UnitTypeId, out var unitDefinition))
            return false;

        if (!State.TryRecruitAtCastle(
                offer.UnitTypeId,
                offer.Cost,
                unitDefinition.MaxHealth,
                castleCell))
            return false;

        if (State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
        State.EvaluateStandardOutcome();
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
