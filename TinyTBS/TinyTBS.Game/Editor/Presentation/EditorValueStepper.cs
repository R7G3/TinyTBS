using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Presentation;

/// <summary>Shared −/+ value stepper row for editor forms.</summary>
public static class EditorValueStepper
{
    public static void AddRow(
        Panel parent,
        List<(Button Button, Action Activate)> settingsEntries,
        List<(Button Decrease, Button Increase)> stepperRows,
        HashSet<Button> stepperButtons,
        string caption,
        Func<string> readValueCaption,
        Action decrease,
        Action increase,
        Action afterFocusChange)
    {
        var row = new Panel();
        GumUiLayout.FillParentWidth(row);
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = 6f;
        parent.AddChild(row);

        var label = new Label { Text = caption };
        GumUiLayout.SetWidthPercent(label, 42f);
        row.AddChild(label);

        var valueLabel = new Label { Text = readValueCaption() };
        GumUiLayout.SetWidthPercent(valueLabel, 22f);
        row.AddChild(valueLabel);

        void OnDecrease()
        {
            decrease();
            valueLabel.Text = readValueCaption();
        }

        void OnIncrease()
        {
            increase();
            valueLabel.Text = readValueCaption();
        }

        var decreaseButton = CreateStepperButton("−", OnDecrease);
        var increaseButton = CreateStepperButton("+", OnIncrease);
        decreaseButton.Click += (_, _) => afterFocusChange();
        increaseButton.Click += (_, _) => afterFocusChange();
        row.AddChild(decreaseButton);
        row.AddChild(increaseButton);

        settingsEntries.Add((decreaseButton, OnDecrease));
        settingsEntries.Add((increaseButton, OnIncrease));
        stepperRows.Add((decreaseButton, increaseButton));
        stepperButtons.Add(decreaseButton);
        stepperButtons.Add(increaseButton);
    }

    public static Button CreateStepperButton(string text, Action onClick)
    {
        var button = new Button { Text = text };
        GumUiLayout.SetAbsoluteWidth(button, 44f);
        GumUiLayout.SetAbsoluteHeight(button, 36f);
        button.Click += (_, _) => onClick();
        return button;
    }

    public static bool TryMoveWithinRow(
        List<(Button Decrease, Button Increase)> stepperRows,
        List<(Button Button, Action Activate)> settingsEntries,
        ref int settingsFocusIndex,
        int horizontal)
    {
        if (settingsFocusIndex < 0 || settingsFocusIndex >= settingsEntries.Count)
            return false;

        var focused = settingsEntries[settingsFocusIndex].Button;
        foreach (var (decrease, increase) in stepperRows)
        {
            if (ReferenceEquals(focused, decrease) && horizontal > 0)
            {
                settingsFocusIndex = IndexOf(settingsEntries, increase);
                GumFocusableButtonList.ApplyFocus(settingsEntries, ref settingsFocusIndex);
                return true;
            }

            if (ReferenceEquals(focused, increase) && horizontal < 0)
            {
                settingsFocusIndex = IndexOf(settingsEntries, decrease);
                GumFocusableButtonList.ApplyFocus(settingsEntries, ref settingsFocusIndex);
                return true;
            }
        }

        return false;
    }

    private static int IndexOf(List<(Button Button, Action Activate)> entries, Button button)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Button, button))
                return i;
        }

        return 0;
    }
}
