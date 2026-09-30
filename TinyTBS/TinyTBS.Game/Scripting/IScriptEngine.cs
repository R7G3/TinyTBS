using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>Loads and compiles map board and campaign meta scripts.</summary>
public interface IScriptEngine
{
    /// <summary>
    /// Compiles <paramref name="sourceCode"/> into map hook implementations.
    /// Empty / comment-only source yields a no-op script.
    /// </summary>
    LoadedScript<IMapScriptHooks> LoadMapScript(string sourceCode, string? sourceFileName = null);

    /// <summary>
    /// Compiles <paramref name="sourceCode"/> into campaign hook implementations.
    /// Empty / comment-only source yields a no-op script.
    /// </summary>
    LoadedScript<ICampaignScriptHooks> LoadCampaignScript(string sourceCode, string? sourceFileName = null);
}
