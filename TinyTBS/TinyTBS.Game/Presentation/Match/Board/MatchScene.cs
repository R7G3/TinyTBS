using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using TinyTBS.Engine.Ecs.Components;
using TinyTBS.Engine.Ecs.Systems;
using TinyTBS.Engine.Rendering;
using TinyTBS.Rules.Match;
using TinyTBS.Game.Presentation.Shared;
using EcsWorld = MonoGame.Extended.ECS.World;

namespace TinyTBS.Game.Presentation.Match.Board;

/// <summary>
/// Visual side of a match: tilemap, gravestones, buildings, units synced from <see cref="MatchState"/>.
/// Unit moves animate along a cheapest terrain path (presentation only).
/// </summary>
public sealed class MatchScene : IDisposable
{
    private readonly MatchState _state;
    private readonly MatchCursor _cursor;
    private readonly MatchBoardLayout _layout;
    private readonly MatchTextureAtlas _textures;
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchUnitMoveAnimator _moveAnimator = new();
    private readonly MatchCursorAnimator _cursorAnimator = new();
    private readonly List<int> _gravestoneEntityIds = [];
    private readonly List<int> _buildingEntityIds = [];
    private readonly Dictionary<int, int> _unitEntityById = new();

    /// <summary>Last MoveUnit we already started animating (avoid re-trigger while LastAction persists).</summary>
    private (int UnitId, GridCell Source, GridCell Target)? _startedMoveKey;

    public MatchScene(
        MatchState state,
        MatchCursor cursor,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        MatchTextureAtlas textures)
    {
        _state = state;
        _cursor = cursor;
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

        foreach (var gravestone in state.Gravestones)
        {
            // Gravestone art has no team mask; transparent tint skips the second draw pass.
            var entityId = CreateMaskedVisual(
                gravestone.Cell,
                new TeamSprite(textures.Gravestone, textures.Gravestone),
                Color.Transparent,
                SpriteDrawLayer.Gravestone);
            World.GetEntity(entityId).Attach(new GridPosition(gravestone.Cell.X, gravestone.Cell.Y));
            _gravestoneEntityIds.Add(entityId);
        }

        foreach (var building in state.Buildings)
        {
            var entityId = CreateMaskedVisual(
                building.Cell,
                textures.Building(building.TypeId, building.IsRuined),
                PlayerPalette.ForOwner(building.OwnerPlayerIndex),
                SpriteDrawLayer.Building);
            World.GetEntity(entityId).Attach(new GridPosition(building.Cell.X, building.Cell.Y));
            _buildingEntityIds.Add(entityId);
        }

        foreach (var unit in state.Units)
            CreateUnitVisual(unit, textures);
    }

    public EcsWorld World { get; }

    public MatchBoardLayout Layout => _layout;

    /// <summary>True while a unit sprite is sliding along its move path.</summary>
    public bool IsMoveAnimating => _moveAnimator.IsActive;

    /// <summary>True while the board cursor is sliding (bot aim).</summary>
    public bool IsCursorAnimating => _cursorAnimator.IsActive;

    public void PrepareFrame(int viewportWidth, int viewportHeight)
    {
        _layout.UpdateForViewport(viewportWidth, viewportHeight);
        TryBeginMoveAnimationFromLastAction();
        CancelAnimationIfLogicMovedAway();
        SyncGravestoneTransforms();
        SyncBuildingTransforms();
        SyncUnitTransformsFromState();
    }

