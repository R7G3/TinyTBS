using Gum;
using Gum.Forms;
using XnaGame = Microsoft.Xna.Framework.Game;

namespace TinyTBS.Game.Gum;

/// <summary>
/// One-time Gum initialization for the game instance.
/// </summary>
internal static class GumBootstrap
{
    public static void Initialize(XnaGame game)
    {
        GumService.Default.Initialize(game, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader!.XnaContentManager = game.Content;
        GumService.Default.UseKeyboardDefaults();
        GumService.Default.UseGamepadDefaults();

        // Canvas tracks the back buffer; GumService.Update reapplies fit on resize.
        GumService.Default.EnableExpandToWindow(1f);
    }
}
