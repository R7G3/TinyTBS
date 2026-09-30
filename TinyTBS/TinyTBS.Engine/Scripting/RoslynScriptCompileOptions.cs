using System.Reflection;

namespace TinyTBS.Engine.Scripting;

/// <summary>Parameters for wrapping user script members into a generated class and compiling it in the sandbox.</summary>
public sealed class RoslynScriptCompileOptions
{
    public required string UserSource { get; init; }

    public required string GeneratedNamespace { get; init; }

    public required string GeneratedTypeName { get; init; }

    /// <summary>Interface or base type name in the generated class declaration (e.g. <c>IMapScriptHooks</c>).</summary>
    public required string ImplementsTypeName { get; init; }

    /// <summary>Namespaces imported into the generated file (normally the script API namespace).</summary>
    public required IReadOnlyList<string> Usings { get; init; }

    /// <summary>The only non-framework assemblies the script is compiled against (the script API).</summary>
    public required IReadOnlyList<Assembly> ApiAssemblies { get; init; }

    /// <summary>API namespaces whose types scripts may use, in addition to the framework allowlist.</summary>
    public required IReadOnlyList<string> ApiNamespaces { get; init; }

    /// <summary>Fully qualified API types that scripts must not reference (for example the budget guard).</summary>
    public IReadOnlyList<string> ForbiddenApiTypes { get; init; } = [];

    /// <summary>
    /// Fully qualified static class with <c>Tick()</c> and <c>EnterFrame()</c>; the compiler inserts calls to it
    /// into every loop, jump and function body.
    /// </summary>
    public required string BudgetTypeName { get; init; }

    public required string AssemblyNamePrefix { get; init; }

    public string SourceFileName { get; init; } = "script.cs";
}
