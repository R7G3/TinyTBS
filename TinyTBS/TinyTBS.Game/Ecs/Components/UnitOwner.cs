namespace TinyTBS.Game.Ecs.Components;

public sealed class UnitOwner
{
    public int PlayerIndex { get; set; }

    public UnitOwner(int playerIndex) => PlayerIndex = playerIndex;
}
