using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Core.Assets;

namespace TinyTBS.Game.Assets;

/// <summary>
/// Engine: main-menu background texture resolved through <see cref="IAssetResolver"/>.
/// </summary>
public sealed class MainMenuBackground : IDisposable
{
    private readonly LoadedTexture? _texture;

    internal MainMenuBackground(LoadedTexture? texture)
    {
        _texture = texture;
    }

    public Texture2D? Texture => _texture?.Texture;

    public static MainMenuBackground Load(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        IAssetResolver assets)
    {
        var loaded = GameTextureLoader.TryLoad(
            graphicsDevice,
            content,
            assets,
            logicalRelativePath: "Images/placeholder.png",
            contentAssetName: "Images/placeholder");
        return new MainMenuBackground(loaded);
    }

    public void Dispose() => _texture?.DisposeIfOwned();
}
