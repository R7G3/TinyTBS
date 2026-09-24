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
            return LoadFromFile(graphicsDevice, resolvedPath);

        try
        {
            return new LoadedTexture(content.Load<Texture2D>(contentAssetName), disposeOnUnload: false);
        }
        catch (ContentLoadException)
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a PNG from an absolute file path, or falls back to bundled content using a derived asset name.
    /// </summary>
    public static LoadedTexture? TryLoadModuleSprite(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        string absoluteFilePath,
        string moduleRelativePath)
    {
        if (File.Exists(absoluteFilePath))
            return LoadFromFile(graphicsDevice, absoluteFilePath);

        var contentAssetName = ModuleSpritePath.ToContentAssetName(moduleRelativePath);
        if (contentAssetName is null)
            return null;

        var logicalRelativePath = contentAssetName + ".png";
        return TryLoad(graphicsDevice, content, assets, logicalRelativePath, contentAssetName);
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

    public static LoadedTexture LoadModuleSpriteOrFallback(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets,
        string absoluteFilePath,
        string moduleRelativePath)
    {
        return TryLoadModuleSprite(
                graphicsDevice,
                content,
                assets,
                absoluteFilePath,
                moduleRelativePath)
            ?? new LoadedTexture(CreateSolid(graphicsDevice, 16, 16), disposeOnUnload: true);
    }

    private static LoadedTexture LoadFromFile(GraphicsDevice graphicsDevice, string absoluteFilePath)
    {
        using var stream = File.OpenRead(absoluteFilePath);
        return new LoadedTexture(Texture2D.FromStream(graphicsDevice, stream), disposeOnUnload: true);
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