    /// <summary>Advance move + cursor presentation; call once per Update with frame delta.</summary>
    public void TickMoveAnimation(GameTime gameTime)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _moveAnimator.Update(elapsed);
        _cursorAnimator.Update(elapsed);
    }

    /// <summary>
    /// Starts a Manhattan cursor slide to <paramref name="to"/>.
    /// Returns false if already there (caller may act immediately).
    /// </summary>
    public bool BeginCursorAim(GridCell from, GridCell to) =>
        _cursorAnimator.Begin(from, to);

    public void CancelCursorAim() => _cursorAnimator.Cancel();

    /// <summary>Cell under the animated cursor, or null if not aiming.</summary>
    public bool TryGetCursorAimLogicalCell(out GridCell cell) =>
        _cursorAnimator.TryGetLogicalCell(out cell);

    /// <summary>
    /// Where to draw the cursor: fractional cell while aiming, else the logical <see cref="MatchCursor"/> cell.
    /// </summary>
    public void GetVisualCursorCell(out float cellX, out float cellY)
    {
        if (_cursorAnimator.TryGetVisualCell(out cellX, out cellY))
            return;

        cellX = _cursor.Cell.X;
        cellY = _cursor.Cell.Y;
    }

    public void Update(GameTime gameTime) => World.Update(gameTime);

    /// <summary>
    /// One SpriteBatch pass for tiles and team sprites; overlays draw after that batch ends
    /// so they can use their own blend state (e.g. Multiply for threat tints).
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
        _spriteBatch.End();

        afterEntities?.Invoke(_spriteBatch, _layout);
    }

    /// <summary>World top-left of the unit sprite (animated position when walking).</summary>
    public Vector2 GetUnitVisualTopLeft(MatchUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (_moveAnimator.TryGetVisualTopLeft(unit.Id, CellTopLeft, out var animated))
            return animated;
        return CellTopLeft(unit.Cell.X, unit.Cell.Y);
    }

    public void Dispose() => World.Dispose();

    private void TryBeginMoveAnimationFromLastAction()
    {
        if (_moveAnimator.IsActive)
            return;

        if (_state.LastAction is not { Kind: MatchActionKind.MoveUnit } action)
            return;
        if (action.UnitId is not int unitId
            || action.Source is not GridCell source
            || action.Target is not GridCell target)
        {
            return;
        }

        var key = (unitId, source, target);
        if (_startedMoveKey == key)
            return;

        if (!_state.TryGetUnit(unitId, out var moving)
            || !_state.ContentCatalog.TryGetUnit(moving.TypeId, out var definition))
        {
            _startedMoveKey = key;
            return;
        }

        // Unit logically already on target; exclude it so the destination is not "blocked by self".
        var path = MatchPathfinder.FindCheapestPath(
            _state,
            source,
            target,
            definition.MovementClass,
            definition.Speed,
            exceptUnitId: unitId);

        _moveAnimator.Begin(unitId, path);
        _startedMoveKey = key;
    }

    private void CancelAnimationIfLogicMovedAway()
    {
        if (!_moveAnimator.IsActive || _moveAnimator.UnitId is not int unitId)
            return;

        foreach (var unit in _state.Units)
        {
            if (unit.Id != unitId)
                continue;

            // Undo move / unexpected teleport: snap cancel.
            if (unit.Cell != _moveAnimator.TargetCell)
                _moveAnimator.Cancel();
            return;
        }

        // Unit died mid-walk.
        _moveAnimator.Cancel();
    }

    private void CreateUnitVisual(MatchUnit unit, MatchTextureAtlas textures)
    {
        var entityId = CreateMaskedVisual(
            unit.Cell,
            textures.Unit(unit.TypeId),
            PlayerPalette.ForPlayer(unit.PlayerIndex),
            SpriteDrawLayer.Unit);

        World.GetEntity(entityId).Attach(new UnitOwner(unit.PlayerIndex));
        World.GetEntity(entityId).Attach(new GridPosition(unit.Cell.X, unit.Cell.Y));
        _unitEntityById[unit.Id] = entityId;
    }

    private int CreateMaskedVisual(GridCell cell, TeamSprite sprite, Color teamColor, int drawLayer)
    {
        var entity = World.CreateEntity();
        // Same footprint as terrain tiles: sprite origin = top-left of the cell.
        var origin = Vector2.Zero;

        entity.Attach(new Transform2(CellTopLeft(cell.X, cell.Y)));
        entity.Attach(new TeamMaskedSprite(sprite.Base, sprite.Mask, teamColor, origin));
        entity.Attach(new SpriteDrawLayer(drawLayer));
        return entity.Id;
    }

    private Vector2 CellTopLeft(int cellX, int cellY) =>
        _layout.Origin + new Vector2(cellX * _layout.TileSize, cellY * _layout.TileSize);

    private void SyncGravestoneTransforms()
    {
        // Rebuild visuals if count diverged (combat may add stones).
        while (_gravestoneEntityIds.Count > _state.Gravestones.Count)
        {
            var last = _gravestoneEntityIds[^1];
            _gravestoneEntityIds.RemoveAt(_gravestoneEntityIds.Count - 1);
            World.DestroyEntity(last);
        }

        while (_gravestoneEntityIds.Count < _state.Gravestones.Count)
        {
            var cell = _state.Gravestones[_gravestoneEntityIds.Count].Cell;
            var entityId = CreateMaskedVisual(
                cell,
                new TeamSprite(_textures.Gravestone, _textures.Gravestone),
                Color.Transparent,
                SpriteDrawLayer.Gravestone);
            World.GetEntity(entityId).Attach(new GridPosition(cell.X, cell.Y));
            _gravestoneEntityIds.Add(entityId);
        }

        for (var i = 0; i < _gravestoneEntityIds.Count; i++)
        {
            var cell = _state.Gravestones[i].Cell;
            var entity = World.GetEntity(_gravestoneEntityIds[i]);
            var grid = entity.Get<GridPosition>();
            grid.X = cell.X;
            grid.Y = cell.Y;
            entity.Get<Transform2>().Position = CellTopLeft(cell.X, cell.Y);
        }
    }

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

            var sprite = _textures.Building(building.TypeId, building.IsRuined);
            var masked = entity.Get<TeamMaskedSprite>();
            masked.BaseTexture = sprite.Base;
            masked.MaskTexture = sprite.Mask;
            masked.TeamColor = PlayerPalette.ForOwner(building.OwnerPlayerIndex);
        }
    }

    private void SyncUnitTransformsFromState()
    {
        var livingIds = new HashSet<int>();
        foreach (var unit in _state.Units)
        {
            livingIds.Add(unit.Id);
            if (!_unitEntityById.ContainsKey(unit.Id))
                CreateUnitVisual(unit, _textures);

            if (!_unitEntityById.TryGetValue(unit.Id, out var entityId))
                continue;

            var entity = World.GetEntity(entityId);
            var grid = entity.Get<GridPosition>();
            grid.X = unit.Cell.X;
            grid.Y = unit.Cell.Y;

            // While walking, sprite follows the path; grid component stays on logical cell.
            entity.Get<Transform2>().Position = GetUnitVisualTopLeft(unit);

            var masked = entity.Get<TeamMaskedSprite>();
            masked.TeamColor = PlayerPalette.ForPlayer(unit.PlayerIndex);
            masked.DimFactor = unit.IsActive
                ? TeamMaskedSprite.ActiveDimFactor
                : TeamMaskedSprite.InactiveDimFactor;
            masked.GreyMix = unit.IsActive
                ? TeamMaskedSprite.ActiveGreyMix
                : TeamMaskedSprite.InactiveGreyMix;
        }

        foreach (var pair in _unitEntityById.ToArray())
        {
            if (livingIds.Contains(pair.Key))
                continue;

            World.DestroyEntity(pair.Value);
            _unitEntityById.Remove(pair.Key);
        }
    }
}
