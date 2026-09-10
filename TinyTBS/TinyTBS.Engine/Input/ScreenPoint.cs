namespace TinyTBS.Engine.Input;

/// <summary>Screen-space point in pixels (device / pointer).</summary>
public readonly struct ScreenPoint(int x, int y) : IEquatable<ScreenPoint>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public bool Equals(ScreenPoint other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is ScreenPoint other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(ScreenPoint left, ScreenPoint right) => left.Equals(right);

    public static bool operator !=(ScreenPoint left, ScreenPoint right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y})";
}
