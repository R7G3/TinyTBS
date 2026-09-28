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
    private readonly IExternalFilePicker _filePicker;

    private SpriteBatch? _spriteBatch;
    private bool _applyingGraphicsChanges;

    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 786;

    public GameMain(
        IUserDataPaths userDataPaths,
        IFileContentProvider files,
        IAssetResolver assets,
        IExternalFilePicker filePicker)
    {
        _userDataPaths = userDataPaths;
        _files = files;
        _assets = assets;
        _filePicker = filePicker;

        _graphics = new GraphicsDeviceManager(this)
        {
            // Must be set in the constructor — after the first device create,
            // MonoGame keeps the platform default (800×480) until ApplyChanges.
            PreferredBackBufferWidth = DefaultWindowWidth,
            PreferredBackBufferHeight = DefaultWindowHeight,
        };
        _screenManager = new ScreenManager();
        Components.Add(_screenManager);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public IUserDataPaths UserDataPaths => _userDataPaths;
    public IFileContentProvider Files => _files;
    public IAssetResolver Assets => _assets;
    public IExternalFilePicker FilePicker => _filePicker;

    public IGameCommandSource Commands => _commands;

    public IPointerSource Pointer => _pointer;

    public SpriteBatch SharedSpriteBatch =>
        _spriteBatch ?? throw new InvalidOperationException("SpriteBatch is not loaded yet.");

    protected override void Initialize()
    {
        _userDataPaths.EnsureCreated();

        base.Initialize();

        Window.AllowUserResizing = true;

        // Re-assert size after device/window creation (DesktopGL sometimes ignores ctor prefs).
        ApplyBackBufferSize(DefaultWindowWidth, DefaultWindowHeight);

        Window.ClientSizeChanged += OnWindowClientSizeChanged;

        GumBootstrap.Initialize(this);
        _screenManager.ShowScreen(new MainMenuScreen(this, _assets));
    }

    private void OnWindowClientSizeChanged(object? sender, EventArgs e)
    {
        if (_applyingGraphicsChanges)
            return;

        var width = Window.ClientBounds.Width;
        var height = Window.ClientBounds.Height;
        if (width <= 0 || height <= 0)
            return;

        ApplyBackBufferSize(width, height);
    }

    private void ApplyBackBufferSize(int width, int height)
    {
        if (_graphics.PreferredBackBufferWidth == width
            && _graphics.PreferredBackBufferHeight == height
            && GraphicsDevice is not null
            && GraphicsDevice.PresentationParameters.BackBufferWidth == width
            && GraphicsDevice.PresentationParameters.BackBufferHeight == height)
        {
            return;
        }

        _applyingGraphicsChanges = true;
        try
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
        }
        finally
        {
            _applyingGraphicsChanges = false;
        }
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
