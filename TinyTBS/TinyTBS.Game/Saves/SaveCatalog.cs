using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;

namespace TinyTBS.Game.Saves;

/// <summary>Unified newest-first listing of match and campaign saves.</summary>
public sealed class SaveCatalog
{
    private readonly MatchSaveLibrary _matchLibrary;
    private readonly CampaignProgressStore _campaignStore;

    public SaveCatalog(IUserDataPaths userDataPaths)
    {
        ArgumentNullException.ThrowIfNull(userDataPaths);
        _matchLibrary = new MatchSaveLibrary(userDataPaths);
        _campaignStore = new CampaignProgressStore(userDataPaths);
    }

    public IReadOnlyList<SaveCatalogEntry> ListNewestFirst()
    {
        var entries = new List<SaveCatalogEntry>();

        foreach (var match in _matchLibrary.ListMatchSavesNewestFirst())
            entries.Add(SaveCatalogEntry.FromMatch(match));

        foreach (var campaign in _campaignStore.ListNewestFirst())
            entries.Add(SaveCatalogEntry.FromCampaign(campaign));

        return entries
            .OrderByDescending(entry => entry.WrittenAtUtc)
            .ThenByDescending(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetLatest(out SaveCatalogEntry entry)
    {
        var list = ListNewestFirst();
        if (list.Count == 0)
        {
            entry = null!;
            return false;
        }

        entry = list[0];
        return true;
    }

    public MatchSaveLibrary MatchLibrary => _matchLibrary;

    public CampaignProgressStore CampaignStore => _campaignStore;
}
