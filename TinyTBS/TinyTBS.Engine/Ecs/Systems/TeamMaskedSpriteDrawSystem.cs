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

            _spriteBatch.Draw(
                visual.BaseTexture,
                position,
                sourceRectangle: null,
                Color.White,
                rotation: 0f,
                visual.Origin,
                scale: scale,
                SpriteEffects.None,
                layerDepth: 0f);

            _spriteBatch.Draw(
                visual.MaskTexture,
                position,
                sourceRectangle: null,
                visual.TeamColor,
                rotation: 0f,
                visual.Origin,
                scale: scale,
                SpriteEffects.None,
                layerDepth: 0f);
        }
    }
}
