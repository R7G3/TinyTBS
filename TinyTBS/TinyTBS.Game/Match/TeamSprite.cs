using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Game.Match;

/// <summary>Base + team-mask texture pair for tinted unit/building sprites.</summary>
public readonly struct TeamSprite(Texture2D baseTexture, Texture2D maskTexture)
{
    public Texture2D Base { get; } = baseTexture;
    public Texture2D Mask { get; } = maskTexture;
}
