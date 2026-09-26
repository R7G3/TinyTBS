using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Modules.Models;

/// <summary>One <c>replaces</c> entry from a scenario module.</summary>
public sealed class ContentIdReplace
{
    public required ContentId From { get; init; }

    public required ContentId To { get; init; }
}
