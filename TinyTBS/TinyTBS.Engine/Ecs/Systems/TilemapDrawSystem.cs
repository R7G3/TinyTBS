using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.ECS.Systems;
using TinyTBS.Engine.Rendering;

namespace TinyTBS.Engine.Ecs.Systems;

/// <summary>
/// Draws one texture per cell from a row-major tile array. Expects an open SpriteBatch
/// and layout prepared for the viewport.
/// </summary>
public sealed class TilemapDrawSystem : DrawSystem
{
    private static readonly Color Border = new(40, 48, 64);

    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private readonly Texture2D[] _tiles;
    private readonly Texture2D _pixel;

    public TilemapDrawSystem(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        Texture2D[] tilesRowMajor)
    {
        if (tilesRowMajor.Length != layout.Width * layout.Height)
            throw new ArgumentException("Tile array length must equal layout width * height.", nameof(tilesRowMajor));

        _spriteBatch = spriteBatch;
        _layout = layout;
        _tiles = tilesRowMajor;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public override void Draw(GameTime gameTime)
    {
        for (var y = 0; y < _layout.Height; y++)
        {
            for (var x = 0; x < _layout.Width; x++)
            {
                var texture = _tiles[y * _layout.Width + x];
                var position = _layout.Origin + new Vector2(x * _layout.TileSize, y * _layout.TileSize);
                _spriteBatch.Draw(
                    texture,
                    new Rectangle((int)position.X, (int)position.Y, _layout.TileSize, _layout.TileSize),
                    Color.White);
            }
        }

        var bounds = new Rectangle(
            (int)_layout.Origin.X,
            (int)_layout.Origin.Y,
            _layout.Width * _layout.TileSize,
            _layout.Height * _layout.TileSize);
        SpriteBatchPrimitives.DrawRectBorder(_spriteBatch, _pixel, bounds, Border, thickness: 2);
    }
}
