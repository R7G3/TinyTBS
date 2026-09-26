using TinyTBS.Game.Match;
using TinyTBS.Game.Modules.Models;

namespace TinyTBS.Game.Modules;

/// <summary>Resolved match content: scenario folder, catalog, and replace table.</summary>
public sealed class MatchContentLoadResult
{
    public required MatchContentComposition Composition { get; init; }

    public required ScenarioModuleDefinition Scenario { get; init; }

    public required MatchContentCatalog Catalog { get; init; }

    public required ContentIdReplaceTable Replaces { get; init; }
}
