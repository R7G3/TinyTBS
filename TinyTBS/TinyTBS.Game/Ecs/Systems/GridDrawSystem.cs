using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ECS.Systems;
using TinyTBS.Game.Match;
using TinyTBS.Game.Rendering;

namespace TinyTBS.Game.Ecs.Systems;

/// <summary>
/// Draws the checkerboard grid. Expects <see cref="MatchBoardLayout"/> already prepared for the viewport.
/// </summary>
public sealed class GridDrawSystem : DrawSystem
{
    private static readonly Color LightTile = new(48, 56, 72);
    private static readonly Color DarkTile = new(36, 42, 56);
    private static readonly Color Border = new(90, 100, 120);

    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private readonly Texture2D _pixel;

    public GridDrawSystem(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, MatchBoardLayout layout)
    {
        _spriteBatch = spriteBatch;
        _layout = layout;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        for (var y = 0; y < _layout.Height; y++)
        {
            for (var x = 0; x < _layout.Width; x++)
            {
                var color = (x + y) % 2 == 0 ? LightTile : DarkTile;
                var position = _layout.Origin + new Vector2(x * _layout.TileSize, y * _layout.TileSize);
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)position.X, (int)position.Y, _layout.TileSize, _layout.TileSize),
                    color);
            }
        }

        var bounds = new Rectangle(
            (int)_layout.Origin.X,
            (int)_layout.Origin.Y,
            _layout.Width * _layout.TileSize,
            _layout.Height * _layout.TileSize);
        SpriteBatchPrimitives.DrawRectBorder(_spriteBatch, _pixel, bounds, Border, thickness: 2);

        _spriteBatch.End();
    }
}
