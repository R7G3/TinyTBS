using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Compiles a wrapped user script into an in-memory assembly and creates a hook instance.
/// Callers supply usings, generated type names, and extra assemblies (typically the game assembly).
/// </summary>
public static class RoslynScriptCompiler
{
    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    public static THooks CreateInstance<THooks>(RoslynScriptCompileOptions options)
        where THooks : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.UserSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GeneratedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GeneratedTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ImplementsTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AssemblyNamePrefix);
        ArgumentNullException.ThrowIfNull(options.Usings);
        ArgumentNullException.ThrowIfNull(options.ExtraAssemblies);

        ScriptSourceValidator.Validate(options.UserSource);

        var compilationUnit = WrapUserSource(options);
        var assemblyBytes = CompileToAssembly(
            compilationUnit,
            options.SourceFileName,
            options.AssemblyNamePrefix,
            options.ExtraAssemblies);

        var assembly = Assembly.Load(assemblyBytes);
        var fullTypeName = $"{options.GeneratedNamespace}.{options.GeneratedTypeName}";
        var scriptType = assembly.GetType(fullTypeName)
            ?? throw new ScriptHostException($"Compiled script is missing type '{fullTypeName}'.");

        if (Activator.CreateInstance(scriptType) is not THooks hooks)
        {
            throw new ScriptHostException(
                $"Compiled script type '{scriptType.FullName}' does not implement '{typeof(THooks).FullName}'.");
        }

        return hooks;
    }

    private static string WrapUserSource(RoslynScriptCompileOptions options)
    {
        var indented = Indent(options.UserSource, options.UserSourceIndentSpaces);
        var extraUsings = string.Join(
            Environment.NewLine,
            options.Usings
                .Where(import => !string.IsNullOrWhiteSpace(import))
                .Select(import => $"using {import.Trim()};"));

        if (extraUsings.Length > 0)
            extraUsings += Environment.NewLine;

        return $$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            {{extraUsings}}namespace {{options.GeneratedNamespace}}
            {
                public sealed class {{options.GeneratedTypeName}} : {{options.ImplementsTypeName}}
                {
            {{indented}}
                }
            }
            """;
    }

    private static string Indent(string text, int indentSpaces)
    {
        var indent = new string(' ', Math.Max(0, indentSpaces));
        var lines = text.Split(LineSeparators, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(line => indent + line));
    }

    private static byte[] CompileToAssembly(
        string compilationUnit,
        string sourceFileName,
        string assemblyNamePrefix,
        IReadOnlyList<Assembly> extraAssemblies)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            compilationUnit,
            path: sourceFileName,
            encoding: Encoding.UTF8);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"{assemblyNamePrefix}_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: CreateMetadataReferences(extraAssemblies),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var errors = string.Join(
                Environment.NewLine,
                emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                    .Select(diagnostic => diagnostic.ToString()));
            throw new ScriptHostException($"Failed to compile script:{Environment.NewLine}{errors}");
        }

        return stream.ToArray();
    }

    private static ImmutableArray<MetadataReference> CreateMetadataReferences(
        IReadOnlyList<Assembly> extraAssemblies)
    {
        var references = new List<MetadataReference>();
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var fileName = Path.GetFileName(path);
                if (!IsAllowedPlatformAssembly(fileName))
                    continue;

                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in extraAssemblies)
        {
            if (assembly is null)
                continue;

            var path = assembly.Location;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ScriptHostException(
                    $"Cannot resolve assembly path for script references: '{assembly.FullName}'.");
            }

            if (!seen.Add(path))
                continue;

            references.Add(MetadataReference.CreateFromFile(path));
        }

        return references.ToImmutableArray();
    }

    private static bool IsAllowedPlatformAssembly(string fileName)
    {
        return fileName.Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Collections.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Linq.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Linq.Expressions.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Console.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.ObjectModel.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Threading.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Threading.Thread.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Text.RegularExpressions.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Runtime.Extensions.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("System.Collections.Concurrent.dll", StringComparison.OrdinalIgnoreCase);
    }
}
