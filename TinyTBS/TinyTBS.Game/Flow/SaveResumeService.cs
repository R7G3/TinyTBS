using TinyTBS.Engine.IO;
using TinyTBS.Game.Match.Session;
using TinyTBS.Game.Saves;

namespace TinyTBS.Game.Flow;

/// <summary>Lists saves and builds the request that continues one. Shared by the main menu and Load Game.</summary>
public sealed class SaveResumeService
{
    private readonly SaveCatalog _catalog;

    public SaveResumeService(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _catalog = new SaveCatalog(files, userDataPaths);
    }

    public IReadOnlyList<SaveCatalogEntry> ListNewestFirst() => _catalog.ListNewestFirst();

    public bool TryGetLatest(out SaveCatalogEntry entry) => _catalog.TryGetLatest(out entry);

    public MatchStartRequest CreateResumeRequest(SaveCatalogEntry entry) => _catalog.CreateResumeRequest(entry);

    public void Delete(SaveCatalogEntry entry) => _catalog.Delete(entry);
}
