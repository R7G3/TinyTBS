using TinyTBS.Game.Match;
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
        GridCell? cellActionChooserCell)
    {
        var match = session.State;

        _hud.PlayerLabel = $"P{match.CurrentPlayer + 1}";
        _hud.GoldText = $"{match.GetMoney(match.CurrentPlayer)}g";
        _hud.TurnText = $"Turn {match.TurnNumber}";
        _hud.StatusBarColor = PlayerPalette.ForPlayer(match.CurrentPlayer);
        _hud.CompactInfoText = MatchInfoFormatter.FormatCompact(match, session.ContentCatalog);
        _hud.DetailHeaderText = MatchInfoFormatter.FormatDetailHeader(match);
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
            .Select((offer, index) => new GameplayShopOfferViewModel
            {
                UnitTypeId = offer.UnitTypeId,
                Name = catalog.DisplayName(offer.UnitTypeId),
                StatsText = catalog.FormatCombatStats(offer.UnitTypeId),
                Cost = offer.Cost,
                CanAfford = match.GetMoney(match.CurrentPlayer) >= offer.Cost,
                OfferIndex = index,
            })
            .ToArray();

        if (_hud.IsShopVisible && string.IsNullOrEmpty(_hud.ShopStatusText))
            _hud.ShopStatusText = "Recruit onto the castle cell.";

        _composer.Sync(_hud);

        if (_hud.IsTileDetailVisible)
            _composer.SyncDetailIcons(session.Textures, match);
        if (_hud.IsShopVisible)
            _composer.SyncShopIcons(session.Textures, match.CurrentPlayer);
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
