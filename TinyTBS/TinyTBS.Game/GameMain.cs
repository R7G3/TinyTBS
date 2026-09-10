using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Screens;

namespace TinyTBS.Game;

public sealed class GameMain : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenManager _screenManager;
    private readonly GameCommandService _commands = new();
    private readonly PointerInputService _pointer = new();
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
        _screenManager = new ScreenManager();
        Components.Add(_screenManager);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public IUserDataPaths UserDataPaths => _userDataPaths;
    public IFileContentProvider Files => _files;
    public IAssetResolver Assets => _assets;

    public IGameCommandSource Commands => _commands;

    public IPointerSource Pointer => _pointer;

    public SpriteBatch SharedSpriteBatch =>
        _spriteBatch ?? throw new InvalidOperationException("SpriteBatch is not loaded yet.");

    protected override void Initialize()
    {
        _userDataPaths.EnsureCreated();

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;

        base.Initialize();

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnWindowClientSizeChanged;

        GumBootstrap.Initialize(this);
        _screenManager.ShowScreen(new MainMenuScreen(this, _assets));
    }

    private void OnWindowClientSizeChanged(object? sender, EventArgs e)
    {
        var width = Window.ClientBounds.Width;
        var height = Window.ClientBounds.Height;
        if (width <= 0 || height <= 0)
            return;

        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
    }

    protected override void Update(GameTime gameTime)
    {
        _commands.Update();
        _pointer.Update();
        base.Update(gameTime);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }
}
