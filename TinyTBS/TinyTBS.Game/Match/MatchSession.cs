using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;
using MonoGame.Extended.Graphics;
using TinyTBS.Core.Match;
using TinyTBS.Game.Ecs.Components;
using TinyTBS.Game.Ecs.Systems;

namespace TinyTBS.Game.Match;

/// <summary>
/// Minimal two-player match: select own unit, move one tile, end turn.
/// </summary>
public sealed class MatchSession : IDisposable
{
    private static readonly Color[] PlayerColors =
    [
        new(90, 160, 255),
        new(255, 120, 90),
    ];

    private readonly MatchBoardLayout _layout = new();
    private readonly List<int> _unitEntityIds = [];

    public MatchSession(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D unitTexture)
    {
        World = new WorldBuilder()
            .AddSystem(new GridDrawSystem(graphicsDevice, spriteBatch, _layout))
            .AddSystem(new UnitDrawSystem(spriteBatch, _layout))
            .Build();

        SpawnDemoUnits(unitTexture);
        Cursor = new GridCell(MatchDefaults.GridWidth / 2, MatchDefaults.GridHeight / 2);
    }

    public World World { get; }

    public MatchBoardLayout Layout => _layout;

    public int CurrentPlayer { get; private set; }

    public int? SelectedEntityId { get; private set; }

    public GridCell Cursor { get; set; }

    public string StatusText
    {
        get
        {
            if (SelectedEntityId is int entityId)
            {
                var grid = World.GetEntity(entityId).Get<GridPosition>();
                var owner = World.GetEntity(entityId).Get<UnitOwner>();
                return $"Player {CurrentPlayer + 1} — unit P{owner.PlayerIndex + 1} at {grid.Cell}";
            }

            return $"Player {CurrentPlayer + 1} — select a unit";
        }
    }

    public void MoveCursor(int deltaX, int deltaY)
    {
        var x = Math.Clamp(Cursor.X + deltaX, 0, MatchDefaults.GridWidth - 1);
        var y = Math.Clamp(Cursor.Y + deltaY, 0, MatchDefaults.GridHeight - 1);
        Cursor = new GridCell(x, y);
    }

    public void HandleConfirm()
    {
        if (SelectedEntityId is null)
        {
            TrySelectUnitAt(Cursor);
            return;
        }

        TryMoveSelectedUnitTo(Cursor);
    }

    public void HandlePointer(GridCell cell) => Cursor = cell;

    public void EndTurn()
    {
        CurrentPlayer = (CurrentPlayer + 1) % MatchDefaults.PlayerCount;
        SelectedEntityId = null;
    }

    public void Update(GameTime gameTime) => World.Update(gameTime);

    public void Draw(GameTime gameTime) => World.Draw(gameTime);

    public void Dispose() => World.Dispose();

    private void SpawnDemoUnits(Texture2D unitTexture)
    {
        AddUnit(unitTexture, new GridCell(1, 1), playerIndex: 0);
        AddUnit(unitTexture, new GridCell(MatchDefaults.GridWidth - 2, MatchDefaults.GridHeight - 2), playerIndex: 1);
    }

    private void AddUnit(Texture2D unitTexture, GridCell cell, int playerIndex)
    {
        var entity = World.CreateEntity();
        var region = new Texture2DRegion(unitTexture);
        var sprite = new Sprite(region)
        {
            Color = PlayerColors[playerIndex % PlayerColors.Length],
            Origin = new Vector2(region.Width * 0.5f, region.Height * 0.5f),
        };

        entity.Attach(new GridPosition(cell.X, cell.Y));
        entity.Attach(new UnitOwner(playerIndex));
        entity.Attach(new Transform2(_layout.CellToWorldCenter(cell)));
        entity.Attach(sprite);

        _unitEntityIds.Add(entity.Id);
    }

    private void TrySelectUnitAt(GridCell cell)
    {
        foreach (var entityId in _unitEntityIds)
        {
            var entity = World.GetEntity(entityId);
            if (entity.Get<GridPosition>().Cell != cell)
                continue;

            if (entity.Get<UnitOwner>().PlayerIndex != CurrentPlayer)
                return;

            SelectedEntityId = entityId;
            return;
        }
    }

    private void TryMoveSelectedUnitTo(GridCell destination)
    {
        if (SelectedEntityId is not int entityId)
            return;

        var entity = World.GetEntity(entityId);
        var currentCell = entity.Get<GridPosition>().Cell;
        if (currentCell.ManhattanDistanceTo(destination) != 1)
            return;

        if (IsOccupied(destination, exceptEntityId: entityId))
            return;

        var grid = entity.Get<GridPosition>();
        grid.X = destination.X;
        grid.Y = destination.Y;
        var transform = entity.Get<Transform2>();
        transform.Position = _layout.CellToWorldCenter(destination);
        SelectedEntityId = null;
    }

    private bool IsOccupied(GridCell cell, int? exceptEntityId = null)
    {
        foreach (var entityId in _unitEntityIds)
        {
            if (exceptEntityId == entityId)
                continue;

            if (World.GetEntity(entityId).Get<GridPosition>().Cell == cell)
                return true;
        }

        return false;
    }
}
