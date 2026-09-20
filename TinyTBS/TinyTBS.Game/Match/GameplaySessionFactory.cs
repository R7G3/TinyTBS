using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Levels;

namespace TinyTBS.Game.Match;

/// <summary>
/// Loads match textures via <see cref="IAssetResolver"/> and builds a session from a level fixture.
/// </summary>
public static class GameplaySessionFactory
{
    public const string DemoLevelId = "demo";

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files)
    {
        var fixturesRoot = files.Combine(AppContext.BaseDirectory, "Fixtures");
        var level = LevelFolderLoader.LoadFromModuleLevels(fixturesRoot, DemoLevelId, files);
        var state = MatchState.FromMap(level.Map);
        var textures = MatchTextureAtlas.Load(graphicsDevice, content, assets);
        var scene = new MatchScene(state, graphicsDevice, spriteBatch, textures);
        var cursorHighlight = new CursorHighlightRenderer(graphicsDevice);

        return new GameplaySession(state, scene, cursorHighlight, textures);
    }
}
