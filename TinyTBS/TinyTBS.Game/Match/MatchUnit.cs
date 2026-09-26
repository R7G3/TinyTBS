using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>Logical unit on the match grid (no rendering).</summary>
public sealed class MatchUnit
{
    public MatchUnit(
        int id,
        ContentId typeId,
        GridCell cell,
        int playerIndex,
        int maxHealth,
        int hitPoints)
    {
        if (maxHealth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHealth));
        if (hitPoints <= 0 || hitPoints > maxHealth)
            throw new ArgumentOutOfRangeException(nameof(hitPoints));

        Id = id;
        TypeId = typeId;
        Cell = cell;
        PlayerIndex = playerIndex;
        MaxHealth = maxHealth;
        HitPoints = hitPoints;
    }

    public int Id { get; }

    public ContentId TypeId { get; }

    public GridCell Cell { get; set; }

    public int PlayerIndex { get; }

    public int MaxHealth { get; }

    public int HitPoints { get; set; }
}
