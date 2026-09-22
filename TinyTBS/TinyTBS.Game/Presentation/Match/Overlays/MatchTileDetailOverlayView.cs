using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.GueDeriving;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchTileDetailOverlayView
{
    private Panel? _panel;
    private Label? _headerLabel;
    private Label? _terrainLabel;
    private Label? _buildingLabel;
    private Label? _unitLabel;
    private Panel? _buildingRow;
    private Panel? _unitRow;
    private GumTeamIconSlot? _terrainIcon;
    private GumTeamIconSlot? _buildingIcon;
    private GumTeamIconSlot? _unitIcon;

    public void Build(Panel root, GameplayHudViewModel hud)
    {
        _panel = GumMatchOverlayPanel.Create(root, widthPercent: 56f, centerXPercent: 50f, centerYPercent: 50f, MatchUiColors.OverlayDark);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 14f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Tile detail" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _headerLabel = new Label { Text = hud.DetailHeaderText };
        GumUiLayout.FillParentWidth(_headerLabel);
        stack.AddChild(_headerLabel);

        GumDetailContentRow.Add(
            stack,
            hud.DetailTerrainText,
            withTeamMask: false,
            out _terrainLabel,
            out _,
            out _terrainIcon);

        GumDetailContentRow.Add(
            stack,
            hud.DetailBuildingText,
            withTeamMask: true,
            out _buildingLabel,
            out _buildingRow,
            out _buildingIcon);

        GumDetailContentRow.Add(
            stack,
            hud.DetailUnitText,
            withTeamMask: true,
            out _unitLabel,
            out _unitRow,
            out _unitIcon);

        GumUiLayout.AddVerticalSpacer(stack, 14f);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_headerLabel is not null)
            _headerLabel.Text = hud.DetailHeaderText;
        if (_terrainLabel is not null)
            _terrainLabel.Text = hud.DetailTerrainText;
        if (_buildingLabel is not null)
            _buildingLabel.Text = hud.DetailBuildingText;
        if (_unitLabel is not null)
            _unitLabel.Text = hud.DetailUnitText;

        GumMatchVisibility.SetVisible(_buildingRow, hud.HasDetailBuilding);
        GumMatchVisibility.SetVisible(_unitRow, hud.HasDetailUnit);
        GumMatchVisibility.SetVisible(_panel, hud.IsTileDetailVisible);
    }

    public void SyncIcons(MatchTextureAtlas textures, MatchState match)
    {
        if (_panel is null || !_panel.IsVisible)
            return;

        if (_terrainIcon is not null)
            _terrainIcon.BaseSprite.Texture = textures.Terrain(match.GetTerrain(match.Cursor));

        if (_buildingRow is { IsVisible: true }
            && _buildingIcon?.MaskSprite is not null
            && match.TryGetBuildingAt(match.Cursor, out var building))
        {
            var sprite = textures.Building(building.Kind);
            _buildingIcon.BaseSprite.Texture = sprite.Base;
            _buildingIcon.MaskSprite.Texture = sprite.Mask;
            _buildingIcon.MaskSprite.Color = PlayerPalette.ForOwner(building.OwnerPlayerIndex);
        }

        if (_unitRow is { IsVisible: true }
            && _unitIcon?.MaskSprite is not null
            && match.TryGetUnitAt(match.Cursor, out var unit))
        {
            var sprite = textures.Unit(unit.Kind);
            _unitIcon.BaseSprite.Texture = sprite.Base;
            _unitIcon.MaskSprite.Texture = sprite.Mask;
            _unitIcon.MaskSprite.Color = PlayerPalette.ForPlayer(unit.PlayerIndex);
        }
    }
}
