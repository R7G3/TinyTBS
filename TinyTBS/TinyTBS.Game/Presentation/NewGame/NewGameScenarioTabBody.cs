using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Scenario tab list body.</summary>
internal static class NewGameScenarioTabBody
{
    public static void Populate(NewGameListBuilder list, NewGameViewModel viewModel, Action<string> onSelectScenario)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onSelectScenario);

        if (viewModel.SelectedMode is null || viewModel.Scenarios.Count == 0)
        {
            list.AddHint(viewModel.TabEmptyHint);
            return;
        }

        foreach (var scenario in viewModel.Scenarios)
        {
            var moduleId = scenario.ModuleId;
            list.AddRow(scenario.SummaryLine, () => onSelectScenario(moduleId));
        }
    }
}
