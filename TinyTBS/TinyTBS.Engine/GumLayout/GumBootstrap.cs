using Gum;
using Gum.Forms;
using XnaGame = Microsoft.Xna.Framework.Game;

namespace TinyTBS.Engine.GumLayout;

/// <summary>
/// One-time Gum initialization for the MonoGame instance.
/// </summary>
public static class GumBootstrap
{
    public static void Initialize(XnaGame game)
    {
        GumService.Default.Initialize(game, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader!.XnaContentManager = game.Content;
        GumService.Default.UseKeyboardDefaults();
        // Do NOT call UseGamepadDefaults: Gum spatial nav fights GameCommand-driven
        // menu focus (D-pad / stick would double-step or land on scrollbars).
        // Gamepad is polled only via GameCommandService -> screen HandleInput.

        // Canvas tracks the back buffer; GumService.Update reapplies fit on resize.
        GumService.Default.EnableExpandToWindow(1f);

        // V3 default focus is a bottom bar; menus use a full outline around the control.
        GumFocusOutline.InstallDefaultButtonTemplate();
    }
}
