using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.Match.Controls;

/// <summary>One row: icon left, multiline label right.</summary>
internal static class GumDetailContentRow
{
    public static void Add(
        Panel parent,
        string text,
        bool withTeamMask,
        out Label textLabel,
        out Panel row,
        out GumTeamIconSlot iconSlot)
    {
        const float iconSize = GumTeamIconSlot.DefaultIconSize;

        row = new Panel();
        row.Visual.HasEvents = false;
        row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Visual.MinHeight = iconSize;
        row.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.Visual.StackSpacing = 12;
        GumUiLayout.FillParentWidth(row);
        parent.AddChild(row);

        iconSlot = GumTeamIconSlot.AddToRow(row, withTeamMask, iconSize);

        textLabel = new Label { Text = text };
        textLabel.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
        textLabel.Visual.Width = -(iconSize + 12f);
        textLabel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.AddChild(textLabel);
    }
}
