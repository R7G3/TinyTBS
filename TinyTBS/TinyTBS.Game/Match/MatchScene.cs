using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using TinyTBS.Engine.Ecs.Components;
using TinyTBS.Engine.Ecs.Systems;
using TinyTBS.Engine.Rendering;

namespace TinyTBS.Game.Match;

/// <summary>
/// Visual side of a match: tilemap, buildings, units synced from <see cref="MatchState"/>.
/// </summary>
public sealed class MatchScene : IDisposable
{
    private readonly MatchState _state;
    private readonly MatchBoardLayout _layout;
    private readonly MatchTextureAtlas _textures;
    private readonly SpriteBatch _spriteBatch;
    private readonly List<int> _buildingEntityIds = [];
    private readonly Dictionary<int, int> _unitEntityById = new();

    public MatchScene(
        MatchState state,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        MatchTextureAtlas textures)
    {
        _state = state;
        _textures = textures;
        _spriteBatch = spriteBatch;
        _layout = new MatchBoardLayout(state.Width, state.Height);

        var tiles = new Texture2D[state.Width * state.Height];
        for (var y = 0; y < state.Height; y++)
        {
            for (var x = 0; x < state.Width; x++)
                tiles[y * state.Width + x] = textures.Terrain(state.GetTerrain(x, y));
        }

        World = new WorldBuilder()
            .AddSystem(new TilemapDrawSystem(graphicsDevice, spriteBatch, _layout, tiles))
            .AddSystem(new TeamMaskedSpriteDrawSystem(spriteBatch, _layout))
            .Build();

        foreach (var building in state.Buildings)
        {
            var entityId = CreateMaskedVisual(
                building.Cell,
                textures.Building(building.Kind, building.IsRuined),
                PlayerPalette.ForOwner(building.OwnerPlayerIndex));
            World.GetEntity(entityId).Attach(new GridPosition(building.Cell.X, building.Cell.Y));
            _buildingEntityIds.Add(entityId);
        }

        foreach (var unit in state.Units)
            CreateUnitVisual(unit, textures);
    }

    public World World { get; }

    public MatchBoardLayout Layout => _layout;

    public void PrepareFrame(int viewportWidth, int viewportHeight)
    {
        _layout.UpdateForViewport(viewportWidth, viewportHeight);
        SyncBuildingTransforms();
        SyncUnitTransformsFromState();
    }

    public void Update(GameTime gameTime) => World.Update(gameTime);

    /// <summary>
    /// One SpriteBatch pass for tiles, team sprites, and optional overlays (e.g. cursor).
    /// </summary>
    public void Draw(GameTime gameTime, Action<SpriteBatch, MatchBoardLayout>? afterEntities = null)
    {
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);

        World.Draw(gameTime);
        afterEntities?.Invoke(_spriteBatch, _layout);

        _spriteBatch.End();
    }

    public void Dispose() => World.Dispose();

    private void CreateUnitVisual(MatchUnit unit, MatchTextureAtlas textures)
    {
        var entityId = CreateMaskedVisual(
            unit.Cell,
            textures.Unit(unit.Kind),
            PlayerPalette.ForPlayer(unit.PlayerIndex));

        World.GetEntity(entityId).Attach(new UnitOwner(unit.PlayerIndex));
        World.GetEntity(entityId).Attach(new GridPosition(unit.Cell.X, unit.Cell.Y));
        _unitEntityById[unit.Id] = entityId;
    }

    private int CreateMaskedVisual(GridCell cell, TeamSprite sprite, Color teamColor)
    {
        var entity = World.CreateEntity();
        // Same footprint as terrain tiles: sprite origin = top-left of the cell.
        var origin = Vector2.Zero;

        entity.Attach(new Transform2(CellTopLeft(cell.X, cell.Y)));
        entity.Attach(new TeamMaskedSprite(sprite.Base, sprite.Mask, teamColor, origin));
        return entity.Id;
    }

    private Vector2 CellTopLeft(int cellX, int cellY) =>
        _layout.Origin + new Vector2(cellX * _layout.TileSize, cellY * _layout.TileSize);

    private void SyncBuildingTransforms()
    {
        for (var i = 0; i < _buildingEntityIds.Count; i++)
        {
            var building = _state.Buildings[i];
            var entity = World.GetEntity(_buildingEntityIds[i]);
            var grid = entity.Get<GridPosition>();
            grid.X = building.Cell.X;
            grid.Y = building.Cell.Y;
            entity.Get<Transform2>().Position = CellTopLeft(building.Cell.X, building.Cell.Y);
        }
    }

    private void SyncUnitTransformsFromState()
    {
        foreach (var unit in _state.Units)
        {
            if (!_unitEntityById.ContainsKey(unit.Id))
                CreateUnitVisual(unit, _textures);

            if (!_unitEntityById.TryGetValue(unit.Id, out var entityId))
                continue;

            var entity = World.GetEntity(entityId);
            var grid = entity.Get<GridPosition>();
            grid.X = unit.Cell.X;
            grid.Y = unit.Cell.Y;
            entity.Get<Transform2>().Position = CellTopLeft(unit.Cell.X, unit.Cell.Y);
        }
    }
}
