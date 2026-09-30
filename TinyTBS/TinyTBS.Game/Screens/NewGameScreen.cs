using Microsoft.Xna.Framework;
using TinyTBS.Engine.Diagnostics;
using TinyTBS.Game.Flow;
using TinyTBS.Game.Input;
using TinyTBS.Game.Presentation.NewGame;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Game.ViewModels;
using TinyTBS.Rules.Ai;

namespace TinyTBS.Game.Screens;

/// <summary>
/// New Game tabs: Mode → Scenario → Level → Composition → Lobby.
/// Selection, unlock and the start request live on <see cref="NewGameDraft"/>.
/// </summary>
public sealed class NewGameScreen : MenuScreen
{
    private readonly NewGameViewModel _viewModel = new();
    private readonly NewGameView _view = new();
    private readonly NewGameDraft _draft;

    private NewGameFocusAnchor _focusAnchor = NewGameFocusAnchor.Auto;

    public NewGameScreen(GameMain game)
        : base(game)
    {
        _draft = new NewGameDraft(App.NewGame, App.Campaigns);
    }

    protected override void OnLoad()
    {
        TinyGame.UserDataPaths.EnsureCreated();
        _viewModel.ActiveTab = NewGameTab.Mode;
        Refresh("Pick Campaign or Skirmish, then scenario and level.");
    }

    protected override void OnUnload() => _view.Clear();

    protected override void OnUpdate(GameTime gameTime, float elapsedSeconds)
    {
        _view.ApplyResponsiveLayout();
        _view.HandleInput(TinyGame.Commands, elapsedSeconds, TinyGame.Pointer);
        _view.HandlePointerScroll(TinyGame.Pointer.ScrollWheelDelta);

        if (_viewModel.ShowAddPlayerTypeChooser
            && (TinyGame.Commands.WasPressed(GameCommand.Back)
                || TinyGame.Commands.WasPressed(GameCommand.Cancel)))
        {
            CancelAddPlayerChooser();
            return;
        }

        if (WasLeavePressed())
            GoToMainMenu();
    }

    private void Refresh(string statusText)
    {
        _draft.Project(_viewModel, statusText);

        var focusAnchor = _focusAnchor;
        _focusAnchor = NewGameFocusAnchor.Auto;

        _view.Build(
            _viewModel,
            onSelectTab: SelectTab,
            onSelectMode: SelectMode,
            onSelectScenario: SelectScenario,
            onSelectLevel: SelectLevel,
            onSelectComposition: SelectComposition,
            onOpenAddPlayerChooser: OpenAddPlayerChooser,
            onAddLocalPlayer: AddLocalPlayer,
            onAddBotPlayer: AddBotPlayer,
            onCancelAddPlayerChooser: CancelAddPlayerChooser,
            onRemovePlayerAt: RemovePlayerAt,
            onActivatePlayerSlot: ActivatePlayerSlot,
            onDecreaseGold: () => AdjustGold(-_draft.GoldStepAmount),
            onIncreaseGold: () => AdjustGold(_draft.GoldStepAmount),
            onDecreaseUnitCap: () => AdjustUnitCap(-_draft.UnitCapStepAmount),
            onIncreaseUnitCap: () => AdjustUnitCap(_draft.UnitCapStepAmount),
            onStart: StartMatch,
            onBack: GoToMainMenu,
            focusAnchor: focusAnchor);
    }

    private void SelectTab(NewGameTab tab)
    {
        _viewModel.ActiveTab = tab;
        if (tab != NewGameTab.Lobby)
            _draft.CancelAddPlayerChooser();

        _focusAnchor = tab == NewGameTab.Lobby
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.Auto;
        Refresh(StatusForTab(tab));
    }

    private static string StatusForTab(NewGameTab tab) =>
        tab switch
        {
            NewGameTab.Mode => "Pick Campaign or Skirmish.",
            NewGameTab.Scenario => "Pick a scenario module.",
            NewGameTab.Level => "Pick a level.",
            NewGameTab.Composition => "Choose scenario defaults or a bundle preset.",
            NewGameTab.Lobby => "Review lobby settings, then Start.",
            _ => "New Game",
        };

    private void SelectMode(NewGamePlayMode mode)
    {
        _draft.SelectMode(mode);
        _viewModel.ActiveTab = NewGameTab.Scenario;
        _focusAnchor = NewGameFocusAnchor.SelectedScenario;
        Refresh(mode == NewGamePlayMode.Campaign
            ? "Campaign selected. Pick a scenario."
            : "Skirmish selected. Pick a scenario.");
    }

