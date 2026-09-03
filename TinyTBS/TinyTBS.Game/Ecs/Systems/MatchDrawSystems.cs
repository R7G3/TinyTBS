using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;
using MonoGame.Extended.Graphics;
using TinyTBS.Core.Match;
using TinyTBS.Game.Ecs.Components;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ecs.Systems;

public sealed class GridDrawSystem : DrawSystem
{
    private static readonly Color LightTile = new(48, 56, 72);
    private static readonly Color DarkTile = new(36, 42, 56);
    private static readonly Color Border = new(90, 100, 120);

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private readonly Texture2D _pixel;

    public GridDrawSystem(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, MatchBoardLayout layout)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _layout = layout;

        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public override void Draw(GameTime gameTime)
    {
        _layout.UpdateForViewport(
            _graphicsDevice.Viewport.Width,
            _graphicsDevice.Viewport.Height);

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
        DrawBorder(bounds);

        _spriteBatch.End();
    }

    private void DrawBorder(Rectangle bounds)
    {
        const int thickness = 2;
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), Border);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), Border);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), Border);
        _spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), Border);
    }
}

public sealed class UnitDrawSystem : EntityDrawSystem
{
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private ComponentMapper<Transform2>? _transformMapper;
    private ComponentMapper<Sprite>? _spriteMapper;
    private ComponentMapper<GridPosition>? _gridMapper;

    public UnitDrawSystem(SpriteBatch spriteBatch, MatchBoardLayout layout)
        : base(Aspect.All(typeof(Transform2), typeof(Sprite), typeof(GridPosition)))
    {
        _spriteBatch = spriteBatch;
        _layout = layout;
    }

    public override void Initialize(IComponentMapperService mapperService)
    {
        _transformMapper = mapperService.GetMapper<Transform2>();
        _spriteMapper = mapperService.GetMapper<Sprite>();
        _gridMapper = mapperService.GetMapper<GridPosition>();
    }

    public override void Draw(GameTime gameTime)
    {
        if (_transformMapper is null || _spriteMapper is null || _gridMapper is null)
            return;

        _layout.UpdateForViewport(
            _spriteBatch.GraphicsDevice.Viewport.Width,
            _spriteBatch.GraphicsDevice.Viewport.Height);

        _spriteBatch.Begin();

        foreach (var entityId in ActiveEntities)
        {
            var grid = _gridMapper.Get(entityId);
            var transform = _transformMapper.Get(entityId);
            var sprite = _spriteMapper.Get(entityId);

            transform.Position = _layout.CellToWorldCenter(grid.Cell);
            _spriteBatch.Draw(sprite, transform);
        }

        _spriteBatch.End();
    }
}
