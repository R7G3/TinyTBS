using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Match;

/// <summary>Logical unit on the match grid (no rendering).</summary>
public sealed class MatchUnit
{
    public const int MaxLevel = 9;

    /// <summary>Cumulative XP needed for level N is <c>N * XpPerLevel</c> (L1=2 … L9=18).</summary>
    public const int XpPerLevel = 2;

    public const int MaxExperience = MaxLevel * XpPerLevel;

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
        IsActive = true;
    }

    public int Id { get; }

    public ContentId TypeId { get; }

    public GridCell Cell { get; set; }

    public int PlayerIndex { get; }

    public int MaxHealth { get; }

    public int HitPoints { get; set; }

    /// <summary>False after attack / capture / wait / exclusive move until next owner turn.</summary>
    public bool IsActive { get; set; }

    /// <summary>True after a move this activation (still may act unless moveOrAttackExclusive).</summary>
    public bool HasMovedThisActivation { get; set; }

    /// <summary>
    /// Cell before the move this activation; used to cancel / undo a post-move.
    /// Valid only while <see cref="HasMovedThisActivation"/> is true.
    /// </summary>
    public GridCell CellBeforeMove { get; set; }

    /// <summary>Accumulated XP for level thresholds (L1=2 … L9=18).</summary>
    public int Experience { get; set; }

    /// <summary>0–9 from <see cref="Experience"/>.</summary>
    public int Level => Math.Min(MaxLevel, Experience / XpPerLevel);

    /// <summary>Flat attack bonus from level (+2 per level; L0 = 0).</summary>
    public int AttackBonusFromLevel => Level * 2;

    public void GainExperience(int amount)
    {
        if (amount <= 0)
            return;
        Experience = Math.Min(Experience + amount, MaxExperience);
    }

    public void ResetProgression()
    {
        Experience = 0;
    }
}
