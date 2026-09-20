using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Game.Assets;

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
        var resolvedPath = assets.Resolve(logicalRelativePath);
        if (resolvedPath is not null && File.Exists(resolvedPath))
        {
            using var stream = File.OpenRead(resolvedPath);
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
        var pixels = new Microsoft.Xna.Framework.Color[width * height];
        Array.Fill(pixels, Microsoft.Xna.Framework.Color.White);
        texture.SetData(pixels);
        return texture;
    }
}
