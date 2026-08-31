using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TinyTBS.Core.Assets;
using TinyTBS.Core.IO;

namespace TinyTBS.Game;

public sealed class GameMain : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly IUserDataPaths _userDataPaths;
    private readonly IFileContentProvider _files;
    private readonly IAssetResolver _assets;

    private SpriteBatch? _spriteBatch;

    public GameMain(
        IUserDataPaths userDataPaths,
        IFileContentProvider files,
        IAssetResolver assets)
    {
        _userDataPaths = userDataPaths;
        _files = files;
        _assets = assets;

        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public IUserDataPaths UserDataPaths => _userDataPaths;
    public IFileContentProvider Files => _files;
    public IAssetResolver Assets => _assets;

    protected override void Initialize()
    {
        _userDataPaths.EnsureCreated();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        base.Draw(gameTime);
    }
}
