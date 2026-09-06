namespace TinyTBS.Core.Match;

/// <summary>Integer tile coordinates on a match grid.</summary>
public readonly struct GridCell(int x, int y) : IEquatable<GridCell>
{
    public int X { get; } = x;
    public int Y { get; } = y;

    public int ManhattanDistanceTo(GridCell other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public bool Equals(GridCell other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is GridCell other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(GridCell left, GridCell right) => left.Equals(right);

    public static bool operator !=(GridCell left, GridCell right) => !left.Equals(right);

    public override string ToString() => $"({X}, {Y})";
}
