using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Microsoft.Xna.Framework;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Adds list rows into the New Game scroll panel and registers focusables.</summary>
internal sealed class NewGameListBuilder
{
    private const float PlayerSwatchSize = 28f;
    private const float PlayerSwatchGap = 10f;
    private const float PlayerRemoveWidth = 36f;

    private readonly Panel _listPanel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries;

    public NewGameListBuilder(
        Panel listPanel,
        List<(Button Button, Action Activate)> focusableEntries)
    {
        _listPanel = listPanel ?? throw new ArgumentNullException(nameof(listPanel));
        _focusableEntries = focusableEntries ?? throw new ArgumentNullException(nameof(focusableEntries));
    }

    public int FocusableCount => _focusableEntries.Count;

    public void AddRow(string text, Action onActivate)
    {
        var button = new Button { Text = text, IsEnabled = true };
        GumUiLayout.FillParentWidth(button);
        button.Click += (_, _) => onActivate();
        _listPanel.AddChild(button);
        _focusableEntries.Add((button, onActivate));
    }

    public void AddDisabledRow(string text)
    {
        var button = new Button { Text = text, IsEnabled = false };
        button.Visual.HasEvents = false;
        GumUiLayout.FillParentWidth(button);
        _listPanel.AddChild(button);
    }

    public void AddHint(string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _listPanel.AddChild(label);
    }

    /// <summary>
    /// Slot row: palette swatch + caption + optional X (remove). Returns X focus index, or -1.
    /// </summary>
    public int AddPlayerSlotRow(
        string summary,
        Color swatchColor,
        bool showRemove,
        bool canRemove,
        Action? onRemove)
    {
        var trailingWidth = showRemove ? PlayerSwatchGap + PlayerRemoveWidth : 0f;

        var row = new Panel();
        row.Visual.HasEvents = false;
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.MinHeight = PlayerSwatchSize;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = PlayerSwatchGap;
        GumUiLayout.FillParentWidth(row);
        _listPanel.AddChild(row);

        var swatchHost = new Panel();
        swatchHost.Visual.HasEvents = false;
        GumUiLayout.SetAbsoluteWidth(swatchHost, PlayerSwatchSize);
        GumUiLayout.SetAbsoluteHeight(swatchHost, PlayerSwatchSize);
        GumUiLayout.AddSolidBackground(swatchHost, swatchColor);
        row.AddChild(swatchHost);

        var label = new Label { Text = summary };
        label.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        label.Visual.Width = -(PlayerSwatchSize + PlayerSwatchGap + trailingWidth);
        label.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.AddChild(label);

        if (!showRemove)
            return -1;

        if (canRemove && onRemove is not null)
        {
            var removeButton = new Button { Text = "X", IsEnabled = true };
            GumUiLayout.SetAbsoluteWidth(removeButton, PlayerRemoveWidth);
            removeButton.Click += (_, _) => onRemove();
            row.AddChild(removeButton);
            _focusableEntries.Add((removeButton, onRemove));
            return _focusableEntries.Count - 1;
        }

        var disabledRemove = new Button { Text = "X", IsEnabled = false };
        disabledRemove.Visual.HasEvents = false;
        GumUiLayout.SetAbsoluteWidth(disabledRemove, PlayerRemoveWidth);
        row.AddChild(disabledRemove);
        return -1;
    }

    /// <summary>
    /// Value caption + −/+ without Gum Click (press + hold-repeat is owned by the view).
    /// </summary>
    public NewGameValueStepperWidgets AddValueStepper(
        string label,
        int value,
        Action onDecrease,
        Action onIncrease)
    {
        var caption = new Label { Text = $"{label}: {value}" };
        GumUiLayout.FillParentWidth(caption);
        _listPanel.AddChild(caption);

        // ASCII hyphen — Gum default font often lacks U+2212 (minus sign).
        var decreaseButton = AddStepperButton("-", onDecrease);
        var decreaseFocusIndex = _focusableEntries.Count - 1;
        var increaseButton = AddStepperButton("+", onIncrease);
        var increaseFocusIndex = _focusableEntries.Count - 1;

        return new NewGameValueStepperWidgets
        {
            ValueLabel = caption,
            DecreaseButton = decreaseButton,
            IncreaseButton = increaseButton,
            DecreaseFocusIndex = decreaseFocusIndex,
            IncreaseFocusIndex = increaseFocusIndex,
        };
    }

    private Button AddStepperButton(string text, Action onActivate)
    {
        var button = new Button { Text = text, IsEnabled = true };
        GumUiLayout.FillParentWidth(button);
        _listPanel.AddChild(button);
        _focusableEntries.Add((button, onActivate));
        return button;
    }
}

