using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Saves;

/// <summary>Unified newest-first listing of match and campaign saves, and how to resume or delete each.</summary>
public sealed class SaveCatalog
{
    private readonly MatchSaveLibrary _matchLibrary;
    private readonly CampaignProgressStore _campaignStore;
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public SaveCatalog(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
        _matchLibrary = new MatchSaveLibrary(files, userDataPaths);
        _campaignStore = new CampaignProgressStore(files, userDataPaths);
    }

    public IReadOnlyList<SaveCatalogEntry> ListNewestFirst()
    {
        var entries = new List<SaveCatalogEntry>();

        foreach (var match in _matchLibrary.ListMatchSavesNewestFirst())
            entries.Add(SaveCatalogEntry.FromMatch(_files, match));

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

    /// <summary>
    /// The request that resumes <paramref name="entry"/>: the saved match state, or the current chapter
    /// of a campaign progress file.
    /// </summary>
    public MatchStartRequest CreateResumeRequest(SaveCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsCampaign)
            return CampaignRunRestorer.CreateChapterStartRequest(_campaignStore.ReadFile(entry.FilePath));

        return MatchSaveResume.CreateRequest(_matchLibrary.Load(entry.FilePath), _files, _userDataPaths);
    }

    public void Delete(SaveCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.IsCampaign)
            _campaignStore.Delete(entry.FilePath);
        else
            _matchLibrary.Delete(entry.FilePath);
    }
}
