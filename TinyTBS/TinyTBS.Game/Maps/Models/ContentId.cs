namespace TinyTBS.Game.Maps.Models;

/// <summary>Logical content id: <c>{namespace}/{localId}</c>.</summary>
public readonly record struct ContentId(string Namespace, string LocalId)
{
    public string Full => $"{Namespace}/{LocalId}";

    public static bool TryParse(string? value, out ContentId id)
    {
        id = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var slashIndex = value.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= value.Length - 1)
            return false;

        var contentNamespace = value[..slashIndex].Trim();
        var localId = value[(slashIndex + 1)..].Trim();
        if (contentNamespace.Length == 0 || localId.Length == 0)
            return false;

        id = new ContentId(contentNamespace, localId);
        return true;
    }

    public static ContentId Parse(string value)
    {
        if (!TryParse(value, out var contentId))
            throw new MapLoadException($"Invalid content id '{value}'. Expected namespace/localId.");
        return contentId;
    }

    public override string ToString() => Full;
}
