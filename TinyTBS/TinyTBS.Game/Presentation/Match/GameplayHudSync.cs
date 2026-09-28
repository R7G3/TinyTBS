using TinyTBS.Game.Ai;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Maps live match state into the HUD view-model and pushes to Gum.</summary>
public sealed class GameplayHudSync
{
    private readonly GameplayHudViewModel _hud;
    private readonly GameplayHudComposer _composer;
    private bool _campaignResultResolved;

    public GameplayHudSync(GameplayHudViewModel hud, GameplayHudComposer composer)
    {
        _hud = hud;
        _composer = composer;
    }

    public GameplayHudViewModel Hud => _hud;

    public ChapterEndResult? LastChapterEndResult { get; private set; }

    public void SetGoalsFromLevel(MatchLevelBrief brief) =>
        _hud.GoalsText = BuildGoalsText(brief);

    public void SyncFromSession(
        GameplaySession session,
        GridCell? cellActionChooserCell,
        CampaignProgressService? campaignService = null)
    {
        var match = session.State;

        _hud.PlayerLabel = $"P{match.CurrentPlayer + 1}";
        _hud.GoldText = $"{match.GetMoney(match.CurrentPlayer)}g";
        _hud.UnitsText = $"{match.CountUnitsForPlayer(match.CurrentPlayer)}/{session.LevelBrief.UnitCap}";
        _hud.TurnText = $"Turn {match.TurnNumber}";
        _hud.StatusBarColor = PlayerPalette.ForPlayer(match.CurrentPlayer);
        _hud.CompactInfoText = MatchInfoFormatter.FormatCompact(match, session.ContentCatalog);
        _hud.DetailTerrainText = MatchInfoFormatter.FormatTerrainDetail(match);
        _hud.DetailBuildingText = MatchInfoFormatter.FormatBuildingDetail(match, session.ContentCatalog);
        _hud.DetailUnitText = MatchInfoFormatter.FormatUnitDetail(match, session.ContentCatalog);
        _hud.HasDetailBuilding = !string.IsNullOrEmpty(_hud.DetailBuildingText);
        _hud.HasDetailUnit = !string.IsNullOrEmpty(_hud.DetailUnitText);
        _hud.InfoPreferLeft = match.Cursor.X >= match.Width / 2;
        _hud.InfoPreferTop = match.Cursor.Y >= match.Height / 2;

        if (_hud.IsCellActionChooserVisible && cellActionChooserCell is { } chooserCell)
        {
            var anchor = session.Scene.Layout.GetCellTopCenter(chooserCell.X, chooserCell.Y);
            _hud.CellActionChooserAnchorX = anchor.X;
            _hud.CellActionChooserAnchorY = anchor.Y;
        }

        var catalog = session.ContentCatalog;
        _hud.ShopOffers = catalog.ShopOffers
            .Select((offer, index) => (offer, index))
            .Where(entry => !match.IsUniqueUnitOwnedByCurrentPlayer(entry.offer.UnitTypeId))
            .Select(entry =>
            {
                var cost = match.ResolveRecruitCost(entry.offer.UnitTypeId, entry.offer.Cost);
                return new GameplayShopOfferViewModel
                {
                    UnitTypeId = entry.offer.UnitTypeId,
                    Name = catalog.DisplayName(entry.offer.UnitTypeId),
                    StatsText = catalog.FormatCombatStats(entry.offer.UnitTypeId),
                    Cost = cost,
                    CanAfford = match.GetMoney(match.CurrentPlayer) >= cost
                        && match.CountUnitsForPlayer(match.CurrentPlayer) < match.UnitCap,
                    OfferIndex = entry.index,
                };
            })
            .ToArray();

        if (_hud.IsShopVisible && string.IsNullOrEmpty(_hud.ShopStatusText))
            _hud.ShopStatusText = "Recruit onto the castle cell.";

        if (match.IsMatchOver)
        {
            _hud.MatchResultText = FormatMatchResult(match);
            GameplayHudOverlayState.PrepareForMatchResult(_hud);
            ResolveCampaignResultOnce(session, campaignService);
        }

        _composer.Sync(_hud);

        if (_hud.IsTileDetailVisible)
            _composer.SyncDetailIcons(session.Textures, match);
        if (_hud.IsShopVisible)
            _composer.SyncShopIcons(session.Textures, match.CurrentPlayer);
    }

    private void ResolveCampaignResultOnce(
        GameplaySession session,
        CampaignProgressService? campaignService)
    {
        if (_campaignResultResolved || session.CampaignRun is null || campaignService is null)
            return;

        _campaignResultResolved = true;
        var run = session.CampaignRun;
        var campaign = campaignService.TryLoadDefinition(run.ScenarioModuleId);
        if (campaign is null)
        {
            _hud.ShowMatchResultNextChapter = false;
            _hud.ShowMatchResultRetry = true;
            return;
        }

        var localWon = IsLocalPlayerWinner(session);
        try
        {
            LastChapterEndResult = localWon
                ? campaignService.ApplyChapterWon(run, campaign)
                : campaignService.ApplyChapterLost(run, campaign);
        }
        catch (Exception exception)
        {
            _hud.MatchResultText += $"{Environment.NewLine}Progress error: {exception.Message}";
            _hud.ShowMatchResultNextChapter = false;
            _hud.ShowMatchResultRetry = true;
            return;
        }

        _hud.ShowMatchResultNextChapter = LastChapterEndResult.HasNextChapter;
        _hud.ShowMatchResultRetry = !localWon;
        if (localWon && LastChapterEndResult.HasNextChapter)
            _hud.MatchResultText += $"{Environment.NewLine}Next: {LastChapterEndResult.NextLevelId}";
        else if (localWon)
            _hud.MatchResultText += $"{Environment.NewLine}Campaign complete";
    }

    private static bool IsLocalPlayerWinner(GameplaySession session)
    {
        if (session.State.WinnerPlayerIndex is not int winner)
            return false;

        if (winner < 0 || winner >= session.PlayerSeats.Count)
            return winner == 0;

        return session.PlayerSeats[winner].Kind == MatchPlayerKind.Local;
    }

    private static string FormatMatchResult(MatchState match)
    {
        if (match.WinnerPlayerIndex is not int winner)
            return "Match over";

        var reason = string.IsNullOrWhiteSpace(match.VictoryReason) ? "victory" : match.VictoryReason;
        return reason.Equals("standard", StringComparison.OrdinalIgnoreCase)
            ? $"Player {winner + 1} wins"
            : $"Player {winner + 1} wins ({reason})";
    }

    private static string BuildGoalsText(MatchLevelBrief brief)
    {
        var description = string.IsNullOrWhiteSpace(brief.Description)
            ? brief.Title
            : brief.Description;
        return $"{description}{Environment.NewLine}{Environment.NewLine}"
            + $"Victory: {brief.VictoryType}{Environment.NewLine}"
            + $"Defeat: {brief.DefeatType}{Environment.NewLine}"
            + $"Team defeat: {brief.TeamDefeatMode}";
    }
}
