using System.Text.RegularExpressions;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Static text checks before compiling user scripts (sandbox level 2).
/// Not a hard sandbox — blocks common escape/DoS patterns, including Linux/Android surfaces.
/// </summary>
public static partial class ScriptSourceValidator
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    /// <summary>Namespaces that open filesystem, network, native, process, or mobile platform APIs.</summary>
    private static readonly string[] ForbiddenUsingPrefixes =
    [
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
        "Mono.Unix",
        "Mono.Posix",
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

    private static readonly string[] ForbiddenSubstrings =
    [
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
        "DllImport",
        "LibraryImport",
        "UnmanagedCallersOnly",
        "NativeLibrary",
        "Marshal.",
        "GCHandle",
        "unsafe",
        "stackalloc",
        "delegate*",
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
        "HttpClient",
        "WebClient",
        "TcpClient",
        "UdpClient",
        "HttpListener",
        "System.Net.Http",
        "System.Net.Sockets",
        "new Socket(",
        "/proc/",
        "/sys/",
        "/dev/",
        "/etc/",
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
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (sourceCode.Contains("#r", StringComparison.OrdinalIgnoreCase))
            throw new ScriptHostException("Scripts must not use '#r' directives.");

        foreach (System.Text.RegularExpressions.Match match in UsingDirectiveRegex().Matches(sourceCode))
        {
            var importedNamespace = match.Groups["name"].Value.Trim();
            foreach (var forbiddenPrefix in ForbiddenUsingPrefixes)
            {
                if (importedNamespace.Equals(forbiddenPrefix, StringComparison.Ordinal)
                    || importedNamespace.StartsWith(forbiddenPrefix + ".", StringComparison.Ordinal))
                {
                    throw new ScriptHostException(
                        $"Scripts must not import '{importedNamespace}'.");
                }
            }
        }

        foreach (var forbidden in ForbiddenSubstrings)
        {
            if (ContainsForbiddenToken(sourceCode, forbidden))
            {
                throw new ScriptHostException(
                    $"Scripts must not use '{forbidden.Trim()}'.");
            }
        }

        if (FixedStatementRegex().IsMatch(sourceCode))
        {
            throw new ScriptHostException(
                "Scripts must not use 'fixed' statements (native memory pinning).");
        }
    }

    public static bool IsEffectivelyEmpty(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

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
