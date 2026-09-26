using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Overlays;

public sealed class MatchShopOverlayView
{
    /// <summary>Fallback when layout has not measured yet (icon + multiline stats + padding).</summary>
    private const float OfferRowHeightEstimate = 110f;

    private const float OfferRowSpacing = 8f;

    /// <summary>V3 ScrollViewer border / clip insets that AbsoluteHeight of InnerPanel does not include.</summary>
    private const float ScrollViewerChromeHeight = 36f;

    private const float ScrollIntoViewMargin = 6f;
    private const float OffersListBottomPadding = 3f;
    private const float OverlayChromeHeight = 220f;

    private Panel? _panel;
    private Label? _statusLabel;
    private ScrollViewer? _offersScroll;
    private Panel? _offersPanel;
    private Button? _closeButton;
    private Action<int>? _onBuyOffer;
    private IReadOnlyList<GameplayShopOfferViewModel> _lastOffers = [];
    private readonly List<Button> _offerButtons = [];
    private readonly List<Panel> _offerRows = [];
    private readonly List<ShopOfferRowIcon> _offerIcons = [];

    /// <summary>0..N-1 = offer rows; N = Close. Owned by us so Gum spatial nav cannot land on the scrollbar.</summary>
    private int _focusIndex;

    public void Build(Panel root, GameplayHudViewModel hud, Action onCloseShop, Action<int> onBuyOffer)
    {
        _panel = GumMatchOverlayPanel.Create(root, maxWidthPixels: 480f, centerXPercent: 50f, centerYPercent: 50f, MatchUiColors.OverlayShop);
        var stack = GumMatchOverlayPanel.AddContentStack(_panel, spacing: 8f);

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        var title = new Label { Text = "Castle shop" };
        GumUiLayout.FillParentWidth(title);
        stack.AddChild(title);

        _statusLabel = new Label { Text = hud.ShopStatusText };
        GumUiLayout.FillParentWidth(_statusLabel);
        stack.AddChild(_statusLabel);

        _offersScroll = new ScrollViewer();
        GumUiLayout.FillParentWidth(_offersScroll);
        _offersScroll.Visual.HeightUnits = DimensionUnitType.Absolute;
        _offersScroll.InnerPanel.WidthUnits = DimensionUnitType.RelativeToParent;
        _offersScroll.InnerPanel.Width = 0;
        _offersScroll.InnerPanel.HeightUnits = DimensionUnitType.RelativeToChildren;
        // Auto: mouse wheel + thumb work. Gamepad never focuses the bar — we own D-pad navigation.
        _offersScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ApplyShopListBackground(_offersScroll);
        DisableScrollChromeFocus(_offersScroll);
        stack.AddChild(_offersScroll);

        _offersPanel = new Panel();
        _offersPanel.Visual.HasEvents = false;
        _offersPanel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _offersPanel.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        _offersPanel.Visual.StackSpacing = OfferRowSpacing;
        GumUiLayout.FillParentWidth(_offersPanel);
        _offersScroll.AddChild(_offersPanel);

        var closeButton = new Button { Text = "Close" };
        closeButton.Visual.Width = 72;
        closeButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        closeButton.Click += (_, _) => onCloseShop();
        stack.AddChild(closeButton);
        _closeButton = closeButton;

        GumUiLayout.AddVerticalSpacer(stack, 14f);

        _onBuyOffer = onBuyOffer;
        ApplyResponsiveLayout(hud.ShopOffers.Count);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = hud.ShopStatusText;

        RebuildOffers(hud);
        ApplyResponsiveLayout(hud.ShopOffers.Count);
        if (hud.IsShopVisible)
            MaintainShopFocusAndScroll();

        GumMatchVisibility.SetVisible(_panel, hud.IsShopVisible);
    }

    /// <summary>
    /// Call after Gum.Update while the shop is open. Owns D-pad / stick so focus never lands on the scrollbar.
    /// </summary>
    public void HandleGamepadNavigation(IGameCommandSource commands)
    {
        if (_panel is not { IsVisible: true })
            return;

        if (commands.WasPressed(GameCommand.NavigateDown))
        {
            _focusIndex = Math.Min(_focusIndex + 1, FocusSlotCount - 1);
            ApplyFocusIndex();
            return;
        }

        if (commands.WasPressed(GameCommand.NavigateUp))
        {
            _focusIndex = Math.Max(_focusIndex - 1, 0);
            ApplyFocusIndex();
            return;
        }

        MaintainShopFocusAndScroll();
    }

    public void FocusFirstOffer()
    {
        _focusIndex = 0;
        for (var index = 0; index < _offerButtons.Count; index++)
        {
            if (!_offerButtons[index].IsEnabled)
                continue;
            _focusIndex = index;
            ApplyFocusIndex();
            return;
        }

        _focusIndex = Math.Max(FocusSlotCount - 1, 0);
        ApplyFocusIndex();
    }

