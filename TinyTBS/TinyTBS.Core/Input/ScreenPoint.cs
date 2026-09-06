namespace TinyTBS.Core.Input;

/// <summary>Screen-space point in pixels (device-agnostic; filled by engine input).</summary>
public readonly struct ScreenPoint(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
}
