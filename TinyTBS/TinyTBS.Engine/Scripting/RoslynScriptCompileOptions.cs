using System.Reflection;

namespace TinyTBS.Engine.Scripting;

/// <summary>Parameters for wrapping user script methods into a generated class and compiling with Roslyn.</summary>
public sealed class RoslynScriptCompileOptions
{
    public required string UserSource { get; init; }

    public required string GeneratedNamespace { get; init; }

    public required string GeneratedTypeName { get; init; }

    /// <summary>Interface or base type name in the generated class declaration (e.g. <c>IMapScriptHooks</c>).</summary>
    public required string ImplementsTypeName { get; init; }

    public required IReadOnlyList<string> Usings { get; init; }

    public required IReadOnlyList<Assembly> ExtraAssemblies { get; init; }

    public required string AssemblyNamePrefix { get; init; }

    public string SourceFileName { get; init; } = "script.cs";

    public int UserSourceIndentSpaces { get; init; } = 8;
}
