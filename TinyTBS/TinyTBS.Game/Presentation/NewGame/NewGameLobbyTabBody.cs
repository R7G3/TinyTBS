using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Lobby tab list body (session, skirmish players, gold / unit-cap steppers).</summary>
internal static class NewGameLobbyTabBody
{
    public static NewGameLobbyTabBodyResult Populate(
        NewGameListBuilder list,
        NewGameViewModel viewModel,
        Action<NewGameLobbySessionMode> onSelectLobbySession,
        Action onAddPlayer,
        Action onRemovePlayer,
        Action onDecreaseGold,
        Action onIncreaseGold,
        Action onDecreaseUnitCap,
        Action onIncreaseUnitCap)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onSelectLobbySession);
        ArgumentNullException.ThrowIfNull(onAddPlayer);
        ArgumentNullException.ThrowIfNull(onRemovePlayer);
        ArgumentNullException.ThrowIfNull(onDecreaseGold);
        ArgumentNullException.ThrowIfNull(onIncreaseGold);
        ArgumentNullException.ThrowIfNull(onDecreaseUnitCap);
        ArgumentNullException.ThrowIfNull(onIncreaseUnitCap);

        if (string.IsNullOrWhiteSpace(viewModel.SelectedLevelId))
        {
            list.AddHint(viewModel.TabEmptyHint);
            return new NewGameLobbyTabBodyResult();
        }

        var lobbySessionFocusIndex = -1;
        var addPlayerFocusIndex = -1;
        var removePlayerFocusIndex = -1;

        list.AddHint("Session");
        foreach (var option in viewModel.LobbySessionOptions)
        {
            if (option.IsEnabled)
            {
                var mode = option.Mode;
                list.AddRow(option.SummaryLine, () => onSelectLobbySession(mode));
                if (option.IsSelected)
                    lobbySessionFocusIndex = list.FocusableCount - 1;
            }
            else
            {
                list.AddDisabledRow(option.SummaryLine);
            }
        }

        if (!string.IsNullOrWhiteSpace(viewModel.LobbyNote))
            list.AddHint(viewModel.LobbyNote);

        if (!viewModel.ShowSkirmishLobby)
        {
            return new NewGameLobbyTabBodyResult
            {
                LobbySessionFocusIndex = lobbySessionFocusIndex,
            };
        }

        list.AddHint("Players");
        foreach (var slot in viewModel.PlayerSlots)
            list.AddHint(slot.SummaryLine);

        if (viewModel.CanAddPlayer)
        {
            list.AddRow("Add player", onAddPlayer);
            addPlayerFocusIndex = list.FocusableCount - 1;
        }
        else
        {
            list.AddDisabledRow("Add player");
        }

        if (viewModel.CanRemovePlayer)
        {
            list.AddRow("Remove player", onRemovePlayer);
            removePlayerFocusIndex = list.FocusableCount - 1;
        }
        else
        {
            list.AddDisabledRow("Remove player");
        }

        list.AddDisabledRow("Invite (soon)");
        list.AddDisabledRow("Bot (soon)");

        var goldStepper = list.AddValueStepper(
            "Gold",
            viewModel.StartingGold,
            onDecreaseGold,
            onIncreaseGold);
        var unitCapStepper = list.AddValueStepper(
            "Unit cap",
            viewModel.UnitCap,
            onDecreaseUnitCap,
            onIncreaseUnitCap);

        return new NewGameLobbyTabBodyResult
        {
            LobbySessionFocusIndex = lobbySessionFocusIndex,
            AddPlayerFocusIndex = addPlayerFocusIndex,
            RemovePlayerFocusIndex = removePlayerFocusIndex,
            GoldStepper = goldStepper,
            UnitCapStepper = unitCapStepper,
        };
    }
}
