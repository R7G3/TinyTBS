using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Composition tab list body.</summary>
internal static class NewGameCompositionTabBody
{
    public static void Populate(
        NewGameListBuilder list,
        NewGameViewModel viewModel,
        Action<string> onSelectComposition)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onSelectComposition);

        if (string.IsNullOrWhiteSpace(viewModel.SelectedScenarioModuleId)
            || viewModel.CompositionOptions.Count == 0)
        {
            list.AddHint(viewModel.TabEmptyHint);
            return;
        }

        foreach (var option in viewModel.CompositionOptions)
        {
            var sourceId = option.SourceId;
            list.AddRow(option.SummaryLine, () => onSelectComposition(sourceId));
        }

        if (!string.IsNullOrWhiteSpace(viewModel.CompositionSummary))
            list.AddHint(viewModel.CompositionSummary);
    }
}
