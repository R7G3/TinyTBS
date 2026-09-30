namespace TinyTBS.Game.Match.Loading;

/// <summary>Discrete progress snapshot while a match load pipeline runs its stages.</summary>
public readonly record struct MatchLoadProgress(int CompletedStages, int TotalStages, string StageLabel)
{
    public float Fraction =>
        TotalStages <= 0 ? 0f : Math.Clamp(CompletedStages / (float)TotalStages, 0f, 1f);

    public static MatchLoadProgress Starting(int totalStages, string stageLabel) =>
        new(CompletedStages: 0, TotalStages: totalStages, StageLabel: stageLabel);
}
