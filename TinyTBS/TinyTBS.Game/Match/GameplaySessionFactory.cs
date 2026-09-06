using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Core.Assets;
using TinyTBS.Core.Match;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Rendering;

namespace TinyTBS.Game.Match;

/// <summary>
/// Engine: loads match textures via <see cref="IAssetResolver"/> and builds a demo session.
/// </summary>
public static class GameplaySessionFactory
{
    private const string UnitLogicalPath = "Images/placeholder.png";
    private const string UnitContentName = "Images/placeholder";

    public static GameplaySession CreateDemo(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        SpriteBatch spriteBatch,
        IAssetResolver assets)
    {
        var unitTexture = GameTextureLoader.LoadOrFallback(
            graphicsDevice,
            content,
            assets,
            logicalRelativePath: UnitLogicalPath,
            contentAssetName: UnitContentName);

        var state = MatchState.CreateDemo();
        var scene = new MatchScene(state, graphicsDevice, spriteBatch, unitTexture.Texture);
        var cursorHighlight = new CursorHighlightRenderer(graphicsDevice);

        return new GameplaySession(state, scene, cursorHighlight, unitTexture);
    }
}
