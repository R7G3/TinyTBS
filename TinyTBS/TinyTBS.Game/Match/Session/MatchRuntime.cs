using TinyTBS.Rules.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Rules.Modules.Models;
using TinyTBS.Game.Scripting;

namespace TinyTBS.Game.Match.Session;

/// <summary>
/// A running match without presentation: rules state, map script hooks, seats and content setup.
/// Every player command goes through here so scripts see it and the standard outcome is re-evaluated.
/// </summary>
public sealed class MatchRuntime : IDisposable
{
    public MatchRuntime(
        MatchState state,
        MapScriptHost scriptHost,
        MatchLevelBrief levelBrief,
        IReadOnlyList<MatchPlayerSeat> playerSeats,
        MatchContentComposition composition,
        IReadOnlyDictionary<string, string> moduleVersions)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        ScriptHost = scriptHost ?? throw new ArgumentNullException(nameof(scriptHost));
        LevelBrief = levelBrief ?? throw new ArgumentNullException(nameof(levelBrief));
        PlayerSeats = playerSeats ?? throw new ArgumentNullException(nameof(playerSeats));
        Composition = composition ?? throw new ArgumentNullException(nameof(composition));
        ModuleVersions = moduleVersions ?? throw new ArgumentNullException(nameof(moduleVersions));
    }

    public MatchState State { get; }

    public MapScriptHost ScriptHost { get; }

    public MatchLevelBrief LevelBrief { get; }

    public IReadOnlyList<MatchPlayerSeat> PlayerSeats { get; }

    /// <summary>Content composition used to start / resume this match (for saves).</summary>
    public MatchContentComposition Composition { get; }

    /// <summary>Module id → version at session build time.</summary>
    public IReadOnlyDictionary<string, string> ModuleVersions { get; }

    /// <summary>When non-null, this match is a campaign chapter.</summary>
    public CampaignRunState? CampaignRun { get; set; }

    /// <summary>
    /// Overlay hints for a campaign chapter, set once when <see cref="MatchEnded"/> is handled.
    /// Null for a skirmish and until a campaign chapter actually ends.
    /// </summary>
    public MatchCampaignResult? CampaignChapterResult { get; private set; }

    public MatchContentCatalog ContentCatalog => State.ContentCatalog;

    private Action<MatchRuntime>? _matchEnded;
    private bool _matchEndedRaised;

    /// <summary>
    /// Fires once, after <see cref="StartFreshMatch"/> or a command leaves the match over.
    /// A subscriber that arrives later is called immediately.
    /// A resumed save that was already finished does not fire, so loading it does not apply the chapter again.
    /// </summary>
    public event Action<MatchRuntime>? MatchEnded
    {
        add
        {
            ArgumentNullException.ThrowIfNull(value);
            _matchEnded += value;
            if (_matchEndedRaised)
                value(this);
        }
        remove => _matchEnded -= value;
    }

    internal void SetCampaignChapterResult(MatchCampaignResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        CampaignChapterResult ??= result;
    }

    public bool IsCurrentPlayerBot()
    {
        var index = State.CurrentPlayer;
        return index >= 0
            && index < PlayerSeats.Count
            && PlayerSeats[index].Kind == MatchPlayerKind.Bot;
    }

    /// <summary>Fires the first turn-start hook of a fresh match (resumed saves skip it).</summary>
    public void StartFreshMatch()
    {
        ScriptHost.NotifyMatchStarted(State);
        State.EvaluateStandardOutcome();
        RaiseMatchEnded();
    }

    /// <summary>A Confirm (click / A) on <paramref name="cell"/>, resolved by the rules.</summary>
    public MatchApplyResult ConfirmAt(GridCell cell, int? selectedUnitId)
    {
        if (State.IsMatchOver)
            return new MatchApplyResult(false, null);

        var result = State.ConfirmAt(cell, selectedUnitId);
        return FinishCommand(result);
    }

    public MatchApplyResult TryApply(MatchAction action, int? selectedUnitId)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.Kind == MatchActionKind.EndTurn)
            return EndTurn(selectedUnitId);

        var result = State.TryApply(action, selectedUnitId);
        return FinishCommand(result);
    }

    public MatchApplyResult EndTurn(int? selectedUnitId)
    {
        var result = State.TryApply(MatchAction.EndTurn, selectedUnitId);
        if (!result.Applied)
            return FinishCommand(result);

        if (!State.IsMatchOver)
            ScriptHost.NotifyPlayerTurnStart(State);
        State.EvaluateStandardOutcome();
        RaiseMatchEnded();
        return result with { SelectedUnitId = State.NormalizeSelection(result.SelectedUnitId) };
    }

    public MatchApplyResult ClearSelection(int? selectedUnitId)
    {
        var result = State.ClearSelection(selectedUnitId);
        return result with { SelectedUnitId = State.NormalizeSelection(result.SelectedUnitId) };
    }

    /// <summary>Finish the selected unit without attack/capture (face north / Y).</summary>
    public MatchApplyResult TryWaitSelectedUnit(int? selectedUnitId) =>
        selectedUnitId is int unitId && State.TryGetUnit(unitId, out var unit)
            ? TryApply(MatchAction.WaitUnit(unitId, unit.Cell), selectedUnitId)
            : new MatchApplyResult(false, State.NormalizeSelection(selectedUnitId));

    public MatchApplyResult TryBuyShopOffer(int offerIndex, GridCell castleCell, int? selectedUnitId) =>
        offerIndex >= 0 && offerIndex < ContentCatalog.ShopOffers.Count
            ? TryApply(MatchAction.RecruitUnit(ContentCatalog.ShopOffers[offerIndex].UnitTypeId, castleCell), selectedUnitId)
            : new MatchApplyResult(false, State.NormalizeSelection(selectedUnitId));

    public void Dispose() => ScriptHost.Dispose();

    private MatchApplyResult FinishCommand(MatchApplyResult result)
    {
        AfterPlayerCommand(result.Applied);
        return result with { SelectedUnitId = State.NormalizeSelection(result.SelectedUnitId) };
    }

    private void AfterPlayerCommand(bool applied)
    {
        if (applied && State.LastAction is { } action)
            ScriptHost.NotifyAfterPlayerAction(State, action);
        State.EvaluateStandardOutcome();
        RaiseMatchEnded();
    }

    private void RaiseMatchEnded()
    {
        if (_matchEndedRaised || !State.IsMatchOver)
            return;

        _matchEndedRaised = true;
        _matchEnded?.Invoke(this);
    }
}
