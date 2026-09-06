using Microsoft.Xna.Framework.Input;
using TinyTBS.Core.Input;

namespace TinyTBS.Game.Input;

/// <summary>
/// Engine: polls mouse (later touch) into <see cref="IPointerSource"/>.
/// </summary>
public sealed class PointerInputService : IPointerSource
{
    private bool _currentPrimary;
    private bool _previousPrimary;
    private ScreenPoint _position;

    public bool IsPrimaryDown => _currentPrimary;

    public bool WasPrimaryPressed => _currentPrimary && !_previousPrimary;

    public ScreenPoint Position => _position;

    public void Update()
    {
        _previousPrimary = _currentPrimary;

        var mouse = Mouse.GetState();
        _currentPrimary = mouse.LeftButton == ButtonState.Pressed;
        _position = new ScreenPoint(mouse.X, mouse.Y);
    }
}
