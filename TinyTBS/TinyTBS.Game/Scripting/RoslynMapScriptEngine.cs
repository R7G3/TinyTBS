using System.Reflection;
using TinyTBS.Engine.Scripting;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>
/// Compiles map / campaign <c>script.cs</c> against <c>TinyTBS.Scripting.Api</c> only, through the
/// Roslyn sandbox (<see cref="RoslynScriptCompiler"/>).
/// </summary>
public sealed class RoslynMapScriptEngine : IScriptEngine
{
    private static readonly Assembly ApiAssembly = typeof(IMapScriptHooks).Assembly;
    private static readonly string ApiNamespace = typeof(IMapScriptHooks).Namespace!;

    private const string MapGeneratedNamespace = "TinyTBS.MapScripts.Generated";
    private const string MapGeneratedTypeName = "MapScriptInstance";
    private const string CampaignGeneratedNamespace = "TinyTBS.CampaignScripts.Generated";
    private const string CampaignGeneratedTypeName = "CampaignScriptInstance";

    public LoadedScript<IMapScriptHooks> LoadMapScript(string sourceCode, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (ScriptSourceText.IsEffectivelyEmpty(sourceCode))
            return new LoadedScript<IMapScriptHooks>(new NoOpMapScriptHooks());

        return Load<IMapScriptHooks>(
            sourceCode,
            sourceFileName,
            MapGeneratedNamespace,
            MapGeneratedTypeName,
            "TinyTBS.MapScript");
    }

    public LoadedScript<ICampaignScriptHooks> LoadCampaignScript(string sourceCode, string? sourceFileName = null)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        if (ScriptSourceText.IsEffectivelyEmpty(sourceCode))
            return new LoadedScript<ICampaignScriptHooks>(new NoOpCampaignScriptHooks());

        return Load<ICampaignScriptHooks>(
            sourceCode,
            sourceFileName,
            CampaignGeneratedNamespace,
            CampaignGeneratedTypeName,
            "TinyTBS.CampaignScript");
    }

    private static LoadedScript<THooks> Load<THooks>(
        string sourceCode,
        string? sourceFileName,
        string generatedNamespace,
        string generatedTypeName,
        string assemblyNamePrefix)
        where THooks : class
    {
        var compiled = RoslynScriptCompiler.Compile(new RoslynScriptCompileOptions
        {
            UserSource = sourceCode,
            GeneratedNamespace = generatedNamespace,
            GeneratedTypeName = generatedTypeName,
            ImplementsTypeName = typeof(THooks).Name,
            Usings = [ApiNamespace],
            ApiAssemblies = [ApiAssembly],
            ApiNamespaces = [ApiNamespace],
            ForbiddenApiTypes = [typeof(ScriptBudget).FullName!, typeof(ScriptBudgetFrame).FullName!],
            BudgetTypeName = typeof(ScriptBudget).FullName!,
            AssemblyNamePrefix = assemblyNamePrefix,
            SourceFileName = sourceFileName ?? "script.cs",
        });

        try
        {
            // Field initializers and constructors are script code too: run them under the budget.
            var instance = ScriptExecution.Run(
                "constructor",
                ScriptExecution.DefaultTimeout,
                () => Activator.CreateInstance(compiled.ScriptType));
            if (instance is not THooks hooks)
                throw new ScriptHostException($"Compiled script does not implement '{typeof(THooks).Name}'.");

            return new LoadedScript<THooks>(hooks, compiled);
        }
        catch
        {
            compiled.Dispose();
            throw;
        }
    }
}
