using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;
using TinyTBS.Engine.Ecs.Components;
using TinyTBS.Engine.Rendering;

namespace TinyTBS.Engine.Ecs.Systems;

/// <summary>
/// Draws base (white) then team mask (tinted). Expects an open SpriteBatch; positions synced outside Draw.
/// Scale follows <see cref="MatchBoardLayout.TileSize"/> so sprites track board zoom.
/// Order: <see cref="SpriteDrawLayer"/> ascending, then screen Y (stable over entity id churn).
/// </summary>
public sealed class TeamMaskedSpriteDrawSystem : EntityDrawSystem
{
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private readonly List<int> _drawOrder = [];
    private ComponentMapper<Transform2>? _transformMapper;
    private ComponentMapper<TeamMaskedSprite>? _spriteMapper;
    private ComponentMapper<SpriteDrawLayer>? _drawLayerMapper;

    public TeamMaskedSpriteDrawSystem(SpriteBatch spriteBatch, MatchBoardLayout layout)
        : base(Aspect.All(typeof(Transform2), typeof(TeamMaskedSprite)))
    {
        _spriteBatch = spriteBatch;
        _layout = layout;
    }

    public override void Initialize(IComponentMapperService mapperService)
    {
        _transformMapper = mapperService.GetMapper<Transform2>();
        _spriteMapper = mapperService.GetMapper<TeamMaskedSprite>();
        _drawLayerMapper = mapperService.GetMapper<SpriteDrawLayer>();
    }

    public override void Draw(GameTime gameTime)
    {
        if (_transformMapper is null || _spriteMapper is null)
            return;

        _drawOrder.Clear();
        foreach (var entityId in ActiveEntities)
            _drawOrder.Add(entityId);

        _drawOrder.Sort(CompareDrawOrder);

        foreach (var entityId in _drawOrder)
        {
            var transform = _transformMapper.Get(entityId);
            var visual = _spriteMapper.Get(entityId);
            var position = transform.Position;
            var textureWidth = Math.Max(1, visual.BaseTexture.Width);
            var scale = _layout.TileSize / (float)textureWidth;
            var baseColor = ApplySpentUnitTint(Color.White, visual.DimFactor, visual.GreyMix);

            _spriteBatch.Draw(
                visual.BaseTexture,
                position,
                sourceRectangle: null,
                baseColor,
                rotation: 0f,
                visual.Origin,
                scale: scale,
                SpriteEffects.None,
                layerDepth: 0f);

            // Gravestones use Transparent team color to skip the mask pass.
            if (visual.TeamColor.A == 0)
                continue;

            var maskColor = ApplySpentUnitTint(visual.TeamColor, visual.DimFactor, visual.GreyMix);
            _spriteBatch.Draw(
                visual.MaskTexture,
                position,
                sourceRectangle: null,
                maskColor,
                rotation: 0f,
                visual.Origin,
                scale: scale,
                SpriteEffects.None,
                layerDepth: 0f);
        }
    }

    private int CompareDrawOrder(int leftId, int rightId)
    {
        var leftLayer = _drawLayerMapper is not null && _drawLayerMapper.Has(leftId)
            ? _drawLayerMapper.Get(leftId).Layer
            : SpriteDrawLayer.Building;
        var rightLayer = _drawLayerMapper is not null && _drawLayerMapper.Has(rightId)
            ? _drawLayerMapper.Get(rightId).Layer
            : SpriteDrawLayer.Building;

        var layerCompare = leftLayer.CompareTo(rightLayer);
        if (layerCompare != 0)
            return layerCompare;

        var leftY = _transformMapper!.Get(leftId).Position.Y;
        var rightY = _transformMapper.Get(rightId).Position.Y;
        var yCompare = leftY.CompareTo(rightY);
        if (yCompare != 0)
            return yCompare;

        return leftId.CompareTo(rightId);
    }

    /// <summary>
    /// Darkens and greys RGB while keeping the sprite fully opaque (no wash-out transparency).
    /// </summary>
    private static Color ApplySpentUnitTint(Color color, float dimFactor, float greyMix)
    {
        if (color.A == 0)
            return Color.Transparent;

        var dim = Math.Clamp(dimFactor, 0f, 1f);
        var grey = Math.Clamp(greyMix, 0f, 1f);

        var red = color.R / 255f * dim;
        var green = color.G / 255f * dim;
        var blue = color.B / 255f * dim;

        if (grey > 0f)
        {
            const float midGrey = 0.48f;
            red += (midGrey - red) * grey;
            green += (midGrey - green) * grey;
            blue += (midGrey - blue) * grey;
        }

        return new Color(red, green, blue, 1f);
    }
}
