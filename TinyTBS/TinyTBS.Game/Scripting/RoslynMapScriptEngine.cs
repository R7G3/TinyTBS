using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Compiles map <c>script.cs</c> with Roslyn into <see cref="IMapScriptHooks"/>.
/// User methods are inserted into a generated class (sandbox references + source validation).
/// </summary>
public sealed class RoslynMapScriptEngine : IScriptEngine
{
    private const string GeneratedNamespace = "TinyTBS.MapScripts.Generated";
    private const string GeneratedTypeName = "MapScriptInstance";

    public IMapScriptHooks LoadMapScript(string sourceCode, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        MapScriptSourceValidator.Validate(sourceCode);

        if (MapScriptSourceValidator.IsEffectivelyEmpty(sourceCode))
            return new NoOpMapScriptHooks();

        var compilationUnit = WrapUserSource(sourceCode);
        var assemblyBytes = CompileToAssembly(compilationUnit, sourceFileName ?? "script.cs");
        var assembly = Assembly.Load(assemblyBytes);
        var scriptType = assembly.GetType($"{GeneratedNamespace}.{GeneratedTypeName}")
            ?? throw new MapScriptException(
                $"Compiled map script is missing type '{GeneratedNamespace}.{GeneratedTypeName}'.");

        if (Activator.CreateInstance(scriptType) is not IMapScriptHooks hooks)
        {
            throw new MapScriptException(
                $"Compiled map script type '{scriptType.FullName}' does not implement IMapScriptHooks.");
        }

        return hooks;
    }

    private static string WrapUserSource(string userSource)
    {
        var indented = Indent(userSource, indentSpaces: 8);
        return $$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using TinyTBS.Game.Scripting;
            using TinyTBS.Game.Scripting.Models;

            namespace {{GeneratedNamespace}}
            {
                public sealed class {{GeneratedTypeName}} : IMapScriptHooks
                {
            {{indented}}
                }
            }
            """;
    }

    private static readonly string[] LineSeparators = ["\r\n", "\n", "\r"];

    private static string Indent(string text, int indentSpaces)
    {
        var indent = new string(' ', indentSpaces);
        var lines = text.Split(LineSeparators, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(line => indent + line));
    }

    private static byte[] CompileToAssembly(string compilationUnit, string sourceFileName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            compilationUnit,
            path: sourceFileName,
            encoding: System.Text.Encoding.UTF8);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"TinyTBS.MapScript_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: CreateMetadataReferences(),
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
            throw new MapScriptException($"Failed to compile map script:{Environment.NewLine}{errors}");
        }

        return stream.ToArray();
    }

    private static ImmutableArray<MetadataReference> CreateMetadataReferences()
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

        var gameAssemblyPath = typeof(IMapScriptHooks).Assembly.Location;
        if (string.IsNullOrWhiteSpace(gameAssemblyPath))
            throw new MapScriptException("Cannot resolve TinyTBS.Game assembly path for script references.");

        references.Add(MetadataReference.CreateFromFile(gameAssemblyPath));
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
