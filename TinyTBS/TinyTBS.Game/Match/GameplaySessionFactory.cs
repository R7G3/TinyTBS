using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Assets;

namespace TinyTBS.Game.Match;

/// <summary>
/// Loads match textures via <see cref="IAssetResolver"/> and builds a demo session.
/// </summary>
public static class GameplaySessionFactory
{
    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets)
    {
        var textures = MatchTextureAtlas.Load(graphicsDevice, content, assets);
        var state = MatchState.CreateDemo();
        var scene = new MatchScene(state, graphicsDevice, spriteBatch, textures);
        var cursorHighlight = new CursorHighlightRenderer(graphicsDevice);

        return new GameplaySession(state, scene, cursorHighlight, textures);
    }
}
