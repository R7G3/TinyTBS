namespace TinyTBS.Game.ViewModels;

/// <summary>One level row under the selected scenario.</summary>
public sealed class NewGameLevelRowViewModel
{
    public required string LevelId { get; init; }

    public required string Title { get; init; }

    public string ModesLabel { get; init; } = string.Empty;

    public bool IsSelected { get; init; }

    public bool IsLocked { get; init; }

    public string SummaryLine
    {
        get
        {
            var body = string.IsNullOrWhiteSpace(ModesLabel)
                ? Title
                : $"{Title} · {ModesLabel}";
            if (IsLocked)
                body += " · locked";
            return IsSelected ? $"[ {body} ]" : body;
        }
    }
}
