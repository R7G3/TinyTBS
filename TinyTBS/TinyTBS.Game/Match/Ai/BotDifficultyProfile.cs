namespace TinyTBS.Game.Match.Ai;

/// <summary>Tunable search limits and eval style for a difficulty.</summary>
public sealed class BotDifficultyProfile
{
    public required int MaxDepth { get; init; }

    public required int NodeLimit { get; init; }

    public required bool UseQuiescence { get; init; }

    /// <summary>Extra plies after depth for noisy actions (attacks) when quiescence is on.</summary>
    public int QuiescencePlies { get; init; }

    public required bool RichEvaluation { get; init; }

    /// <summary>Weight for closing distance to enemies (per Manhattan step, capped).</summary>
    public int AggressionWeight { get; init; } = 3;

    /// <summary>
    /// Enemy distance beyond this is scored as this value — Easy won't race across the whole map.
    /// </summary>
    public int AggressionRangeCap { get; init; } = 99;

    /// <summary>Pull toward own recruit buildings (castle). Higher = more defensive / turtling.</summary>
    public int HomeBiasWeight { get; init; }

    /// <summary>Flat score per living unit (recruit incentive).</summary>
    public int UnitCountWeight { get; init; } = 120;

    /// <summary>
    /// When true, Wait may compete with pure repositioning (attacks/captures/recruits still forced).
    /// </summary>
    public bool AllowWaitWithMoves { get; init; }

    public static BotDifficultyProfile For(BotDifficulty difficulty) =>
        difficulty switch
        {
            BotDifficulty.Normal => new BotDifficultyProfile
            {
                MaxDepth = 2,
                NodeLimit = 2500,
                UseQuiescence = true,
                QuiescencePlies = 2,
                RichEvaluation = true,
                AggressionWeight = 3,
                AggressionRangeCap = 99,
                HomeBiasWeight = 0,
                UnitCountWeight = 120,
                AllowWaitWithMoves = false,
            },
            BotDifficulty.Hard => new BotDifficultyProfile
            {
                // Placeholder until Hard / full-turn policy ships.
                MaxDepth = 3,
                NodeLimit = 8000,
                UseQuiescence = true,
                QuiescencePlies = 3,
                RichEvaluation = true,
                AggressionWeight = 3,
                AggressionRangeCap = 99,
                HomeBiasWeight = 0,
                UnitCountWeight = 120,
                AllowWaitWithMoves = false,
            },
            _ => new BotDifficultyProfile
            {
                MaxDepth = 2,
                NodeLimit = 800,
                UseQuiescence = false,
                QuiescencePlies = 0,
                RichEvaluation = false,
                // Milder: short chase horizon, cling to castle a bit, Wait OK if not fighting.
                AggressionWeight = 1,
                AggressionRangeCap = 4,
                HomeBiasWeight = 2,
                UnitCountWeight = 70,
                AllowWaitWithMoves = true,
            },
        };
}
