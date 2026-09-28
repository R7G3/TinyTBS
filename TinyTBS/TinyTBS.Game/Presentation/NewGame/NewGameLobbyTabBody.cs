using TinyTBS.Game.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.NewGame;

/// <summary>Lobby tab list body (player slots with X, +, type chooser, gold / unit-cap).</summary>
internal static class NewGameLobbyTabBody
{
    public static NewGameLobbyTabBodyResult Populate(
        NewGameListBuilder list,
        NewGameViewModel viewModel,
        Action onOpenAddPlayerChooser,
        Action onAddLocalPlayer,
        Action<TinyTBS.Game.Match.Ai.BotDifficulty> onAddBotPlayer,
        Action onCancelAddPlayerChooser,
        Action<int> onRemovePlayerAt,
        Action onDecreaseGold,
        Action onIncreaseGold,
        Action onDecreaseUnitCap,
        Action onIncreaseUnitCap)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(onOpenAddPlayerChooser);
        ArgumentNullException.ThrowIfNull(onAddLocalPlayer);
        ArgumentNullException.ThrowIfNull(onAddBotPlayer);
        ArgumentNullException.ThrowIfNull(onCancelAddPlayerChooser);
        ArgumentNullException.ThrowIfNull(onRemovePlayerAt);
        ArgumentNullException.ThrowIfNull(onDecreaseGold);
        ArgumentNullException.ThrowIfNull(onIncreaseGold);
        ArgumentNullException.ThrowIfNull(onDecreaseUnitCap);
        ArgumentNullException.ThrowIfNull(onIncreaseUnitCap);

        if (string.IsNullOrWhiteSpace(viewModel.SelectedLevelId))
        {
            list.AddHint(viewModel.TabEmptyHint);
            return new NewGameLobbyTabBodyResult();
        }

        if (!string.IsNullOrWhiteSpace(viewModel.LobbyNote))
            list.AddHint(viewModel.LobbyNote);

        list.AddHint("Players");

        var firstRemoveFocusIndex = -1;
        var canRemove = viewModel.ShowSkirmishLobby && viewModel.CanRemovePlayer;
        foreach (var slot in viewModel.PlayerSlots)
        {
            var slotIndex = slot.SlotIndex;
            var removeFocus = list.AddPlayerSlotRow(
                slot.SummaryLine,
                PlayerPalette.ForPlayer(slot.PaletteIndex),
                showRemove: viewModel.ShowSkirmishLobby,
                canRemove: canRemove,
                onRemove: () => onRemovePlayerAt(slotIndex));
            if (firstRemoveFocusIndex < 0 && removeFocus >= 0)
                firstRemoveFocusIndex = removeFocus;
        }

        var addPlayerFocusIndex = -1;
        var addPlayerTypeLocalFocusIndex = -1;
        var cancelAddPlayerTypeFocusIndex = -1;

        if (!viewModel.ShowSkirmishLobby)
        {
            return new NewGameLobbyTabBodyResult
            {
                FirstRemovePlayerFocusIndex = firstRemoveFocusIndex,
            };
        }

        if (viewModel.ShowAddPlayerTypeChooser && viewModel.CanAddPlayer)
        {
            list.AddHint("Add as");
            list.AddRow("Local", onAddLocalPlayer);
            addPlayerTypeLocalFocusIndex = list.FocusableCount - 1;
            list.AddRow("Bot · Easy", () => onAddBotPlayer(TinyTBS.Game.Match.Ai.BotDifficulty.Easy));
            list.AddRow("Bot · Normal", () => onAddBotPlayer(TinyTBS.Game.Match.Ai.BotDifficulty.Normal));
            list.AddDisabledRow("Remote (soon)");
            list.AddRow("Cancel", onCancelAddPlayerChooser);
            cancelAddPlayerTypeFocusIndex = list.FocusableCount - 1;
        }
        else if (viewModel.CanAddPlayer)
        {
            list.AddRow("+", onOpenAddPlayerChooser);
            addPlayerFocusIndex = list.FocusableCount - 1;
        }
        else
        {
            list.AddDisabledRow("+");
        }

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
            AddPlayerFocusIndex = addPlayerFocusIndex,
            AddPlayerTypeLocalFocusIndex = addPlayerTypeLocalFocusIndex,
            CancelAddPlayerTypeFocusIndex = cancelAddPlayerTypeFocusIndex,
            FirstRemovePlayerFocusIndex = firstRemoveFocusIndex,
            GoldStepper = goldStepper,
            UnitCapStepper = unitCapStepper,
        };
    }
}

