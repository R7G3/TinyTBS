using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game;
using TinyTBS.Game.Assets;

namespace TinyTBS.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] _)
    {
        var userData = new DesktopUserDataPaths();
        GameLog.Initialize(userData.Logs);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            GameLog.Error("Unhandled exception.", eventArgs.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            GameLog.Error("Unobserved task exception.", eventArgs.Exception);

        var files = new FileSystemContentProvider();
        var bundledContentRoot = Path.Combine(AppContext.BaseDirectory, "Content");
        var assets = new ModAssetResolver(userData, files, bundledContentRoot);
        var filePicker = DesktopExternalFilePickers.CreateDefault();
        var uriLauncher = new DesktopShellExternalUriLauncher();

        try
        {
            using var game = new GameMain(userData, files, assets, filePicker, uriLauncher);
            game.Run();
        }
        catch (Exception exception)
        {
            GameLog.Error("Game loop terminated by an exception.", exception);
            throw;
        }

        GameLog.Info("Session ended.");
    }
}
