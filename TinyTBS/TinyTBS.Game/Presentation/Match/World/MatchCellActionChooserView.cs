using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using RenderingLibrary.Graphics;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.World;

public sealed class MatchCellActionChooserView
{
    private Panel? _panel;
    private Panel? _buttonHost;
    private Button? _moveButton;
    private Button? _buyButton;
    private readonly List<Button> _buttons = [];
    private float _lastLayoutWidth = -1f;

    public void Build(Panel root, Action onCellActionMove, Action onCellActionBuy)
    {
        _panel = new Panel();
        _panel.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        _panel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        _panel.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
        _panel.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        _panel.Visual.XOrigin = HorizontalAlignment.Center;
        _panel.Visual.YOrigin = VerticalAlignment.Bottom;
        root.AddChild(_panel);

        GumUiLayout.AddSolidBackground(_panel, MatchUiColors.OverlayDark);

        var stack = new Panel();
        stack.Visual.HasEvents = false;
        stack.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        stack.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.Visual.StackSpacing = 0;
        _panel.AddChild(stack);

        GumUiLayout.AddVerticalSpacer(stack, 6f);

        _buttonHost = new Panel();
        _buttonHost.Visual.HasEvents = false;
        _buttonHost.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        _buttonHost.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.AddChild(_buttonHost);

        _moveButton = new Button { Text = "->" };
        _moveButton.Click += (_, _) => onCellActionMove();
        _buttons.Add(_moveButton);

        _buyButton = new Button { Text = "$" };
        _buyButton.Click += (_, _) => onCellActionBuy();
        _buttons.Add(_buyButton);

        GumUiLayout.AddVerticalSpacer(stack, 6f);
        ApplyButtonLayout(availableWidth: 160f);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        GumMatchVisibility.SetVisible(_panel, hud.IsCellActionChooserVisible);
        Place(hud);
        ApplyButtonLayout(ResolveAvailableWidth());
    }

    public void FocusMove()
    {
        if (_moveButton is not null)
            _moveButton.IsFocused = true;
    }

    public void ClearFocus()
    {
        if (_moveButton is { IsFocused: true })
            _moveButton.IsFocused = false;

        if (_buyButton is { IsFocused: true })
            _buyButton.IsFocused = false;
    }

    private void Place(GameplayHudViewModel hud)
    {
        if (_panel is null || !hud.IsCellActionChooserVisible)
            return;

        const float gapAboveCell = 6f;
        var anchorY = MathF.Max(52f, hud.CellActionChooserAnchorY - gapAboveCell);
        _panel.X = hud.CellActionChooserAnchorX;
        _panel.Y = anchorY;
    }

    private float ResolveAvailableWidth()
    {
        var canvasWidth = GumService.Default.CanvasWidth;
        if (canvasWidth <= 0f)
            return 160f;

        // Keep the floating chooser readable on phone-like windows without overflowing the cell.
        return Math.Clamp(canvasWidth * 0.35f, 100f, 180f);
    }

    private void ApplyButtonLayout(float availableWidth)
    {
        if (_buttonHost is null || _buttons.Count == 0)
            return;

        if (Math.Abs(availableWidth - _lastLayoutWidth) <= 0.5f)
            return;

        _lastLayoutWidth = availableWidth;
        GumUiLayout.LayoutAdaptiveButtonRows(
            _buttonHost,
            _buttons,
            availableWidth,
            spacing: 8f,
            minButtonWidth: 48f,
            preferredButtonWidth: 72f);
    }
}
