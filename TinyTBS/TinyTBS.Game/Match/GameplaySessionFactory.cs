using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Convenience entry points for building a session from vanilla scenario defaults.
/// </summary>
public static class GameplaySessionFactory
{
    /// <summary>Compact smoke-test level under <c>vanilla_scenario</c>.</summary>
    public const string DemoLevelId = "demo";

    /// <summary>Full-roster QA level under <c>vanilla_scenario</c> (default New Game).</summary>
    public const string ProvingGroundsLevelId = "proving-grounds";

    public const string VanillaScenarioModuleId = MatchSessionLoadPipeline.DefaultScenarioModuleId;

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        IUserDataPaths userDataPaths) =>
        CreateFromScenarioLevel(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            userDataPaths,
            VanillaScenarioModuleId,
            ProvingGroundsLevelId);

    public static GameplaySession CreateFromScenarioLevel(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        IUserDataPaths userDataPaths,
        string scenarioModuleId,
        string levelId,
        MatchContentComposition? composition = null)
    {
        var pipeline = new MatchSessionLoadPipeline(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            userDataPaths,
            levelId,
            scenarioModuleId,
            composition);
        return pipeline.RunToCompletion();
    }

    /// <summary>Loads match content catalog from scenario defaults (for tests / tooling).</summary>
    public static MatchContentCatalog LoadCatalogFromScenarioDefaults(
        string scenarioModuleId,
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        var locator = new ContentModuleLocator(files, userDataPaths);
        var scenario = ScenarioModuleLoader.Load(locator.ResolveModuleRoot(scenarioModuleId), files);
        var composition = MatchContentComposition.FromScenarioDefaults(scenario);
        return MatchContentCompositionLoader.Load(composition, locator, files).Catalog;
    }
}
