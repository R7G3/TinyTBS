using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Mode tab list body.</summary>
internal static class NewGameModeTabBody
{
    public static void Populate(NewGameListBuilder list, NewGameViewModel viewModel, Action<NewGamePlayMode> onSelectMode)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onSelectMode);

        if (viewModel.Modes.Count == 0)
        {
            list.AddHint(viewModel.TabEmptyHint);
            return;
        }

        foreach (var mode in viewModel.Modes)
        {
            var playMode = mode.Mode;
            list.AddRow(mode.SummaryLine, () => onSelectMode(playMode));
        }
    }
}
