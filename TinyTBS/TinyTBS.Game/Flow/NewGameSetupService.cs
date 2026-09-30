using TinyTBS.Engine.IO;
using TinyTBS.Game.Modules;
using TinyTBS.Game.Presentation.NewGame;
using TinyTBS.Game.ViewModels;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Flow;

/// <summary>Scenario catalog, bundle presets and composition for the New Game screen.</summary>
public sealed class NewGameSetupService
{
    private readonly NewGameScenarioCatalog _catalog;
    private readonly ContentBundleLibrary _bundles;

    public NewGameSetupService(IFileSystem files, IUserDataPaths userDataPaths)
    {
        _catalog = new NewGameScenarioCatalog(files, userDataPaths);
        _bundles = new ContentBundleLibrary(files, userDataPaths);
    }

    public IReadOnlyList<NewGameScenarioEntry> ListScenarios() => _catalog.ListScenarios();

    public ScenarioModuleDefinition LoadScenario(string moduleId) => _catalog.LoadScenario(moduleId);

    public IReadOnlyList<ContentBundleDefinition> ListBundles() => _bundles.ListEffectiveBundles();

    public static MatchContentComposition ResolveComposition(
        ScenarioModuleDefinition scenario,
        string? sourceId,
        IReadOnlyList<ContentBundleDefinition> bundles)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (string.IsNullOrWhiteSpace(sourceId) || sourceId == NewGameCompositionSources.ScenarioDefaults)
            return MatchContentComposition.FromScenarioDefaults(scenario);

        var bundle = bundles.FirstOrDefault(candidate => candidate.BundleId == sourceId);
        return bundle is null
            ? MatchContentComposition.FromScenarioDefaults(scenario)
            : MatchContentComposition.FromScenarioWithBundle(scenario, bundle);
    }
}
