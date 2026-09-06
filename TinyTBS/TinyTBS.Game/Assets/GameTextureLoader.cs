using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Core.Assets;

namespace TinyTBS.Game.Assets;

/// <summary>
/// A texture plus whether the caller must dispose it (raw/fallback assets only).
/// Textures from <see cref="ContentManager"/> must not be disposed by screens.
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

/// <summary>
/// Loads textures from mod/raw paths first, then from the MonoGame content pipeline (.xnb).
/// </summary>
internal static class GameTextureLoader
{
    public static LoadedTexture? TryLoad(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        string logicalRelativePath,
        string contentAssetName)
    {
        var path = assets.Resolve(logicalRelativePath);
        if (path is not null && File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            return new LoadedTexture(Texture2D.FromStream(graphicsDevice, stream), disposeOnUnload: true);
        }

        try
        {
            return new LoadedTexture(content.Load<Texture2D>(contentAssetName), disposeOnUnload: false);
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    public static LoadedTexture LoadOrFallback(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        string logicalRelativePath,
        string contentAssetName)
    {
        return TryLoad(graphicsDevice, content, assets, logicalRelativePath, contentAssetName)
            ?? new LoadedTexture(CreateSolid(graphicsDevice, 16, 16), disposeOnUnload: true);
    }

    private static Texture2D CreateSolid(GraphicsDevice graphicsDevice, int width, int height)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var data = new Microsoft.Xna.Framework.Color[width * height];
        Array.Fill(data, Microsoft.Xna.Framework.Color.White);
        texture.SetData(data);
        return texture;
    }
}
