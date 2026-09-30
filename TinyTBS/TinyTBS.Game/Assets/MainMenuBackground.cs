using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TinyTBS.Game.Assets;

/// <summary>
/// Menu background: infinite tile field with a slow drifting "camera" that occasionally turns.
/// Flight state is static so the view continues across menu / editor shell screens.
/// </summary>
public sealed class MainMenuBackground : IDisposable
{
    private const float DimOpacity = 0.35f;
    private const float SpeedPixelsPerSecond = 32f;
    private const float TurnRadiansPerSecond = 0.28f;
    private const float MaxHeadingDeltaRadians = 0.85f; // ~49° — no U-turns
    private const float MinSecondsUntilRetarget = 4f;
    private const float MaxSecondsUntilRetarget = 11f;
    private const float MaxDeltaSeconds = 1f / 20f;

    private static readonly Random SharedRandom = new();

    private static bool s_flightInitialized;
    private static Vector2 s_camera;
    private static float s_headingRadians;
    private static float s_targetHeadingRadians;
    private static float s_secondsUntilRetarget;

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
            logicalRelativePath: "Images/menu_background.png",
            contentAssetName: "Images/menu_background");
        return new MainMenuBackground(loaded);
    }

    /// <summary>
    /// Advances the shared camera and fills the viewport by tiling the texture.
    /// </summary>
    public void Draw(
        SpriteBatch spriteBatch,
        int viewportWidth,
        int viewportHeight,
        GameTime gameTime)
    {
        AdvanceFlight((float)gameTime.ElapsedGameTime.TotalSeconds);

        var texture = Texture;
        if (texture is null || viewportWidth <= 0 || viewportHeight <= 0)
            return;

        var tileWidth = texture.Width;
        var tileHeight = texture.Height;
        if (tileWidth <= 0 || tileHeight <= 0)
            return;

        // Sub-pixel scroll + linear filter avoids the 1px stair-steps of Point sampling.
        var originX = PositiveModulo(s_camera.X, tileWidth);
        var originY = PositiveModulo(s_camera.Y, tileHeight);
        var color = Color.White * DimOpacity;

        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        for (var y = -originY; y < viewportHeight; y += tileHeight)
        {
            for (var x = -originX; x < viewportWidth; x += tileWidth)
                spriteBatch.Draw(texture, new Vector2(x, y), color);
        }

        spriteBatch.End();
    }

    public void Dispose() => _texture?.DisposeIfOwned();

    private static void AdvanceFlight(float elapsedSeconds)
    {
        if (elapsedSeconds <= 0f)
            return;

        // Spikes (alt-tab, hitch) would otherwise look like a teleport.
        elapsedSeconds = Math.Min(elapsedSeconds, MaxDeltaSeconds);

        EnsureFlightInitialized();

        s_secondsUntilRetarget -= elapsedSeconds;
        if (s_secondsUntilRetarget <= 0f)
            PickNewTargetHeading();

        var headingDelta = MathHelper.WrapAngle(s_targetHeadingRadians - s_headingRadians);
        var maxTurn = TurnRadiansPerSecond * elapsedSeconds;
        if (Math.Abs(headingDelta) <= maxTurn)
            s_headingRadians = s_targetHeadingRadians;
        else
            s_headingRadians += Math.Sign(headingDelta) * maxTurn;

        s_headingRadians = MathHelper.WrapAngle(s_headingRadians);

        var direction = new Vector2(MathF.Cos(s_headingRadians), MathF.Sin(s_headingRadians));
        s_camera += direction * (SpeedPixelsPerSecond * elapsedSeconds);
    }

    private static void EnsureFlightInitialized()
    {
        if (s_flightInitialized)
            return;

        s_headingRadians = RandomAngle();
        s_targetHeadingRadians = s_headingRadians;
        PickNewTargetHeading();
        s_flightInitialized = true;
    }

    private static void PickNewTargetHeading()
    {
        // Relative nudge from current heading — keeps the drift gentle, no left/right snaps.
        var delta = ((float)SharedRandom.NextDouble() * 2f - 1f) * MaxHeadingDeltaRadians;
        s_targetHeadingRadians = MathHelper.WrapAngle(s_headingRadians + delta);
        var span = MaxSecondsUntilRetarget - MinSecondsUntilRetarget;
        s_secondsUntilRetarget = MinSecondsUntilRetarget + (float)SharedRandom.NextDouble() * span;
    }

    private static float RandomAngle() =>
        (float)(SharedRandom.NextDouble() * Math.PI * 2.0);

    private static float PositiveModulo(float value, float period)
    {
        var remainder = value % period;
        return remainder < 0f ? remainder + period : remainder;
    }
}
