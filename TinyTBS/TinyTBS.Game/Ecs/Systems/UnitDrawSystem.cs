using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;
using MonoGame.Extended.Graphics;
using TinyTBS.Game.Ecs.Components;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Ecs.Systems;

/// <summary>
/// Draws unit sprites. Reads <see cref="Transform2"/> only — positions are synced outside Draw.
/// </summary>
public sealed class UnitDrawSystem : EntityDrawSystem
{
    private readonly SpriteBatch _spriteBatch;
    private ComponentMapper<Transform2>? _transformMapper;
    private ComponentMapper<Sprite>? _spriteMapper;

    public UnitDrawSystem(SpriteBatch spriteBatch)
        : base(Aspect.All(typeof(Transform2), typeof(Sprite)))
    {
        _spriteBatch = spriteBatch;
    }

    public override void Initialize(IComponentMapperService mapperService)
    {
        _transformMapper = mapperService.GetMapper<Transform2>();
        _spriteMapper = mapperService.GetMapper<Sprite>();
    }

    public override void Draw(GameTime gameTime)
    {
        if (_transformMapper is null || _spriteMapper is null)
            return;

        _spriteBatch.Begin();

        foreach (var entityId in ActiveEntities)
        {
            var transform = _transformMapper.Get(entityId);
            var sprite = _spriteMapper.Get(entityId);
            _spriteBatch.Draw(sprite, transform);
        }

        _spriteBatch.End();
    }
}
