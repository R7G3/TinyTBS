using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace TinyTBS.Engine.IO;

/// <summary>
/// Linux open-file via xdg-desktop-portal (<c>org.freedesktop.portal.FileChooser</c>) using <c>gdbus</c>.
/// If portal/gdbus is unavailable, falls back to zenity/kdialog (often portal-backed under Flatpak).
/// </summary>
public sealed class XdgDesktopPortalExternalFilePicker : IExternalFilePicker
{
    private static readonly Regex RequestPathRegex = new(
        @"objectpath\s+['""](?<path>/org/freedesktop/portal/desktop/request/[^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UriRegex = new(
        @"['""](?<uri>file:[^'""]+)['""]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string? PickOpenFile(ExternalFilePickRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TryPickViaPortal(request)
            ?? TryPickViaZenity(request)
            ?? TryPickViaKdialog(request);
    }

    private static string? TryPickViaPortal(ExternalFilePickRequest request)
    {
        if (!CommandExists("gdbus"))
            return null;

        Process? monitor = null;
        try
        {
            // Monitor must start before OpenFile so we do not miss the Response signal.
            monitor = StartPortalMonitor();
            if (monitor is null)
                return null;

            var title = EscapeGdbusString(request.Title);
            var call = RunProcess(
                "gdbus",
                "call --session --dest org.freedesktop.portal.Desktop "
                + "--object-path /org/freedesktop/portal/desktop "
                + "--method org.freedesktop.portal.FileChooser.OpenFile "
                + $"\"\" \"{title}\" \"{{}}\"",
                timeoutMs: 15_000);

            if (call.ExitCode != 0 || string.IsNullOrWhiteSpace(call.StdOut))
                return null;

            if (!RequestPathRegex.IsMatch(call.StdOut))
                return null;

            return WaitForPortalResponse(monitor, timeoutMs: 300_000);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (monitor is not null)
                StopProcess(monitor);
        }
    }

    private static Process? StartPortalMonitor()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gdbus",
                Arguments = "monitor --session --dest org.freedesktop.portal.Desktop",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        process.Start();
        return process;
    }

    private static string? WaitForPortalResponse(Process monitor, int timeoutMs)
    {
        var output = new StringBuilder();
        var gate = new object();

        monitor.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
                return;
            lock (gate)
                output.AppendLine(args.Data);
        };
        monitor.BeginOutputReadLine();

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            string snapshot;
            lock (gate)
                snapshot = output.ToString();

            if (Regex.IsMatch(snapshot, @"Response\s*\(\s*uint32\s+1\b", RegexOptions.CultureInvariant))
                return null;

            if (snapshot.Contains("Response", StringComparison.Ordinal)
                && TryExtractFilePathFromPortalOutput(snapshot, out var path))
            {
                return path;
            }

            if (monitor.HasExited && sw.ElapsedMilliseconds > 2_000)
                break;

            Thread.Sleep(50);
        }

        return null;
    }

    private static bool TryExtractFilePathFromPortalOutput(string output, out string? path)
    {
        path = null;
        if (Regex.IsMatch(output, @"Response\s*\(\s*uint32\s+1\b", RegexOptions.CultureInvariant))
            return false;

        var match = UriRegex.Match(output);
        if (!match.Success)
            return false;

        var uri = match.Groups["uri"].Value;
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || parsed.Scheme != "file")
            return false;

        path = Uri.UnescapeDataString(parsed.LocalPath);
        return !string.IsNullOrWhiteSpace(path);
    }

    private static string? TryPickViaZenity(ExternalFilePickRequest request)
    {
        if (!CommandExists("zenity"))
            return null;

        var args = new StringBuilder();
        args.Append("--file-selection ");
        args.Append("--title=").Append(QuoteShell(request.Title));

        foreach (var extension in request.AllowedExtensions)
        {
            var pattern = NormalizeShellGlob(extension);
            if (pattern.Length == 0)
                continue;
            var label = string.IsNullOrWhiteSpace(request.FilterName) ? pattern : request.FilterName.Trim();
            args.Append(" --file-filter=").Append(QuoteShell(label + " | " + pattern));
        }

        var result = RunProcess("zenity", args.ToString(), timeoutMs: 300_000);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
            return null;

        return result.StdOut.Trim();
    }

    private static string? TryPickViaKdialog(ExternalFilePickRequest request)
    {
        if (!CommandExists("kdialog"))
            return null;

        var filter = BuildKdialogFilter(request);
        var args = $"--getopenfilename . {QuoteShell(filter)} --title {QuoteShell(request.Title)}";
        var result = RunProcess("kdialog", args, timeoutMs: 300_000);
        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
            return null;

        return result.StdOut.Trim();
    }

    private static string BuildKdialogFilter(ExternalFilePickRequest request)
    {
        if (request.AllowedExtensions.Count == 0)
            return "*";

        var patterns = request.AllowedExtensions
            .Select(NormalizeShellGlob)
            .Where(pattern => pattern.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return patterns.Length == 0 ? "*" : string.Join(' ', patterns);
    }

    private static string NormalizeShellGlob(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return string.Empty;

        var trimmed = extension.Trim();
        if (trimmed.StartsWith("*.", StringComparison.Ordinal))
            return trimmed;
        if (trimmed.StartsWith(".", StringComparison.Ordinal))
            return "*" + trimmed;
        return "*." + trimmed;
    }

    private static string EscapeGdbusString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string QuoteShell(string value) =>
        "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static void StopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // ignore
        }
        finally
        {
            process.Dispose();
        }
    }

    private static bool CommandExists(string command)
    {
        try
        {
            var result = RunProcess("sh", $"-c \"command -v {command}\"", timeoutMs: 3_000);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StdOut);
        }
        catch
        {
            return false;
        }
    }

    private static (int ExitCode, string StdOut, string StdErr) RunProcess(
        string fileName,
        string arguments,
        int timeoutMs)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeoutMs))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            return (-1, string.Empty, "timeout");
        }

        return (process.ExitCode, stdOutTask.GetAwaiter().GetResult(), stdErrTask.GetAwaiter().GetResult());
    }
}
