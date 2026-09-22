using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchShopOverlayView
{
    private Panel? _panel;
    private Label? _statusLabel;
    private Panel? _offersPanel;
    private Button? _closeButton;
    private Action<int>? _onBuyOffer;
    private IReadOnlyList<GameplayShopOfferViewModel> _lastOffers = [];
    private readonly List<Button> _offerButtons = [];
    private readonly List<ShopOfferRowIcon> _offerIcons = [];

    public void Build(Panel root, GameplayHudViewModel hud, Action onCloseShop, Action<int> onBuyOffer)
    {
        _panel = GumMatchOverlayPanel.Create(root, widthPercent: 52f, centerXPercent: 50f, centerYPercent: 50f, MatchUiColors.OverlayShop);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 8f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Castle shop" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _statusLabel = new Label { Text = hud.ShopStatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        _offersPanel = new Panel();
        _offersPanel.Visual.HasEvents = false;
        _offersPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _offersPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _offersPanel.Visual.StackSpacing = 8;
        GumUiLayout.FillParentWidth(_offersPanel);
        stack.AddChild(_offersPanel);

        var closeButton = new Button { Text = "Close" };
        closeButton.Visual.Width = 72;
        closeButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        closeButton.Click += (_, _) => onCloseShop();
        stack.AddChild(closeButton);
        _closeButton = closeButton;

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        _onBuyOffer = onBuyOffer;
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = hud.ShopStatusText;

        RebuildOffers(hud);
        GumMatchVisibility.SetVisible(_panel, hud.IsShopVisible);
    }

    public void FocusFirstOffer()
    {
        foreach (var offerButton in _offerButtons)
        {
            if (!offerButton.IsEnabled)
                continue;
            offerButton.IsFocused = true;
            return;
        }

        if (_closeButton is not null)
            _closeButton.IsFocused = true;
    }

    public void ClearFocus()
    {
        foreach (var button in _offerButtons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        if (_closeButton is { IsFocused: true })
            _closeButton.IsFocused = false;
    }

    public void SyncIcons(MatchTextureAtlas textures, int currentPlayerIndex)
    {
        if (_panel is null || !_panel.IsVisible)
            return;

        var teamColor = PlayerPalette.ForPlayer(currentPlayerIndex);
        foreach (var icon in _offerIcons)
        {
            var sprite = textures.Unit(icon.UnitKind);
            icon.BaseSprite.Texture = sprite.Base;
            icon.MaskSprite.Texture = sprite.Mask;
            icon.MaskSprite.Color = teamColor;
        }
    }

    private void RebuildOffers(GameplayHudViewModel hud)
    {
        if (_offersPanel is null || _onBuyOffer is null)
            return;

        if (GumShopOfferRowBuilder.OffersEqual(_lastOffers, hud.ShopOffers))
            return;

        _lastOffers = hud.ShopOffers;
        _offersPanel.Visual.Children.Clear();
        _offerButtons.Clear();
        _offerIcons.Clear();

        foreach (var offer in hud.ShopOffers)
        {
            GumShopOfferRowBuilder.AddOfferRow(
                _offersPanel,
                offer,
                _onBuyOffer,
                _offerButtons,
                _offerIcons);
        }
    }
}
