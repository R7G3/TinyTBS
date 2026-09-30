using TinyTBS.Rules.Match;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Maps live match state into the HUD view-model and pushes to Gum.</summary>
public sealed class GameplayHudSync
{
    private readonly GameplayHudViewModel _hud;
    private readonly GameplayHudComposer _composer;

    public GameplayHudSync(GameplayHudViewModel hud, GameplayHudComposer composer)
    {
        _hud = hud;
        _composer = composer;
    }

    public GameplayHudViewModel Hud => _hud;

    public void SetGoalsFromLevel(MatchLevelBrief brief) =>
        _hud.GoalsText = BuildGoalsText(brief);

    public void SyncFromSession(
        GameplaySession session,
        GridCell? cellActionChooserCell,
        MatchCampaignResult? campaignResult)
    {
        var match = session.State;
        var runtime = session.Runtime;
        var catalog = runtime.ContentCatalog;
        var cursorCell = session.Cursor.Cell;

        _hud.PlayerLabel = FormatPlayerLabel(runtime, match.CurrentPlayer);
        _hud.GoldText = $"{match.GetMoney(match.CurrentPlayer)}g";
        _hud.IncomeText = "Income: " + MatchEconomy.CalculateOwnedBuildingIncome(match, match.CurrentPlayer) + "g";
        _hud.UnitsText = $"{match.CountUnitsForPlayer(match.CurrentPlayer)}/{runtime.LevelBrief.UnitCap}";
        _hud.TurnText = $"Turn {match.TurnNumber}";
        _hud.StatusBarColor = PlayerPalette.ForPlayer(match.CurrentPlayer);
        _hud.CompactInfoText = MatchInfoFormatter.FormatCompact(match, catalog, cursorCell);
        _hud.DetailTerrainText = MatchInfoFormatter.FormatTerrainDetail(match, cursorCell);
        _hud.DetailBuildingText = MatchInfoFormatter.FormatBuildingDetail(match, catalog, cursorCell);
        _hud.DetailUnitText = MatchInfoFormatter.FormatUnitDetail(match, catalog, cursorCell);
        _hud.HasDetailBuilding = !string.IsNullOrEmpty(_hud.DetailBuildingText);
        _hud.HasDetailUnit = !string.IsNullOrEmpty(_hud.DetailUnitText);
        _hud.InfoPreferLeft = cursorCell.X >= match.Width / 2;
        _hud.InfoPreferTop = cursorCell.Y >= match.Height / 2;

        if (_hud.IsCellActionChooserVisible && cellActionChooserCell is { } chooserCell)
        {
            var anchor = session.Scene.Layout.GetCellTopCenter(chooserCell.X, chooserCell.Y);
            _hud.CellActionChooserAnchorX = anchor.X;
            _hud.CellActionChooserAnchorY = anchor.Y;
        }

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
            _hud.MatchResultText = FormatMatchResult(match, runtime);
            if (campaignResult is not null)
            {
                _hud.ShowMatchResultNextChapter = campaignResult.ShowNextChapter;
                _hud.ShowMatchResultRetry = campaignResult.ShowRetry;
                if (!string.IsNullOrEmpty(campaignResult.Note))
                    _hud.MatchResultText += Environment.NewLine + campaignResult.Note;
            }

            _hud.Overlays.Open(MatchOverlay.MatchResult);
        }

        _composer.Sync(_hud);

        if (_hud.IsTileDetailVisible)
            _composer.SyncDetailIcons(session.Textures, match, cursorCell);
        if (_hud.IsShopVisible)
            _composer.SyncShopIcons(session.Textures, match.CurrentPlayer);
    }

    private static string FormatPlayerLabel(MatchRuntime runtime, int playerIndex) =>
        (uint)playerIndex < (uint)runtime.PlayerSeats.Count
            ? PlayerDisplayNames.ForSeat(playerIndex, runtime.PlayerSeats[playerIndex])
            : PlayerDisplayNames.Number(playerIndex);

    private static string FormatMatchResult(MatchState match, MatchRuntime runtime)
    {
        if (match.WinnerPlayerIndex is not int winner)
            return "Match over";

        var who = FormatPlayerLabel(runtime, winner);
        var reason = string.IsNullOrWhiteSpace(match.VictoryReason) ? "victory" : match.VictoryReason;
        return MatchConditionTypes.IsStandard(reason)
            ? who + " wins"
            : who + " wins (" + reason + ")";
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