    public void ClearFocus()
    {
        ClearAllFocusFlags();
        _focusIndex = 0;
    }

    public void SyncIcons(MatchTextureAtlas textures, int currentPlayerIndex)
    {
        if (_panel is null || !_panel.IsVisible)
            return;

        var teamColor = PlayerPalette.ForPlayer(currentPlayerIndex);
        foreach (var icon in _offerIcons)
        {
            var sprite = textures.Unit(icon.UnitTypeId);
            icon.BaseSprite.Texture = sprite.Base;
            icon.MaskSprite.Texture = sprite.Mask;
            icon.MaskSprite.Color = teamColor;
        }
    }

    private int FocusSlotCount => _offerButtons.Count + (_closeButton is null ? 0 : 1);

    private void MaintainShopFocusAndScroll()
    {
        StealFocusFromScrollChrome();
        SyncFocusIndexFromUi();
        // Only re-apply focus if chrome stole it. Do not scroll every frame —
        // that fights mouse-wheel scrolling on the ScrollViewer.
        if (!IsOurFocusIntact())
            ApplyFocusIndex();
    }

    private bool IsOurFocusIntact()
    {
        if (_focusIndex < _offerButtons.Count)
            return _offerButtons[_focusIndex].IsFocused;
        return _closeButton is { IsFocused: true };
    }

