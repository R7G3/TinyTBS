using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.Graphics;
using TinyTBS.Core.Match;
using TinyTBS.Game.Ecs.Components;
using TinyTBS.Game.Ecs.Systems;
using TinyTBS.Game.Rendering;

namespace TinyTBS.Game.Match;

/// <summary>
/// Engine view of a match: ECS world, draw systems, layout sync from <see cref="MatchState"/>.
/// </summary>
public sealed class MatchScene : IDisposable
{
    private static readonly Color[] PlayerColors =
    [
        new(90, 160, 255),
        new(255, 120, 90),
    ];

    private readonly MatchState _state;
    private readonly MatchBoardLayout _layout = new();
    private readonly Dictionary<int, int> _unitEntityById = new();

    public MatchScene(
        MatchState state,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        Texture2D unitTexture)
    {
        _state = state;
        World = new WorldBuilder()
            .AddSystem(new GridDrawSystem(graphicsDevice, spriteBatch, _layout))
            .AddSystem(new UnitDrawSystem(spriteBatch))
            .Build();

        foreach (var unit in state.Units)
            CreateVisual(unit, unitTexture);
    }

    public World World { get; }

    public MatchBoardLayout Layout => _layout;

    public void PrepareFrame(int viewportWidth, int viewportHeight)
    {
        _layout.UpdateForViewport(viewportWidth, viewportHeight);
        SyncUnitTransformsFromState();
    }

    public void Update(GameTime gameTime) => World.Update(gameTime);

    public void Draw(GameTime gameTime) => World.Draw(gameTime);

    public void Dispose() => World.Dispose();

    private void CreateVisual(MatchUnit unit, Texture2D unitTexture)
    {
        var entity = World.CreateEntity();
        var region = new Texture2DRegion(unitTexture);
        var sprite = new Sprite(region)
        {
            Color = PlayerColors[unit.PlayerIndex % PlayerColors.Length],
            Origin = new Vector2(region.Width * 0.5f, region.Height * 0.5f),
        };

        entity.Attach(new GridPosition(unit.Cell.X, unit.Cell.Y));
        entity.Attach(new UnitOwner(unit.PlayerIndex));
        entity.Attach(new Transform2(_layout.CellToWorldCenter(unit.Cell)));
        entity.Attach(sprite);

        _unitEntityById[unit.Id] = entity.Id;
    }

    private void SyncUnitTransformsFromState()
    {
        foreach (var unit in _state.Units)
        {
            if (!_unitEntityById.TryGetValue(unit.Id, out var entityId))
                continue;

            var entity = World.GetEntity(entityId);
            var grid = entity.Get<GridPosition>();
            grid.X = unit.Cell.X;
            grid.Y = unit.Cell.Y;
            entity.Get<Transform2>().Position = _layout.CellToWorldCenter(unit.Cell);
        }
    }
}