    private void SelectScenario(string moduleId)
    {
        _draft.SelectScenario(moduleId);
        _viewModel.ActiveTab = NewGameTab.Level;
        _focusAnchor = NewGameFocusAnchor.SelectedLevel;
        Refresh("Scenario selected. Pick a level.");
    }

    private void SelectLevel(string levelId)
    {
        if (!_draft.TrySelectLevel(levelId))
        {
            Refresh("That chapter is locked.");
            return;
        }

        _viewModel.ActiveTab = NewGameTab.Composition;
        _focusAnchor = NewGameFocusAnchor.SelectedComposition;
        Refresh("Level selected. Choose composition, then lobby or Start.");
    }

    private void SelectComposition(string sourceId)
    {
        _draft.SelectComposition(sourceId);
        _viewModel.ActiveTab = NewGameTab.Lobby;
        _focusAnchor = NewGameFocusAnchor.RemovePlayer;
        Refresh("Composition updated. Review lobby, then Start.");
    }

    private void OpenAddPlayerChooser()
    {
        if (!_draft.TryOpenAddPlayerChooser())
            return;

        _focusAnchor = NewGameFocusAnchor.AddPlayerTypeLocal;
        Refresh("Choose player type.");
    }

    private void AddLocalPlayer()
    {
        if (!_draft.TryAddLocalPlayer())
            return;

        _focusAnchor = SeatFocusAfterEdit();
        Refresh("Local player added.");
    }

    private void AddBotPlayer(BotDifficulty difficulty)
    {
        if (!_draft.TryAddBot(difficulty))
            return;

        _focusAnchor = SeatFocusAfterEdit();
        Refresh($"Bot ({difficulty}) added.");
    }

    private void CancelAddPlayerChooser()
    {
        if (!_viewModel.ShowAddPlayerTypeChooser)
            return;

        _draft.CancelAddPlayerChooser();
        _focusAnchor = SeatFocusAfterEdit();
        Refresh("Add player cancelled.");
    }

    private void RemovePlayerAt(int slotIndex)
    {
        if (!_draft.TryRemovePlayer(slotIndex, out var belowMinimum))
            return;

        if (belowMinimum)
        {
            _focusAnchor = NewGameFocusAnchor.AddPlayerTypeLocal;
            Refresh("Player removed — add Local or Bot to reach the minimum.");
            return;
        }

        _focusAnchor = SeatFocusAfterEdit();
        Refresh("Player removed.");
    }

    private void ActivatePlayerSlot(int slotIndex)
    {
        if (!_draft.CanPressSlot(slotIndex))
            return;

        _focusAnchor = NewGameFocusAnchor.RemovePlayer;
        if (!_draft.IsBotSeat(slotIndex))
        {
            Refresh("Local seat — remove (X) then + Bot to change type.");
            return;
        }

        var difficulty = _draft.CycleBot(slotIndex);
        Refresh($"Player {slotIndex + 1} → bot {PlayerDisplayNames.FormatBotDifficulty(difficulty)}.");
    }

    private NewGameFocusAnchor SeatFocusAfterEdit() =>
        _draft.SeatCount > _draft.PlayersMin
            ? NewGameFocusAnchor.RemovePlayer
            : NewGameFocusAnchor.AddPlayer;

    private void AdjustGold(int delta)
    {
        if (!_draft.TryAdjustGold(delta))
            return;

        _viewModel.StartingGold = _draft.StartingGold;
        _view.SetStatus($"Starting gold: {_draft.StartingGold}.");
        _view.SyncLobbyValues(_draft.StartingGold, _draft.UnitCap);
    }

    private void AdjustUnitCap(int delta)
    {
        if (!_draft.TryAdjustUnitCap(delta))
            return;

        _viewModel.UnitCap = _draft.UnitCap;
        _view.SetStatus($"Unit cap: {_draft.UnitCap}.");
        _view.SyncLobbyValues(_draft.StartingGold, _draft.UnitCap);
    }

    private void StartMatch()
    {
        if (!_draft.CanStart)
        {
            _view.SetStatus("Pick mode, scenario, and level before Start.");
            return;
        }

        if (_draft.IsSelectedLevelLocked)
        {
            _view.SetStatus("That chapter is locked.");
            return;
        }

        try
        {
            Navigator.StartMatch(_draft.CreateStartRequest());
        }
        catch (Exception exception)
        {
            GameLog.Error("Starting a new match failed.", exception);
            _view.SetStatus("Cannot start: " + exception.Message);
        }
    }

    private void GoToMainMenu() =>
        Navigator.ToMainMenu();
}
