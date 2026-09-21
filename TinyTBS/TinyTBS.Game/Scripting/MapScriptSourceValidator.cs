using System.Text.RegularExpressions;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Static text checks before compiling a map script (sandbox level 2).
/// Not a hard sandbox — blocks common escape/DoS patterns, including Linux/Android surfaces.
/// </summary>
public static partial class MapScriptSourceValidator
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    /// <summary>Namespaces that open filesystem, network, native, process, or mobile platform APIs.</summary>
    private static readonly string[] ForbiddenUsingPrefixes =
    [
        // Desktop / general BCL escape hatches
        "System.IO",
        "System.Net",
        "System.Reflection",
        "System.Diagnostics",
        "System.Runtime.InteropServices",
        "System.Runtime.Loader",
        "System.Runtime.Memory",
        "System.Threading",
        "System.CodeDom",
        "Microsoft.Win32",
        "Microsoft.CodeAnalysis",

        // Unix / Linux interop
        "Mono.Unix",
        "Mono.Posix",

        // Android / Java interop (future mobile hosts)
        "Android",
        "AndroidX",
        "Java",
        "Javax",
        "Dalvik",
        "Org.Apache",
        "Xamarin",
        "Java.Interop",
        "Android.Runtime",
    ];

    /// <summary>
    /// Substrings / API names that remain dangerous even with fully-qualified calls
    /// (no <c>using</c>), including Linux paths and Android JNI surfaces.
    /// </summary>
    private static readonly string[] ForbiddenSubstrings =
    [
        // Assembly / scripting host escape
        "#r ",
        "#r\"",
        "Assembly.Load",
        "Assembly.LoadFrom",
        "Assembly.LoadFile",
        "AssemblyLoadContext",
        "AppDomain",
        "Activator.CreateInstance",
        "Type.GetType",
        "Reflection.Emit",

        // Process / environment (Linux: shell, /proc; Android: zygote abuse)
        "Process.Start",
        "Process.GetProcesses",
        "Process.Kill",
        "System.Diagnostics.Process",
        "Environment.Exit",
        "Environment.GetEnvironmentVariable",
        "Environment.SetEnvironmentVariable",
        "Environment.ExpandEnvironmentVariables",
        "Environment.GetFolderPath",
        "Environment.FailFast",

        // Native interop (libc / libdl / libandroid)
        "DllImport",
        "LibraryImport",
        "UnmanagedCallersOnly",
        "NativeLibrary",
        "Marshal.",
        "GCHandle",
        "unsafe",
        "stackalloc",
        "delegate*",

        // IO / IPC without using (fully qualified)
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.Path",
        "System.IO.FileStream",
        "System.IO.FileInfo",
        "System.IO.DirectoryInfo",
        "System.IO.MemoryMappedFiles",
        "System.IO.Pipes",
        "File.",
        "Directory.",
        "FileStream",
        "FileInfo",
        "DirectoryInfo",
        "DriveInfo",
        "MemoryMappedFile",
        "NamedPipe",
        "Path.GetTemp",
        "CreateSymbolicLink",
        "UnixFileMode",

        // Network
        "HttpClient",
        "WebClient",
        "TcpClient",
        "UdpClient",
        "HttpListener",
        "System.Net.Http",
        "System.Net.Sockets",
        "new Socket(",

        // Linux sensitive virtual FS / devices (string literals in scripts)
        "/proc/",
        "/sys/",
        "/dev/",
        "/etc/",

        // Android / Java bridge
        "content://",
        "file://",
        "JNIEnv",
        "JniEnv",
        "JNINative",
        "Java.Lang",
        "Java.IO",
        "Android.App",
        "Android.Content",
        "Android.OS",
        "Android.System",
        "Android.Content.Intent",
        "new Intent(",
        "ContentResolver",
        "Runtime.getRuntime",
        "Os.exec",
        "Os.open",
        "libandroid",
        "liblog",
        "libc.so",
        "libdl.so",
    ];

    public static void Validate(string sourceCode)
    {
        if (sourceCode.Contains("#r", StringComparison.OrdinalIgnoreCase))
            throw new MapScriptException("Map scripts must not use '#r' directives.");

        foreach (System.Text.RegularExpressions.Match match in UsingDirectiveRegex().Matches(sourceCode))
        {
            var importedNamespace = match.Groups["name"].Value.Trim();
            foreach (var forbiddenPrefix in ForbiddenUsingPrefixes)
            {
                if (importedNamespace.Equals(forbiddenPrefix, StringComparison.Ordinal)
                    || importedNamespace.StartsWith(forbiddenPrefix + ".", StringComparison.Ordinal))
                {
                    throw new MapScriptException(
                        $"Map scripts must not import '{importedNamespace}'.");
                }
            }
        }

        foreach (var forbidden in ForbiddenSubstrings)
        {
            if (ContainsForbiddenToken(sourceCode, forbidden))
            {
                throw new MapScriptException(
                    $"Map scripts must not use '{forbidden.Trim()}'.");
            }
        }

        if (FixedStatementRegex().IsMatch(sourceCode))
        {
            throw new MapScriptException(
                "Map scripts must not use 'fixed' statements (native memory pinning).");
        }
    }

    public static bool IsEffectivelyEmpty(string sourceCode)
    {
        var withoutBlockComments = BlockCommentRegex().Replace(sourceCode, string.Empty);
        foreach (var line in withoutBlockComments.Split(LineSeparators, StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
                continue;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Word-sensitive check for keywords like <c>unsafe</c> so identifiers such as
    /// <c>IsUnsafe</c> are not rejected; paths and API names use plain Contains.
    /// </summary>
    private static bool ContainsForbiddenToken(string sourceCode, string forbidden)
    {
        if (forbidden is "unsafe")
            return UnsafeKeywordRegex().IsMatch(sourceCode);

        return sourceCode.Contains(forbidden, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^\s*using\s+(?<name>[A-Za-z0-9_.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex UsingDirectiveRegex();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex BlockCommentRegex();

    [GeneratedRegex(@"\bunsafe\b")]
    private static partial Regex UnsafeKeywordRegex();

    [GeneratedRegex(@"\bfixed\s*\(")]
    private static partial Regex FixedStatementRegex();
}
