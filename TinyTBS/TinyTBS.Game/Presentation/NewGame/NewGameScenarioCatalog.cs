using TinyTBS.Engine.Diagnostics;
using TinyTBS.Engine.IO;
using TinyTBS.Game.Campaigns;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Reads scenario modules, their levels and campaigns from the content library for New Game.</summary>
public sealed class NewGameScenarioCatalog
{
    private readonly IFileSystem _files;
    private readonly ContentModuleLibrary _moduleLibrary;
    private readonly ContentModuleLocator _moduleLocator;

    public NewGameScenarioCatalog(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _moduleLibrary = new ContentModuleLibrary(files, userDataPaths);
        _moduleLocator = new ContentModuleLocator(files, userDataPaths);
    }

    /// <summary>All scenario modules by title; a module that fails to load is listed without levels.</summary>
    public IReadOnlyList<NewGameScenarioEntry> ListScenarios() =>
        _moduleLibrary.ListEffectiveModules()
            .Where(module => module.Type == ContentModuleType.Scenario)
            .OrderBy(module => module.Title, StringComparer.OrdinalIgnoreCase)
            .Select(LoadEntry)
            .ToArray();

    public ScenarioModuleDefinition LoadScenario(string moduleId) =>
        ScenarioModuleLoader.Load(_moduleLocator.ResolveModuleRoot(moduleId), _files);

    private NewGameScenarioEntry LoadEntry(ContentModuleInfo module)
    {
        try
        {
            var scenarioRoot = _moduleLocator.ResolveModuleRoot(module.ModuleId);
            var definition = ScenarioModuleLoader.Load(scenarioRoot, _files);
            return new NewGameScenarioEntry
            {
                Module = module,
                Levels = ScenarioLevelCatalog.ListLevels(scenarioRoot, _files).ToArray(),
                Campaign = CampaignLoader.TryLoadFromScenario(
                    scenarioRoot,
                    definition.CampaignManifestRelativePath,
                    _files),
            };
        }
        catch (Exception exception)
        {
            GameLog.Warning($"Scenario '{module.ModuleId}' levels could not be listed.", exception);
            return new NewGameScenarioEntry { Module = module, Levels = [] };
        }
    }
}
