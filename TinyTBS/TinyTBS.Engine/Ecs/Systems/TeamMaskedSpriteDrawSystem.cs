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
/// </summary>
public sealed class TeamMaskedSpriteDrawSystem : EntityDrawSystem
{
    private readonly SpriteBatch _spriteBatch;
    private readonly MatchBoardLayout _layout;
    private ComponentMapper<Transform2>? _transformMapper;
    private ComponentMapper<TeamMaskedSprite>? _spriteMapper;

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
    }

    public override void Draw(GameTime gameTime)
    {
        if (_transformMapper is null || _spriteMapper is null)
            return;

        foreach (var entityId in ActiveEntities)
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
