using System.Reflection;
using TinyTBS.Engine.Scripting;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Compiles map / campaign <c>script.cs</c> into hook interfaces via <see cref="RoslynScriptCompiler"/>.
/// </summary>
public sealed class RoslynMapScriptEngine : IScriptEngine
{
    private static readonly Assembly GameAssembly = typeof(IMapScriptHooks).Assembly;

    private static readonly string[] ScriptUsings =
    [
        "TinyTBS.Game.Scripting",
        "TinyTBS.Game.Scripting.Models",
    ];

    private const string MapGeneratedNamespace = "TinyTBS.MapScripts.Generated";
    private const string MapGeneratedTypeName = "MapScriptInstance";
    private const string CampaignGeneratedNamespace = "TinyTBS.CampaignScripts.Generated";
    private const string CampaignGeneratedTypeName = "CampaignScriptInstance";

    public IMapScriptHooks LoadMapScript(string sourceCode, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (ScriptSourceValidator.IsEffectivelyEmpty(sourceCode))
            return new NoOpMapScriptHooks();

        return RoslynScriptCompiler.CreateInstance<IMapScriptHooks>(new RoslynScriptCompileOptions
        {
            UserSource = sourceCode,
            GeneratedNamespace = MapGeneratedNamespace,
            GeneratedTypeName = MapGeneratedTypeName,
            ImplementsTypeName = nameof(IMapScriptHooks),
            Usings = ScriptUsings,
            ExtraAssemblies = [GameAssembly],
            AssemblyNamePrefix = "TinyTBS.MapScript",
            SourceFileName = sourceFileName ?? "script.cs",
        });
    }

    public ICampaignScriptHooks LoadCampaignScript(string sourceCode, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (ScriptSourceValidator.IsEffectivelyEmpty(sourceCode))
            return new NoOpCampaignScriptHooks();

        return RoslynScriptCompiler.CreateInstance<ICampaignScriptHooks>(new RoslynScriptCompileOptions
        {
            UserSource = sourceCode,
            GeneratedNamespace = CampaignGeneratedNamespace,
            GeneratedTypeName = CampaignGeneratedTypeName,
            ImplementsTypeName = nameof(ICampaignScriptHooks),
            Usings = ScriptUsings,
            ExtraAssemblies = [GameAssembly],
            AssemblyNamePrefix = "TinyTBS.CampaignScript",
            SourceFileName = sourceFileName ?? "script.cs",
        });
    }
}