    private void SyncFocusIndexFromUi()
    {
        for (var index = 0; index < _offerButtons.Count; index++)
        {
            if (!_offerButtons[index].IsFocused)
                continue;
            _focusIndex = index;
            return;
        }

        if (_closeButton is { IsFocused: true })
            _focusIndex = _offerButtons.Count;
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
        _offerRows.Clear();
        _offerIcons.Clear();

        foreach (var offer in hud.ShopOffers)
        {
            var row = GumShopOfferRowBuilder.AddOfferRow(
                _offersPanel,
                offer,
                _onBuyOffer,
                _offerButtons,
                _offerIcons);
            _offerRows.Add(row);
        }

        // Extra space under the last row so focus outline is not clipped by the ScrollViewer edge.
        GumUiLayout.AddVerticalSpacer(_offersPanel, OffersListBottomPadding);

        if (_offersScroll is not null)
            _offersScroll.VerticalScrollBarValue = 0;

        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(FocusSlotCount - 1, 0));
    }

    private void ApplyResponsiveLayout(int offerCount)
    {
        if (_panel is null || _offersScroll is null)
            return;

        var canvasWidth = GumService.Default.CanvasWidth;
        var canvasHeight = GumService.Default.CanvasHeight;
        if (canvasWidth <= 0f || canvasHeight <= 0f)
            return;

        var maxWidth = Math.Clamp(canvasWidth * 0.42f, 400f, 800f);
        GumUiLayout.SetBoundedWidth(_panel, maxWidth, parentPercent: 92f);

        var heightCap = Math.Max(120f, canvasHeight - OverlayChromeHeight);
        var preferredFloor = Math.Min(280f, heightCap);
        var maxListHeight = Math.Clamp(canvasHeight * 0.55f, preferredFloor, heightCap);
        var contentHeight = MeasureOffersContentHeight(offerCount);
        var desiredListHeight = contentHeight + ScrollViewerChromeHeight;
        var listHeight = offerCount == 0
            ? 200f
            : Math.Min(maxListHeight, Math.Max(desiredListHeight, OfferRowHeightEstimate));

        GumUiLayout.SetAbsoluteHeight(_offersScroll, listHeight);
        _panel.Visual.MaxHeight = Math.Max(canvasHeight * 0.9f, listHeight + OverlayChromeHeight * 0.5f);

        _offersScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ApplyShopListBackground(_offersScroll);
        DisableScrollChromeFocus(_offersScroll);
    }

    private float MeasureOffersContentHeight(int offerCount)
    {
        if (_offersPanel is not null)
        {
            var measured = _offersPanel.Visual.AbsoluteHeight;
            if (measured > 1f)
                return measured;
        }

        return EstimateOffersContentHeight(offerCount);
    }

    private void ApplyFocusIndex()
    {
        if (FocusSlotCount <= 0)
            return;

        _focusIndex = Math.Clamp(_focusIndex, 0, FocusSlotCount - 1);
        ClearAllFocusFlags();

        if (_focusIndex < _offerButtons.Count)
        {
            _offerButtons[_focusIndex].IsFocused = true;
            ScrollOfferFullyIntoView(_focusIndex);
            return;
        }

        if (_closeButton is not null)
            _closeButton.IsFocused = true;
    }

    private void ClearAllFocusFlags()
    {
        foreach (var button in _offerButtons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        if (_offersScroll is { IsFocused: true })
            _offersScroll.IsFocused = false;

        ClearScrollBarFocus(_offersScroll);

        if (_closeButton is { IsFocused: true })
            _closeButton.IsFocused = false;
    }

    private void StealFocusFromScrollChrome()
    {
        if (_offersScroll is null)
            return;

        if (!_offersScroll.IsFocused && !IsScrollBarFocused(_offersScroll))
            return;

        ClearScrollBarFocus(_offersScroll);
        if (_offersScroll.IsFocused)
            _offersScroll.IsFocused = false;
    }

    private void ScrollOfferFullyIntoView(int offerIndex)
    {
        if (_offersScroll is null || offerIndex < 0 || offerIndex >= _offerRows.Count)
            return;

        var itemTop = MeasureOfferTop(offerIndex);
        var itemHeight = MeasureOfferHeight(offerIndex);
        if (itemHeight <= 0f)
            return;

        var viewportHeight = ResolveScrollViewportHeight();
        if (viewportHeight <= 1f)
            return;

        var viewTop = _offersScroll.VerticalScrollBarValue;
        var viewBottom = viewTop + viewportHeight;
        var itemBottom = itemTop + itemHeight;
        var margin = ScrollIntoViewMargin;

        if (itemTop < viewTop + margin)
            _offersScroll.VerticalScrollBarValue = Math.Max(0f, itemTop - margin);
        else if (itemBottom + OffersListBottomPadding > viewBottom - margin)
            _offersScroll.VerticalScrollBarValue = Math.Max(
                0f,
                itemBottom + OffersListBottomPadding - viewportHeight + margin);
    }

    private float MeasureOfferTop(int offerIndex)
    {
        var top = 0f;
        for (var index = 0; index < offerIndex; index++)
            top += MeasureOfferHeight(index) + OfferRowSpacing;
        return top;
    }

    private float MeasureOfferHeight(int offerIndex)
    {
        if (offerIndex < 0 || offerIndex >= _offerRows.Count)
            return OfferRowHeightEstimate;

        var height = _offerRows[offerIndex].Visual.AbsoluteHeight;
        if (height > 1f)
            return height;

        height = _offerRows[offerIndex].Visual.Height;
        return height > 1f ? height : OfferRowHeightEstimate;
    }

    private float ResolveScrollViewportHeight()
    {
        if (_offersScroll is null)
            return 0f;

        if (_offersScroll.Visual is ScrollViewerVisual visual
            && visual.ClipContainerInstance is not null
            && visual.ClipContainerInstance.AbsoluteHeight > 1f)
        {
            return visual.ClipContainerInstance.AbsoluteHeight;
        }

        var outer = _offersScroll.Visual.AbsoluteHeight;
        return outer > ScrollViewerChromeHeight
            ? outer - ScrollViewerChromeHeight
            : outer;
    }

    private static float EstimateOffersContentHeight(int offerCount)
    {
        if (offerCount <= 0)
            return 0f;
        return offerCount * OfferRowHeightEstimate + (offerCount - 1) * OfferRowSpacing;
    }

    private static void ApplyShopListBackground(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        // Match overlay panel fill so gaps between offer rows are not opaque grey.
        visual.BackgroundColor = MatchUiColors.OverlayShop;
    }

    private static void DisableScrollChromeFocus(ScrollViewer scrollViewer)
    {
        if (scrollViewer.Visual is not ScrollViewerVisual visual)
            return;

        if (visual.FocusedIndicator is not null)
            visual.FocusedIndicator.Visible = false;

        // Do not set HasEvents=false / IsEnabled=false on the bar — that breaks mouse scrolling.
        ClearScrollBarFocus(scrollViewer);
    }

    private static bool IsScrollBarFocused(ScrollViewer? scrollViewer)
    {
        if (scrollViewer?.Visual is not ScrollViewerVisual visual)
            return false;

        return IsFormsFocused(visual.VerticalScrollBarInstance)
            || IsFormsFocused(visual.HorizontalScrollBarInstance);
    }

    private static void ClearScrollBarFocus(ScrollViewer? scrollViewer)
    {
        if (scrollViewer?.Visual is not ScrollViewerVisual visual)
            return;

        ClearFormsFocus(visual.VerticalScrollBarInstance);
        ClearFormsFocus(visual.HorizontalScrollBarInstance);
    }

    private static bool IsFormsFocused(GraphicalUiElement? element)
    {
        if (element is ScrollBarVisual { FormsControl.IsFocused: true })
            return true;
        return false;
    }

    private static void ClearFormsFocus(GraphicalUiElement? element)
    {
        if (element is ScrollBarVisual { FormsControl.IsFocused: true } scrollBarVisual)
            scrollBarVisual.FormsControl.IsFocused = false;
    }
}
