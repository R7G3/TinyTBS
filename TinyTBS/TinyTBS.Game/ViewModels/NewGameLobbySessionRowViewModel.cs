namespace TinyTBS.Game.ViewModels;

/// <summary>One session-type row on the Lobby tab (Hotseat today; others greyed).</summary>
public sealed class NewGameLobbySessionRowViewModel
{
    public required NewGameLobbySessionMode Mode { get; init; }

    public required string Title { get; init; }

    public required string DetailLine { get; init; }

    public bool IsSelected { get; init; }

    public bool IsEnabled { get; init; }

    public string SummaryLine
    {
        get
        {
            var body = string.IsNullOrWhiteSpace(DetailLine)
                ? Title
                : $"{Title} — {DetailLine}";
            return IsSelected ? $"[ {body} ]" : body;
        }
    }
}
