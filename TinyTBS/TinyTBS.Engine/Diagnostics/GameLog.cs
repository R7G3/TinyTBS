namespace TinyTBS.Engine.Diagnostics;

/// <summary>
/// Session log file under <c>{UserData}/Logs</c>. Thread-safe; messages written before
/// <see cref="Initialize"/> are dropped. Logging never throws into game code.
/// </summary>
public static class GameLog
{
    private const string SessionFilePrefix = "tinytbs-";
    private const string SessionFileExtension = ".log";
    private const int KeptSessionFiles = 10;

    private static readonly object Sync = new();
    private static string? _filePath;

    /// <summary>Current session log file, or null before <see cref="Initialize"/>.</summary>
    public static string? FilePath => _filePath;

    public static void Initialize(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                DeleteOldSessionFiles(logDirectory);
                _filePath = Path.Combine(
                    logDirectory,
                    SessionFilePrefix + DateTime.Now.ToString("yyyyMMdd-HHmmss") + SessionFileExtension);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _filePath = null;
            }
        }

        Info("Session started.");
    }

    public static void Info(string message) => Write("INFO", message, exception: null);

    public static void Warning(string message, Exception? exception = null) => Write("WARN", message, exception);

    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        if (exception is not null)
            line += Environment.NewLine + exception;

        lock (Sync)
        {
            if (_filePath is null)
                return;

            try
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
            catch (Exception writeException) when (writeException is IOException or UnauthorizedAccessException)
            {
                // A broken log file must not take the game down with it.
            }
        }
    }

    private static void DeleteOldSessionFiles(string logDirectory)
    {
        var sessionFiles = Directory
            .GetFiles(logDirectory, SessionFilePrefix + "*" + SessionFileExtension)
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .Skip(KeptSessionFiles - 1);

        foreach (var path in sessionFiles)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception deleteException) when (deleteException is IOException or UnauthorizedAccessException)
            {
                // Old logs are best-effort housekeeping.
            }
        }
    }
}
