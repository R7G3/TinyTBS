namespace TinyTBS.Game.Scripting;

/// <summary>Loads and compiles map board and campaign meta scripts.</summary>
public interface IScriptEngine
{
    /// <summary>
    /// Compiles <paramref name="sourceCode"/> into map hook implementations.
    /// Empty / comment-only source yields a no-op script.
    /// </summary>
    IMapScriptHooks LoadMapScript(string sourceCode, string? sourceFileName = null);

    /// <summary>
    /// Compiles <paramref name="sourceCode"/> into campaign hook implementations.
    /// Empty / comment-only source yields a no-op script.
    /// </summary>
    ICampaignScriptHooks LoadCampaignScript(string sourceCode, string? sourceFileName = null);
}
