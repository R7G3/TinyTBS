using TinyTBS.Engine.IO;
using TinyTBS.Rules.Saves.Models;

namespace TinyTBS.Game.Saves;

/// <summary>Lists and resolves match saves in <see cref="IUserDataPaths.Saves"/>.</summary>
public sealed class MatchSaveLibrary
{
    private readonly IFileSystem _files;
    private readonly IUserDataPaths _userDataPaths;

    public MatchSaveLibrary(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _userDataPaths = userDataPaths ?? throw new ArgumentNullException(nameof(userDataPaths));
    }

    /// <summary>Match save files, newest first (by writtenAtUtc, then file name).</summary>
    public IReadOnlyList<MatchSaveListEntry> ListMatchSavesNewestFirst()
    {
        _userDataPaths.EnsureCreated();
        if (!_files.DirectoryExists(_userDataPaths.Saves))
            return [];

        var entries = new List<MatchSaveListEntry>();
        foreach (var path in _files.EnumerateFiles(_userDataPaths.Saves, "match_*.json"))
        {
            try
            {
                var document = MatchSaveReader.ReadFile(_files, path);
                entries.Add(new MatchSaveListEntry
                {
                    FilePath = path,
                    WrittenAtUtc = document.WrittenAtUtc,
                    LevelId = document.LevelId,
                    ScenarioModuleId = document.ContentSetup.ScenarioModuleId,
                });
            }
            catch (MatchSaveException)
            {
                // Skip corrupt / non-match files for listing.
            }
        }

        return entries
            .OrderByDescending(entry => entry.WrittenAtUtc)
            .ThenByDescending(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetLatest(out MatchSaveListEntry entry)
    {
        var list = ListMatchSavesNewestFirst();
        if (list.Count == 0)
        {
            entry = null!;
            return false;
        }

        entry = list[0];
        return true;
    }

    public MatchSaveDocument LoadLatest()
    {
        if (!TryGetLatest(out var entry))
            throw new MatchSaveException("No match saves found.");

        return MatchSaveReader.ReadFile(_files, entry.FilePath);
    }

    public MatchSaveDocument Load(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return MatchSaveReader.ReadFile(_files, filePath);
    }

    /// <summary>Deletes a match save file. Returns false if the file was already gone.</summary>
    public bool Delete(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullPath = Path.GetFullPath(filePath);
        var savesRoot = Path.GetFullPath(_userDataPaths.Saves);
        if (!fullPath.StartsWith(savesRoot, StringComparison.OrdinalIgnoreCase))
            throw new MatchSaveException("Refusing to delete a file outside the Saves folder.");

        if (!_files.Exists(fullPath))
            return false;

        try
        {
            _files.DeleteFile(fullPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new MatchSaveException($"Failed to delete save '{fullPath}'.", exception);
        }
    }
}
