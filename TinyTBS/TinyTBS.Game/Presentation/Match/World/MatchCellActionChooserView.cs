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
    private Button? _moveButton;
    private Button? _buyButton;

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

        var buttonRow = new Panel();
        buttonRow.Visual.HasEvents = false;
        buttonRow.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        buttonRow.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        buttonRow.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        buttonRow.Visual.StackSpacing = 8;
        stack.AddChild(buttonRow);

        AddHorizontalPad(buttonRow, 6f);

        _moveButton = new Button { Text = "->" };
        _moveButton.Visual.Width = 72;
        _moveButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        _moveButton.Click += (_, _) => onCellActionMove();
        buttonRow.AddChild(_moveButton);

        _buyButton = new Button { Text = "$" };
        _buyButton.Visual.Width = 56;
        _buyButton.Visual.WidthUnits = DimensionUnitType.Absolute;
        _buyButton.Click += (_, _) => onCellActionBuy();
        buttonRow.AddChild(_buyButton);

        AddHorizontalPad(buttonRow, 6f);

        GumUiLayout.AddVerticalSpacer(stack, 6f);
    }

    public void Sync(GameplayHudViewModel hud)
    {
        GumMatchVisibility.SetVisible(_panel, hud.IsCellActionChooserVisible);
        Place(hud);
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

    private static void AddHorizontalPad(Panel row, float width)
    {
        var pad = new Panel();
        pad.Visual.HasEvents = false;
        pad.Visual.Width = width;
        pad.Visual.WidthUnits = DimensionUnitType.Absolute;
        pad.Visual.Height = 1;
        pad.Visual.HeightUnits = DimensionUnitType.Absolute;
        row.AddChild(pad);
    }
}
