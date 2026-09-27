using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Level tab list body.</summary>
internal static class NewGameLevelTabBody
{
    public static void Populate(NewGameListBuilder list, NewGameViewModel viewModel, Action<string> onSelectLevel)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onSelectLevel);

        if (viewModel.SelectedMode is null
            || string.IsNullOrWhiteSpace(viewModel.SelectedScenarioModuleId)
            || viewModel.Levels.Count == 0)
        {
            list.AddHint(viewModel.TabEmptyHint);
            return;
        }

        foreach (var level in viewModel.Levels)
        {
            var levelId = level.LevelId;
            list.AddRow(level.SummaryLine, () => onSelectLevel(levelId));
        }
    }
}
