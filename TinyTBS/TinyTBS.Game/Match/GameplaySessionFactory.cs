using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Buildings;
using TinyTBS.Game.Themes;
using TinyTBS.Game.Units;

namespace TinyTBS.Game.Match;

/// <summary>
/// Loads match textures via <see cref="IAssetResolver"/> and builds a session from vanilla modules.
/// </summary>
public static class GameplaySessionFactory
{
    /// <summary>Compact smoke-test level under <c>vanilla_scenario</c>.</summary>
    public const string DemoLevelId = "demo";

    /// <summary>Full-roster QA level under <c>vanilla_scenario</c> (default New Game / Start match).</summary>
    public const string ProvingGroundsLevelId = "proving-grounds";

    public const string VanillaScenarioRelativePath = "Vanilla/Modules/vanilla_scenario";

    public const string VanillaUnitsRelativePath = "Vanilla/Modules/vanilla_units";

    public const string VanillaBuildingsRelativePath = "Vanilla/Modules/vanilla_buildings";

    public const string VanillaThemeRelativePath = "Vanilla/Modules/vanilla_theme";

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files) =>
        CreateFromVanillaLevel(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            ProvingGroundsLevelId);

    public static GameplaySession CreateFromVanillaLevel(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files,
        string levelId)
    {
        var pipeline = new MatchSessionLoadPipeline(
            graphicsDevice,
            content,
            spriteBatch,
            assets,
            files,
            levelId);
        return pipeline.RunToCompletion();
    }

    public static MatchContentCatalog LoadVanillaContentCatalog(IFileContentProvider files)
    {
        var unitsRoot = files.Combine(AppContext.BaseDirectory, VanillaUnitsRelativePath);
        var buildingsRoot = files.Combine(AppContext.BaseDirectory, VanillaBuildingsRelativePath);
        var themeRoot = files.Combine(AppContext.BaseDirectory, VanillaThemeRelativePath);
        var unitsModule = UnitModuleLoader.Load(unitsRoot, files);
        var buildingsModule = BuildingModuleLoader.Load(buildingsRoot, files);
        var themeModule = ThemeModuleLoader.Load(themeRoot, files);
        return new MatchContentCatalog(unitsModule, buildingsModule, themeModule);
    }
}
