using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.Content;

/// <summary>Adds list rows into the content library scroll panel and registers focusables.</summary>
internal sealed class ContentLibraryListBuilder
{
    private readonly Panel _listPanel;
    private readonly List<(Button Button, Action Activate)> _focusableEntries;

    public ContentLibraryListBuilder(
        Panel listPanel,
        List<(Button Button, Action Activate)> focusableEntries)
    {
        _listPanel = listPanel ?? throw new ArgumentNullException(nameof(listPanel));
        _focusableEntries = focusableEntries ?? throw new ArgumentNullException(nameof(focusableEntries));
    }

    public void AddRow(string text, Action onActivate, bool isEnabled = true)
    {
        var button = new Button { Text = text, IsEnabled = isEnabled };
        GumUiLayout.FillParentWidth(button);
        if (isEnabled)
        {
            button.Click += (_, _) => onActivate();
            _focusableEntries.Add((button, onActivate));
        }
        else
        {
            button.Visual.HasEvents = false;
        }

        _listPanel.AddChild(button);
    }

    public void AddTwoLineRow(string titleLine, string detailLine, Action onActivate)
    {
        // Same pattern as shop offer rows: Dock.Fill button grows with the content shell,
        // so focus highlight covers both title and detail lines.
        const float padding = 6f;

        var row = new Panel();
        row.Visual.HasEvents = false;
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        GumUiLayout.FillParentWidth(row);
        _listPanel.AddChild(row);

        var button = new Button { Text = string.Empty, IsEnabled = true };
        button.Dock(Dock.Fill);
        button.Click += (_, _) => onActivate();
        row.AddChild(button);
        _focusableEntries.Add((button, onActivate));

        var shell = new Panel();
        shell.Visual.HasEvents = false;
        shell.Visual.X = padding;
        shell.Visual.Y = 0;
        shell.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        shell.Visual.Width = -(padding * 2);
        shell.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        shell.Visual.StackSpacing = 2f;
        row.AddChild(shell);

        GumUiLayout.AddVerticalSpacer(shell, padding);

        var title = new Label { Text = titleLine };
        title.Visual.HasEvents = false;
        title.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        title.Visual.Width = 0;
        title.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.AddChild(title);

        var detail = new Label { Text = detailLine };
        detail.Visual.HasEvents = false;
        detail.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        detail.Visual.Width = 0;
        detail.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        shell.AddChild(detail);

        GumUiLayout.AddVerticalSpacer(shell, padding);
    }

    public void AddHint(string text)
    {
        var label = new Label { Text = text };
        GumUiLayout.FillParentWidth(label);
        _listPanel.AddChild(label);
    }
}
