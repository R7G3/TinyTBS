using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Maps;

namespace TinyTBS.Game.Match;

/// <summary>
/// Loads match textures via <see cref="IAssetResolver"/> and builds a session from a map fixture.
/// </summary>
public static class GameplaySessionFactory
{
    public const string DemoMapId = "demo";

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets,
        IFileContentProvider files)
    {
        var fixturesRoot = files.Combine(AppContext.BaseDirectory, "Fixtures");
        var map = MapFolderLoader.LoadFromModuleMaps(fixturesRoot, DemoMapId, files);
        var state = MatchState.FromMap(map);
        var textures = MatchTextureAtlas.Load(graphicsDevice, content, assets);
        var scene = new MatchScene(state, graphicsDevice, spriteBatch, textures);
        var cursorHighlight = new CursorHighlightRenderer(graphicsDevice);

        return new GameplaySession(state, scene, cursorHighlight, textures);
    }
}
