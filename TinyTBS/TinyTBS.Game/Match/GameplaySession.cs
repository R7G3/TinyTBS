using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Modules.Models;
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
        UnitLevelLabelRenderer unitLevelLabels,
        IReadOnlyList<MatchPlayerSeat> playerSeats,
        MatchContentComposition composition,
        IReadOnlyDictionary<string, string> moduleVersions)
    {
        State = state;
        Scene = scene;
        CursorHighlight = cursorHighlight;
        ScriptHost = scriptHost;
        LevelBrief = levelBrief;
        Minimap = minimap;
        ContentCatalog = contentCatalog;
        UnitLevelLabels = unitLevelLabels;
        PlayerSeats = playerSeats;
        Composition = composition;
        ModuleVersions = moduleVersions;
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

    public IReadOnlyList<MatchPlayerSeat> PlayerSeats { get; }

    /// <summary>Content composition used to start / resume this match (for saves).</summary>
    public MatchContentComposition Composition { get; }

    /// <summary>Module id → version at session build time.</summary>
    public IReadOnlyDictionary<string, string> ModuleVersions { get; }

    /// <summary>When non-null, this match is a campaign chapter.</summary>
    public CampaignRunState? CampaignRun { get; set; }

    public MatchTextureAtlas Textures => _textures;

    public bool IsCurrentPlayerBot()
    {
        var index = State.CurrentPlayer;
        return index >= 0
            && index < PlayerSeats.Count
            && PlayerSeats[index].Kind == MatchPlayerKind.Bot;
    }

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

    /// <summary>Recruit by offer identity (bot AI).</summary>
    public bool TryBuyShopOffer(ContentId unitTypeId, int cost, GridCell castleCell)
    {
        for (var i = 0; i < ContentCatalog.ShopOffers.Count; i++)
        {
            var offer = ContentCatalog.ShopOffers[i];
            if (offer.UnitTypeId == unitTypeId && offer.Cost == cost)
                return TryBuyShopOffer(i, castleCell);
        }

        return false;
    }

    public void Dispose()
    {
        Scene.Dispose();
        CursorHighlight.Dispose();
        Minimap.Dispose();
        _textures.Dispose();
    }
}
