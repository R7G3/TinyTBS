using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Game.Assets;

/// <summary>
/// A texture plus whether the caller must dispose it (raw/fallback assets only).
/// Textures from <see cref="Microsoft.Xna.Framework.Content.ContentManager"/> must not be disposed by screens.
/// </summary>
internal readonly struct LoadedTexture(Texture2D texture, bool disposeOnUnload)
{
    public Texture2D Texture { get; } = texture;

    public bool DisposeOnUnload { get; } = disposeOnUnload;

    public void DisposeIfOwned()
    {
        if (DisposeOnUnload)
            Texture.Dispose();
    }
}
