namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state bag for the match loading screen.</summary>
public sealed class LoadingViewModel
{
    public string Title { get; set; } = "Loading";

    public string StageLabel { get; set; } = "Preparing…";

    /// <summary>0..1 fill amount for the progress bar.</summary>
    public float ProgressFraction { get; set; }

    public string ProgressPercentText => $"{Math.Clamp((int)Math.Round(ProgressFraction * 100f), 0, 100)}%";
}
