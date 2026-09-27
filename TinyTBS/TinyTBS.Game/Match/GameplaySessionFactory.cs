using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Match;

/// <summary>
/// Convenience entry points for building a session from vanilla scenario / bundle defaults.
/// </summary>
public static class GameplaySessionFactory
{
    /// <summary>Compact smoke-test level under <c>vanilla_scenario</c>.</summary>
    public const string DemoLevelId = "demo";

    /// <summary>Full-roster QA level under <c>vanilla_scenario</c> (default New Game).</summary>
    public const string ProvingGroundsLevelId = "proving-grounds";

    public const string VanillaScenarioModuleId = MatchSessionLoadPipeline.DefaultScenarioModuleId;

    public const string VanillaBundleId = "vanilla";

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        IUserDataPaths userDataPaths) =>
        CreateFromBundleLevel(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            userDataPaths,
            VanillaBundleId,
            ProvingGroundsLevelId);

    public static GameplaySession CreateFromBundleLevel(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        IUserDataPaths userDataPaths,
        string bundleId,
        string levelId)
    {
        var composition = LoadCompositionFromBundle(bundleId, files, userDataPaths);
        return CreateFromScenarioLevel(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            userDataPaths,
            composition.ScenarioModuleId,
            levelId,
            composition);
    }

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

    /// <summary>
    /// Loads match composition from a <c>*.bundle.json</c> preset (defaults + scenario replaces).
    /// </summary>
    public static MatchContentComposition LoadCompositionFromBundle(
        string bundleId,
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        var bundleLocator = new ContentBundleLocator(files, userDataPaths);
        var bundle = bundleLocator.Load(bundleId);
        var moduleLocator = new ContentModuleLocator(files, userDataPaths);
        var scenario = ScenarioModuleLoader.Load(
            moduleLocator.ResolveModuleRoot(bundle.Defaults.ScenarioModuleId),
            files);
        return MatchContentComposition.FromBundleDefaults(bundle, scenario.Replaces);
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

    /// <summary>Loads match content catalog from a bundle preset (for tests / tooling).</summary>
    public static MatchContentCatalog LoadCatalogFromBundle(
        string bundleId,
        IFileContentProvider files,
        IUserDataPaths userDataPaths)
    {
        var composition = LoadCompositionFromBundle(bundleId, files, userDataPaths);
        var locator = new ContentModuleLocator(files, userDataPaths);
        return MatchContentCompositionLoader.Load(composition, locator, files).Catalog;
    }
}
