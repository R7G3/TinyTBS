namespace TinyTBS.Game.Match;

/// <summary>Discrete progress snapshot while building a <see cref="GameplaySession"/>.</summary>
public readonly record struct MatchLoadProgress(int CompletedStages, int TotalStages, string StageLabel)
{
    public float Fraction =>
        TotalStages <= 0 ? 0f : Math.Clamp(CompletedStages / (float)TotalStages, 0f, 1f);

    public static MatchLoadProgress Starting(int totalStages, string stageLabel) =>
        new(CompletedStages: 0, TotalStages: totalStages, StageLabel: stageLabel);
}
