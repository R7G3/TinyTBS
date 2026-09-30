using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TinyTBS.Engine.Scripting;

/// <summary>
/// Compiles a wrapped user script in the sandbox and loads it into a collectible load context:
/// compile (errors reported in script line numbers) → <see cref="ScriptSandboxValidator"/> →
/// <see cref="ScriptBudgetRewriter"/> → emit. Emitted assemblies are cached by source hash.
/// </summary>
public static class RoslynScriptCompiler
{
    private const int CompiledCacheCapacity = 32;

    /// <summary>Framework assemblies scripts compile against; the sandbox policy narrows usable types further.</summary>
    private static readonly HashSet<string> PlatformAssemblyFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System.Private.CoreLib.dll",
        "System.Runtime.dll",
        "System.Runtime.Extensions.dll",
        "System.Collections.dll",
        "netstandard.dll",
        "mscorlib.dll",
    };

    private static readonly ConcurrentDictionary<string, byte[]> CompiledAssemblies = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, MetadataReference> AssemblyReferences =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences =
        new(CreatePlatformReferences, LazyThreadSafetyMode.ExecutionAndPublication);

    public static CompiledScript Compile(RoslynScriptCompileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.UserSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GeneratedNamespace);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.GeneratedTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ImplementsTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.AssemblyNamePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BudgetTypeName);
        ArgumentNullException.ThrowIfNull(options.Usings);
        ArgumentNullException.ThrowIfNull(options.ApiAssemblies);
        ArgumentNullException.ThrowIfNull(options.ApiNamespaces);

        var compilationUnit = WrapUserSource(options);
        var cacheKey = CreateCacheKey(options, compilationUnit);
        if (!CompiledAssemblies.TryGetValue(cacheKey, out var assemblyBytes))
        {
            assemblyBytes = CompileToAssembly(compilationUnit, options);
            if (CompiledAssemblies.Count >= CompiledCacheCapacity)
                CompiledAssemblies.Clear();
            CompiledAssemblies[cacheKey] = assemblyBytes;
        }

        return Load(assemblyBytes, options);
    }

    private static string WrapUserSource(RoslynScriptCompileOptions options)
    {
        var extraUsings = string.Join(
            Environment.NewLine,
            options.Usings
                .Where(import => !string.IsNullOrWhiteSpace(import))
                .Select(import => $"using {import.Trim()};"));

        if (extraUsings.Length > 0)
            extraUsings += Environment.NewLine;

        var lineFileName = options.SourceFileName.Replace('"', '\'');

        // #line maps compiler errors and sandbox violations back to the author's own script lines.
        return $$"""
            #nullable enable
            using System;
            using System.Collections.Generic;
            {{extraUsings}}namespace {{options.GeneratedNamespace}}
            {
            public sealed class {{options.GeneratedTypeName}} : {{options.ImplementsTypeName}}
            {
            #line 1 "{{lineFileName}}"
            {{options.UserSource}}
            #line default
            }
            }
            """;
    }

    private static byte[] CompileToAssembly(string compilationUnit, RoslynScriptCompileOptions options)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            compilationUnit,
            path: options.SourceFileName,
            encoding: Encoding.UTF8);

        var compilation = CSharpCompilation.Create(
            assemblyName: $"{options.AssemblyNamePrefix}_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: CreateReferences(options.ApiAssemblies),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false));

        ThrowOnErrors(compilation.GetDiagnostics(), "Failed to compile script:");

        var policy = new ScriptSandboxPolicy(
            options.ApiNamespaces,
            options.ForbiddenApiTypes,
            options.GeneratedNamespace,
            compilation.Assembly);
        var violations = ScriptSandboxValidator.Validate(compilation, syntaxTree, policy);
        if (violations.Count > 0)
        {
            throw new ScriptHostException(
                "Script uses features that are not available to scripts:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
        }

        var guardedTree = ScriptBudgetRewriter.Rewrite(
            syntaxTree,
            compilation.GetSemanticModel(syntaxTree),
            options.BudgetTypeName);
        var guardedCompilation = compilation.ReplaceSyntaxTree(syntaxTree, guardedTree);

        using var stream = new MemoryStream();
        var emitResult = guardedCompilation.Emit(stream);
        ThrowOnErrors(emitResult.Diagnostics, "Failed to prepare script for the sandbox:");
        return stream.ToArray();
    }

    private static CompiledScript Load(byte[] assemblyBytes, RoslynScriptCompileOptions options)
    {
        var loadContext = new AssemblyLoadContext(
            $"{options.AssemblyNamePrefix}.{Guid.NewGuid():N}",
            isCollectible: true);
        try
        {
            using var stream = new MemoryStream(assemblyBytes, writable: false);
            var assembly = loadContext.LoadFromStream(stream);
            var fullTypeName = $"{options.GeneratedNamespace}.{options.GeneratedTypeName}";
            var scriptType = assembly.GetType(fullTypeName)
                ?? throw new ScriptHostException($"Compiled script is missing type '{fullTypeName}'.");
            return new CompiledScript(loadContext, scriptType);
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    private static void ThrowOnErrors(IEnumerable<Diagnostic> diagnostics, string header)
    {
        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();
        if (errors.Length > 0)
            throw new ScriptHostException(header + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    private static string CreateCacheKey(RoslynScriptCompileOptions options, string compilationUnit)
    {
        var identity = string.Join(
            "\n",
            compilationUnit,
            options.AssemblyNamePrefix,
            options.BudgetTypeName,
            string.Join(",", options.ApiNamespaces),
            string.Join(",", options.ForbiddenApiTypes),
            string.Join(",", options.ApiAssemblies.Select(assembly => assembly.ManifestModule.ModuleVersionId)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private static ImmutableArray<MetadataReference> CreateReferences(IReadOnlyList<Assembly> apiAssemblies)
    {
        var references = PlatformReferences.Value.ToBuilder();
        foreach (var assembly in apiAssemblies)
        {
            var path = assembly.Location;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ScriptHostException(
                    $"Cannot resolve assembly path for script references: '{assembly.FullName}'.");
            }

            references.Add(AssemblyReferences.GetOrAdd(path, static filePath => MetadataReference.CreateFromFile(filePath)));
        }

        return references.ToImmutable();
    }

    private static ImmutableArray<MetadataReference> CreatePlatformReferences()
    {
        var references = ImmutableArray.CreateBuilder<MetadataReference>();
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrWhiteSpace(trustedAssemblies))
            return references.ToImmutable();

        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (PlatformAssemblyFileNames.Contains(Path.GetFileName(path)))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        return references.ToImmutable();
    }
}
