namespace TinyTBS.Engine.Ecs.Components;

/// <summary>
/// Painter order for board sprites (lower draws first). Used when Deferred SpriteBatch ignores layerDepth.
/// </summary>
public sealed class SpriteDrawLayer
{
    public const int Gravestone = 0;
    public const int Building = 1;
    public const int Unit = 2;

    public int Layer { get; set; }

    public SpriteDrawLayer(int layer) => Layer = layer;
}
