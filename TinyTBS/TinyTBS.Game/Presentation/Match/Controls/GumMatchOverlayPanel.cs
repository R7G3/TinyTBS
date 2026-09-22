using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.Match.Controls;

/// <summary>Centered modal panel shell (background + inner stack placeholder).</summary>
internal static class GumMatchOverlayPanel
{
    public static Panel Create(
        Panel root,
        float widthPercent,
        float centerXPercent,
        float centerYPercent,
        Microsoft.Xna.Framework.Color background)
    {
        var panel = new Panel();
        GumUiLayout.CenterInParent(panel, xPercent: centerXPercent, yPercent: centerYPercent);
        GumUiLayout.SetWidthPercent(panel, widthPercent);
        panel.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        root.AddChild(panel);

        GumUiLayout.AddSolidBackground(panel, background);
        return panel;
    }

    public static Panel AddContentStack(Panel overlayPanel, float spacing, float widthPercent = 92f)
    {
        var stack = GumUiLayout.CreateVerticalStackPanel(spacing: spacing, widthPercent: widthPercent);
        stack.Visual.HasEvents = false;
        GumUiLayout.CenterHorizontallyInParent(stack);
        overlayPanel.AddChild(stack);
        return stack;
    }
}
