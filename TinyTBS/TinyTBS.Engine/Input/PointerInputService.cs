using Microsoft.Xna.Framework.Input;

namespace TinyTBS.Engine.Input;

/// <summary>
/// Polls mouse (later touch) into <see cref="IPointerSource"/>.
/// </summary>
public sealed class PointerInputService : IPointerSource
{
    private bool _currentPrimary;
    private bool _previousPrimary;
    private bool _currentSecondary;
    private bool _previousSecondary;
    private bool _currentMiddle;
    private bool _previousMiddle;
    private int _scrollWheelValue;
    private int _previousScrollWheelValue;
    private ScreenPoint _position;

    public bool IsPrimaryDown => _currentPrimary;

    public bool WasPrimaryPressed => _currentPrimary && !_previousPrimary;

    public bool WasSecondaryPressed => _currentSecondary && !_previousSecondary;

    public bool WasMiddlePressed => _currentMiddle && !_previousMiddle;

    public bool WasAnyButtonPressed =>
        WasPrimaryPressed || WasSecondaryPressed || WasMiddlePressed;

    public int ScrollWheelDelta => _scrollWheelValue - _previousScrollWheelValue;

    public ScreenPoint Position => _position;

    public void Update()
    {
        _previousPrimary = _currentPrimary;
        _previousSecondary = _currentSecondary;
        _previousMiddle = _currentMiddle;
        _previousScrollWheelValue = _scrollWheelValue;

        var mouse = Mouse.GetState();
        _currentPrimary = mouse.LeftButton == ButtonState.Pressed;
        _currentSecondary = mouse.RightButton == ButtonState.Pressed;
        _currentMiddle = mouse.MiddleButton == ButtonState.Pressed;
        _scrollWheelValue = mouse.ScrollWheelValue;
        _position = new ScreenPoint(mouse.X, mouse.Y);
    }
}
