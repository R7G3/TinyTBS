using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Buildings;
using TinyTBS.Game.Levels;
using TinyTBS.Game.Scripting;
using TinyTBS.Game.Units;

namespace TinyTBS.Game.Match;

/// <summary>
/// Loads match textures via <see cref="IAssetResolver"/> and builds a session from vanilla modules.
/// </summary>
public static class GameplaySessionFactory
{
    /// <summary>Compact smoke-test level under <c>vanilla_scenario</c>.</summary>
    public const string DemoLevelId = "demo";

    /// <summary>Full-roster QA level under <c>vanilla_scenario</c> (default Start match).</summary>
    public const string ProvingGroundsLevelId = "proving-grounds";

    public const string VanillaScenarioRelativePath = "Vanilla/Modules/vanilla_scenario";

    public const string VanillaUnitsRelativePath = "Vanilla/Modules/vanilla_units";

    public const string VanillaBuildingsRelativePath = "Vanilla/Modules/vanilla_buildings";

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
        var contentCatalog = LoadVanillaContentCatalog(files);

        var scenarioRoot = files.Combine(AppContext.BaseDirectory, VanillaScenarioRelativePath);
        var level = LevelFolderLoader.LoadFromModuleLevels(scenarioRoot, levelId, files);
        var state = MatchState.FromMap(
            level.Map,
            contentCatalog,
            playerCount: level.Players.DefaultSlots,
            startingGold: level.DefaultStartingGold);

        var scriptEngine = new RoslynMapScriptEngine();
        var scriptHost = MapScriptHost.LoadForMap(
            state,
            level.Map.ScriptPath,
            files,
            scriptEngine);
        scriptHost.NotifyMatchStarted(state);

        var textures = MatchTextureAtlas.Load(graphicsDevice, content, assets, contentCatalog);
        var scene = new MatchScene(state, graphicsDevice, spriteBatch, textures);
        var cursorHighlight = new CursorHighlightRenderer(graphicsDevice);
        var minimap = new MinimapRenderer(graphicsDevice);
        var levelBrief = new MatchLevelBrief
        {
            LevelId = level.Id,
            Title = level.Title,
            Description = level.Description,
            VictoryType = level.Victory.Type,
            DefeatType = level.Defeat.Type,
            TeamDefeatMode = level.TeamDefeatMode,
        };

        return new GameplaySession(
            state,
            scene,
            cursorHighlight,
            textures,
            scriptHost,
            levelBrief,
            minimap,
            contentCatalog);
    }

    public static MatchContentCatalog LoadVanillaContentCatalog(IFileContentProvider files)
    {
        var unitsRoot = files.Combine(AppContext.BaseDirectory, VanillaUnitsRelativePath);
        var buildingsRoot = files.Combine(AppContext.BaseDirectory, VanillaBuildingsRelativePath);
        var unitsModule = UnitModuleLoader.Load(unitsRoot, files);
        var buildingsModule = BuildingModuleLoader.Load(buildingsRoot, files);
        return new MatchContentCatalog(unitsModule, buildingsModule);
    }
}
