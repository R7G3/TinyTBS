using TinyTBS.Game.Campaigns.Models;
using TinyTBS.Rules.Modules.Models;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>A scenario module offered by New Game, with its levels and optional campaign.</summary>
public sealed class NewGameScenarioEntry
{
    public required ContentModuleInfo Module { get; init; }

    public required IReadOnlyList<ScenarioLevelInfo> Levels { get; init; }

    public CampaignDefinition? Campaign { get; init; }
}
