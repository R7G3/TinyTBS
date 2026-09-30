using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Screens;
using TinyTBS.Game.Assets;
using TinyTBS.Game.Input;
using TinyTBS.Engine.IO;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Engine.Input;
using TinyTBS.Game.Match;
using TinyTBS.Game.Saves;
using TinyTBS.Game.Screens;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game;

public sealed class GameMain : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly ScreenManager _screenManager;
    private readonly GameCommandService _commands = new(GumTextInputFocus.IsTextBoxReceivingInput);
    private readonly PointerInputService _pointer = new();
    private readonly IUserDataPaths _userDataPaths;
    private readonly IFileSystem _files;
    private readonly IAssetResolver _assets;
    private readonly IExternalFilePicker _filePicker;
    private readonly IExternalUriLauncher _uriLauncher;

    private SpriteBatch? _spriteBatch;
    private bool _applyingGraphicsChanges;
    private SuspendedMatchHold? _suspendedMatch;

    private const int DefaultWindowWidth = 1280;
    private const int DefaultWindowHeight = 786;

    public GameMain(
        IUserDataPaths userDataPaths,
        IFileSystem files,
        IAssetResolver assets,
        IExternalFilePicker filePicker,
        IExternalUriLauncher uriLauncher)
    {
        _userDataPaths = userDataPaths;
        _files = files;
        _assets = assets;
        _filePicker = filePicker;
        _uriLauncher = uriLauncher;

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = DefaultWindowWidth,
            PreferredBackBufferHeight = DefaultWindowHeight,
        };
        _screenManager = new ScreenManager();
        Components.Add(_screenManager);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public IUserDataPaths UserDataPaths => _userDataPaths;
    public IFileSystem Files => _files;
    public IAssetResolver Assets => _assets;
    public IExternalFilePicker FilePicker => _filePicker;

    public IExternalUriLauncher UriLauncher => _uriLauncher;

    public IGameCommandSource Commands => _commands;

    public IPointerSource Pointer => _pointer;

    public SpriteBatch SharedSpriteBatch =>
        _spriteBatch ?? throw new InvalidOperationException("SpriteBatch is not loaded yet.");

    public bool HasSuspendedMatch => _suspendedMatch is not null;

    public SuspendedMatchHold? SuspendedMatch => _suspendedMatch;

    public void SuspendMatch(GameplaySession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        ClearSuspendedMatch(dispose: true);
        _suspendedMatch = new SuspendedMatchHold(session);
    }

    public void ClearSuspendedMatch(bool dispose)
    {
        if (_suspendedMatch is null)
            return;

        if (dispose)
            _suspendedMatch.Session.Dispose();
        _suspendedMatch = null;
    }

    public GameplaySession? TakeSuspendedMatch()
    {
        if (_suspendedMatch is null)
            return null;

        var session = _suspendedMatch.Session;
        _suspendedMatch = null;
        return session;
    }

    protected override void Initialize()
    {
        _userDataPaths.EnsureCreated();

        base.Initialize();

        Window.AllowUserResizing = true;
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

    protected override void UnloadContent()
    {
        ClearSuspendedMatch(dispose: true);
        base.UnloadContent();
    }
}
