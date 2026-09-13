namespace TinyTBS.Engine.Ecs.Components;

/// <summary>
/// Two-layer sprite: base drawn white, mask tinted with <see cref="TeamColor"/>.
/// </summary>
public sealed class TeamMaskedSprite
{
    public TeamMaskedSprite(
        Microsoft.Xna.Framework.Graphics.Texture2D baseTexture,
        Microsoft.Xna.Framework.Graphics.Texture2D maskTexture,
        Microsoft.Xna.Framework.Color teamColor,
        Microsoft.Xna.Framework.Vector2 origin)
    {
        BaseTexture = baseTexture;
        MaskTexture = maskTexture;
        TeamColor = teamColor;
        Origin = origin;
    }

    public Microsoft.Xna.Framework.Graphics.Texture2D BaseTexture { get; }

    public Microsoft.Xna.Framework.Graphics.Texture2D MaskTexture { get; }

    public Microsoft.Xna.Framework.Color TeamColor { get; set; }

    public Microsoft.Xna.Framework.Vector2 Origin { get; }
}
