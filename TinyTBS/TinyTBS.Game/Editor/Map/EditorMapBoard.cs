using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using TinyTBS.Engine.Ecs.Components;
using TinyTBS.Engine.Ecs.Systems;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Maps;
using TinyTBS.Game.Match;
using TinyTBS.Game.Presentation.Match;
using TinyTBS.Game.Presentation.Shared;

namespace TinyTBS.Game.Editor.Map;

/// <summary>Lightweight board visual for EditableMapDocument (no MatchState).</summary>
public sealed class EditorMapBoard : IDisposable
{
    private readonly EditableMapDocument _document;
    private readonly MatchTextureAtlas _textures;
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private readonly Texture2D[] _tiles;
    private readonly CursorHighlightRenderer _cursorHighlight;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Texture2D _pixel;
    private readonly List<int> _overlayEntityIds = [];
    private readonly World _world;
    private bool _overlaysDirty = true;
    private bool _viewportReady;

    private static readonly Color MapFocusBorder = new(255, 220, 80);

    public EditorMapBoard(
        EditableMapDocument document,
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        MatchTextureAtlas textures)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _textures = textures ?? throw new ArgumentNullException(nameof(textures));
        _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _layout = new MatchBoardLayout(document.Width, document.Height);
        _cursorHighlight = new CursorHighlightRenderer(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _tiles = new Texture2D[document.Width * document.Height];
        RefreshAllTiles();
        _world = new WorldBuilder()
            .AddSystem(new TilemapDrawSystem(_graphicsDevice, _spriteBatch, _layout, _tiles))
            .AddSystem(new TeamMaskedSpriteDrawSystem(_spriteBatch, _layout))
            .Build();
        // Overlays built on first PrepareFrame after viewport origin is known.
    }

    public MatchBoardLayout Layout => _layout;

    public int CursorX { get; private set; }

    public int CursorY { get; private set; }

    public void MoveCursor(int deltaX, int deltaY)
    {
        CursorX = Math.Clamp(CursorX + deltaX, 0, _document.Width - 1);
        CursorY = Math.Clamp(CursorY + deltaY, 0, _document.Height - 1);
        _layout.KeepCellInCentralZone(CursorX, CursorY);
    }

    public void SetCursor(int x, int y)
    {
        CursorX = Math.Clamp(x, 0, _document.Width - 1);
        CursorY = Math.Clamp(y, 0, _document.Height - 1);
        _layout.KeepCellInCentralZone(CursorX, CursorY);
    }

    public void NotifyDocumentChanged()
    {
        RefreshAllTiles();
        _overlaysDirty = true;
    }

    public void PrepareFrame(int viewportWidth, int viewportHeight, GameTime gameTime)
    {
        _layout.UpdateForViewport(viewportWidth, viewportHeight);
        _viewportReady = true;

        // Entity create/destroy is applied in World.Update (same as MatchScene).
        _world.Update(gameTime);

        if (_overlaysDirty)
        {
            RebuildOverlays();
            _overlaysDirty = false;
            _world.Update(gameTime);
        }

        SyncOverlayTransforms();
    }

    public void Draw(GameTime gameTime, bool mapFocused)
    {
        if (!_viewportReady)
            return;

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.Identity);
        _world.Draw(gameTime);

        if (mapFocused)
        {
            const int thickness = 4;
            const int pad = 12;
            var bounds = new Rectangle(
                (int)_layout.Origin.X - pad,
                (int)_layout.Origin.Y - pad,
                _layout.Width * _layout.TileSize + pad * 2,
                _layout.Height * _layout.TileSize + pad * 2);
            SpriteBatchPrimitives.DrawRectBorder(_spriteBatch, _pixel, bounds, MapFocusBorder, thickness);
        }

        _spriteBatch.End();

        _cursorHighlight.Draw(
            _spriteBatch,
            _layout,
            CursorX,
            CursorY,
            hasSelection: false);
    }

    public void Dispose()
    {
        _world.Dispose();
        _cursorHighlight.Dispose();
        _pixel.Dispose();
    }

    private void RefreshAllTiles()
    {
        for (var y = 0; y < _document.Height; y++)
        {
            for (var x = 0; x < _document.Width; x++)
            {
                var terrain = MapSurfaceIds.ParseTerrain(_document.Surface[x, y]);
                _tiles[y * _document.Width + x] = _textures.Terrain(terrain);
            }
        }
    }

    private void RebuildOverlays()
    {
        foreach (var entityId in _overlayEntityIds)
            _world.DestroyEntity(entityId);
        _overlayEntityIds.Clear();

        foreach (var gravestone in _document.Gravestones)
        {
            _overlayEntityIds.Add(CreateMaskedVisual(
                gravestone.X,
                gravestone.Y,
                new TeamSprite(_textures.Gravestone, _textures.Gravestone),
                Color.Transparent,
                SpriteDrawLayer.Gravestone));
        }

        foreach (var building in _document.Buildings)
        {
            var ruined = MapSurfaceIds.IsRuinedBuildingState(building.State);
            _overlayEntityIds.Add(CreateMaskedVisual(
                building.X,
                building.Y,
                _textures.Building(building.Type, ruined),
                PlayerPalette.ForOwner(building.Slot),
                SpriteDrawLayer.Building));
        }

        foreach (var unit in _document.Units)
        {
            _overlayEntityIds.Add(CreateMaskedVisual(
                unit.X,
                unit.Y,
                _textures.Unit(unit.Type),
                PlayerPalette.ForPlayer(unit.Slot),
                SpriteDrawLayer.Unit));
        }
    }

    private void SyncOverlayTransforms()
    {
        // Same pattern as MatchScene: write Position every frame from grid → screen.
        foreach (var entityId in _overlayEntityIds)
        {
            var entity = _world.GetEntity(entityId);
            if (!entity.Has<GridPosition>() || !entity.Has<Transform2>())
                continue;

            var grid = entity.Get<GridPosition>();
            entity.Get<Transform2>().Position = _layout.GetCellTopLeft(grid.X, grid.Y);
        }
    }

    private int CreateMaskedVisual(
        int x,
        int y,
        TeamSprite sprite,
        Color teamColor,
        int drawLayer)
    {
        var entity = _world.CreateEntity();
        entity.Attach(new Transform2(_layout.GetCellTopLeft(x, y)));
        entity.Attach(new GridPosition(x, y));
        entity.Attach(new TeamMaskedSprite(sprite.Base, sprite.Mask, teamColor, Vector2.Zero));
        entity.Attach(new SpriteDrawLayer(drawLayer));
        return entity.Id;
    }
}
