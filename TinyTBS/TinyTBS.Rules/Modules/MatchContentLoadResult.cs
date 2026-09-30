using TinyTBS.Rules.Match;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Rules.Modules;

/// <summary>Resolved match content: scenario folder, catalog, and replace table.</summary>
public sealed class MatchContentLoadResult
{
    public required MatchContentComposition Composition { get; init; }

    public required ScenarioModuleDefinition Scenario { get; init; }

    public required MatchContentCatalog Catalog { get; init; }

    public required ContentIdReplaceTable Replaces { get; init; }
}
