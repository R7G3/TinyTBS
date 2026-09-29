using TinyTBS.Engine.IO;
using TinyTBS.Game.Editor.Levels;

namespace TinyTBS.Game.Editor.Writers;

/// <summary>Writes a minimal skirmish level pointing at a map (delegates to <see cref="LevelDocumentWriter"/>).</summary>
public sealed class LevelStubWriter
{
    private readonly LevelDocumentWriter _levelWriter;

    public LevelStubWriter(IFileContentProvider files)
    {
        _levelWriter = new LevelDocumentWriter(files);
    }

    public string WriteSkirmishStub(
        string scenarioModuleRoot,
        string levelId,
        string title,
        string mapId,
        int playersMin = 2,
        int playersMax = 2)
    {
        var document = EditableLevelDocument.CreateDefault(levelId, title, mapId);
        document.PlayersMin = playersMin;
        document.PlayersMax = Math.Max(playersMin, playersMax);
        document.DefaultSlots = document.PlayersMax;
        document.Modes = ["skirmish"];
        return _levelWriter.Write(scenarioModuleRoot, document);
    }
}
