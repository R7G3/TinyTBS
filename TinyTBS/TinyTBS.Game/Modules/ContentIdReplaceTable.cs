using TinyTBS.Game.Maps.Models;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Applies scenario <c>replaces</c> when resolving map type ids.</summary>
public sealed class ContentIdReplaceTable
{
    private readonly Dictionary<ContentId, ContentId> _map;

    public ContentIdReplaceTable(IReadOnlyList<ContentIdReplace> replaces)
    {
        _map = new Dictionary<ContentId, ContentId>();
        if (replaces is null)
            return;

        foreach (var entry in replaces)
            _map[entry.From] = entry.To;
    }

    public static ContentIdReplaceTable Empty { get; } = new([]);

    public ContentId Resolve(ContentId contentId) =>
        _map.TryGetValue(contentId, out var replacement) ? replacement : contentId;
}
