using TinyTBS.Game.Assets;
using TinyTBS.Engine.IO;
using TinyTBS.Game;

namespace TinyTBS.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        var userData = new DesktopUserDataPaths();
        var files = new FileSystemContentProvider();
        var bundledContentRoot = Path.Combine(AppContext.BaseDirectory, "Content");
        var assets = new ModAssetResolver(userData, files, bundledContentRoot);
        var filePicker = DesktopExternalFilePickers.CreateDefault();
        var uriLauncher = new DesktopShellExternalUriLauncher();

        using var game = new GameMain(userData, files, assets, filePicker, uriLauncher);
        game.Run();
    }
}
