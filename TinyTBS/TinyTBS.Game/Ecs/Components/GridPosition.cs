using TinyTBS.Core.Match;

namespace TinyTBS.Game.Ecs.Components;

public sealed class GridPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public GridCell Cell => new(X, Y);
}
