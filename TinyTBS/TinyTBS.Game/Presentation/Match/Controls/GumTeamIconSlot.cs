using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.Match.Controls;

/// <summary>Fixed-size icon area with optional base+mask sprites for team tinting.</summary>
internal sealed class GumTeamIconSlot
{
    public const float DefaultIconSize = 64f;

    private GumTeamIconSlot(Panel iconSlot, SpriteRuntime baseSprite, SpriteRuntime? maskSprite)
    {
        IconSlot = iconSlot;
        BaseSprite = baseSprite;
        MaskSprite = maskSprite;
    }

    public Panel IconSlot { get; }

    public SpriteRuntime BaseSprite { get; }

    public SpriteRuntime? MaskSprite { get; }

    /// <summary>Icon on the left of a horizontal row; text caller adds to the same row.</summary>
    public static GumTeamIconSlot AddToRow(Panel row, bool withTeamMask, float iconSize = DefaultIconSize)
    {
        var iconSlot = CreateIconPanel(iconSize);
        row.AddChild(iconSlot);
        return AttachSprites(iconSlot, withTeamMask);
    }

    /// <summary>Icon vertically centered in a column (height follows sibling text).</summary>
    public static GumTeamIconSlot AddCenteredInColumn(Panel column, bool withTeamMask, float iconSize = DefaultIconSize)
    {
        var iconSlot = CreateIconPanel(iconSize);
        iconSlot.Visual.X = 0;
        iconSlot.Visual.Y = 50f;
        iconSlot.Visual.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        iconSlot.Visual.YOrigin = VerticalAlignment.Center;
        column.AddChild(iconSlot);
        return AttachSprites(iconSlot, withTeamMask);
    }

    private static Panel CreateIconPanel(float iconSize)
    {
        var iconSlot = new Panel();
        iconSlot.Visual.HasEvents = false;
        iconSlot.Visual.Width = iconSize;
        iconSlot.Visual.WidthUnits = DimensionUnitType.Absolute;
        iconSlot.Visual.Height = iconSize;
        iconSlot.Visual.HeightUnits = DimensionUnitType.Absolute;
        GumUiLayout.AddSolidBackground(iconSlot, MatchUiColors.IconSlot);
        return iconSlot;
    }

    private static GumTeamIconSlot AttachSprites(Panel iconSlot, bool withTeamMask)
    {
        var baseSprite = new SpriteRuntime();
        baseSprite.Dock(Dock.Fill);
        iconSlot.AddChild(baseSprite);

        if (!withTeamMask)
            return new GumTeamIconSlot(iconSlot, baseSprite, maskSprite: null);

        var maskSprite = new SpriteRuntime();
        maskSprite.Dock(Dock.Fill);
        iconSlot.AddChild(maskSprite);
        return new GumTeamIconSlot(iconSlot, baseSprite, maskSprite);
    }
}
