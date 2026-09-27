namespace TinyTBS.Game.ViewModels;

/// <summary>One row on the Mode tab.</summary>
public sealed class NewGameModeRowViewModel
{
    public required NewGamePlayMode Mode { get; init; }

    public required string Title { get; init; }

    public required string DetailLine { get; init; }

    public bool IsSelected { get; init; }

    public string SummaryLine
    {
        get
        {
            var body = string.IsNullOrWhiteSpace(DetailLine) ? Title : $"{Title} — {DetailLine}";
            return IsSelected ? $"[ {body} ]" : body;
        }
    }
}
