using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using RenderingLibrary.Graphics;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.Match.Controls;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.World;

public sealed class MatchCellActionChooserView
{
    private Panel? _panel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries = [];
    private readonly MenuVerticalNavigateRepeat _navigateRepeat = new();
    private int _focusIndex;

    public void Build(Panel root, Action onCellActionMove, Action onCellActionBuy)
    {
        _focusableEntries.Clear();
        _navigateRepeat.Reset();
        _focusIndex = 0;
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

        // Floater is sized to children — do not use LayoutAdaptiveButtonRows (FillParentWidth
        // against RelativeToChildren collapses the row and breaks hit-tests).
        var buttonRow = new Panel();
        buttonRow.Visual.HasEvents = false;
        buttonRow.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        buttonRow.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        buttonRow.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        buttonRow.Visual.StackSpacing = 8;
        stack.AddChild(buttonRow);

        AddHorizontalPad(buttonRow, 6f);

        var moveButton = new Button { Text = "->" };
        GumUiLayout.SetAbsoluteWidth(moveButton, 72f);
        moveButton.Click += (_, _) => onCellActionMove();
        buttonRow.AddChild(moveButton);
        _focusableEntries.Add((moveButton, onCellActionMove));

        var buyButton = new Button { Text = "$" };
        GumUiLayout.SetAbsoluteWidth(buyButton, 56f);
        buyButton.Click += (_, _) => onCellActionBuy();
        buttonRow.AddChild(buyButton);
        _focusableEntries.Add((buyButton, onCellActionBuy));

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
        _focusIndex = 0;
        GumFocusableButtonList.ApplyFocus(_focusableEntries, ref _focusIndex);
    }

    /// <summary>Call after Gum.Update while the chooser is open. Owns D-pad / stick / Confirm.</summary>
    public void HandleGamepadNavigation(IGameCommandSource commands, float elapsedSeconds)
    {
        if (_panel is not { IsVisible: true } || _focusableEntries.Count == 0)
            return;

        GumFocusableButtonList.HandleVerticalInput(
            commands,
            _focusableEntries,
            ref _focusIndex,
            _navigateRepeat,
            elapsedSeconds,
            mapHorizontalToVertical: true);
    }

    public void ClearFocus()
    {
        GumFocusableButtonList.ClearFocus(_focusableEntries);
        _focusIndex = 0;
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
