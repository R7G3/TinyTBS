using TinyTBS.Core.Assets;
using TinyTBS.Core.IO;
using TinyTBS.Game;

namespace TinyTBS.Desktop;

internal static class Program
{
    private static void Main(string[] args)
    {
        var userData = new DesktopUserDataPaths();
        var files = new FileSystemContentProvider();
        var bundledContent = Path.Combine(AppContext.BaseDirectory, "Content");
        var assets = new ModAssetResolver(userData, files, bundledContent);

        using var game = new GameMain(userData, files, assets);
        game.Run();
    }
}
