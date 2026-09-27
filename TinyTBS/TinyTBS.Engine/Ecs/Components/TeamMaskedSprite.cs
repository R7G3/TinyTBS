namespace TinyTBS.Engine.Ecs.Components;

/// <summary>
/// Two-layer sprite: base drawn white, mask tinted with <see cref="TeamColor"/>.
/// Inactive units use RGB dim + grey mix (alpha stays opaque).
/// </summary>
public sealed class TeamMaskedSprite
{
    public const float ActiveDimFactor = 1f;
    public const float ActiveGreyMix = 0f;

    /// <summary>Brightness for spent units (keep mostly opaque; do not use as alpha).</summary>
    public const float InactiveDimFactor = 0.88f;

    /// <summary>How far RGB is pushed toward mid-grey for spent units.</summary>
    public const float InactiveGreyMix = 0.3f;

    public TeamMaskedSprite(
        Microsoft.Xna.Framework.Graphics.Texture2D baseTexture,
        Microsoft.Xna.Framework.Graphics.Texture2D maskTexture,
        Microsoft.Xna.Framework.Color teamColor,
        Microsoft.Xna.Framework.Vector2 origin,
        float dimFactor = ActiveDimFactor,
        float greyMix = ActiveGreyMix)
    {
        BaseTexture = baseTexture;
        MaskTexture = maskTexture;
        TeamColor = teamColor;
        Origin = origin;
        DimFactor = dimFactor;
        GreyMix = greyMix;
    }

    public Microsoft.Xna.Framework.Graphics.Texture2D BaseTexture { get; set; }

    public Microsoft.Xna.Framework.Graphics.Texture2D MaskTexture { get; set; }

    public Microsoft.Xna.Framework.Color TeamColor { get; set; }

    public Microsoft.Xna.Framework.Vector2 Origin { get; }

    /// <summary>1 = full brightness; &lt;1 darkens RGB only (alpha stays opaque).</summary>
    public float DimFactor { get; set; }

    /// <summary>0 = no grey; 1 = full mid-grey. Applied after dim.</summary>
    public float GreyMix { get; set; }
}
