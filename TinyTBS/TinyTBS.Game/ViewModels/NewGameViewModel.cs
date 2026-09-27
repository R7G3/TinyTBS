namespace TinyTBS.Game.ViewModels;

/// <summary>UI-state for the New Game tabbed flow.</summary>
public sealed class NewGameViewModel
{
    public string Title { get; set; } = "New Game";

    public string StatusText { get; set; } = string.Empty;

    public NewGameTab ActiveTab { get; set; } = NewGameTab.Mode;

    public NewGamePlayMode? SelectedMode { get; set; }

    public IReadOnlyList<NewGameModeRowViewModel> Modes { get; set; } = [];

    public IReadOnlyList<NewGameScenarioRowViewModel> Scenarios { get; set; } = [];

    public IReadOnlyList<NewGameLevelRowViewModel> Levels { get; set; } = [];

    public IReadOnlyList<NewGameCompositionOptionViewModel> CompositionOptions { get; set; } = [];

    public IReadOnlyList<NewGamePlayerSlotViewModel> PlayerSlots { get; set; } = [];

    public string? SelectedScenarioModuleId { get; set; }

    public string? SelectedLevelId { get; set; }

    /// <summary><see cref="NewGameCompositionSources.ScenarioDefaults"/> or a bundle id.</summary>
    public string SelectedCompositionSourceId { get; set; } = NewGameCompositionSources.ScenarioDefaults;

    public string CompositionSummary { get; set; } = string.Empty;

    public string LobbyNote { get; set; } = string.Empty;

    /// <summary>Empty-state / hint text when the active tab has no rows yet.</summary>
    public string TabEmptyHint { get; set; } = string.Empty;

    /// <summary>True when Mode is Skirmish (lobby steppers and editable slots).</summary>
    public bool ShowSkirmishLobby { get; set; }

    /// <summary>True while the Add-player type chooser (Local / Bot / Remote) is open.</summary>
    public bool ShowAddPlayerTypeChooser { get; set; }

    public int StartingGold { get; set; }

    public int UnitCap { get; set; }

    public int PlayersMin { get; set; } = 2;

    public int PlayersMax { get; set; } = 2;

    public bool CanAddPlayer => PlayerSlots.Count < PlayersMax;

    public bool CanRemovePlayer => PlayerSlots.Count > PlayersMin;

    public bool CanStart =>
        SelectedMode is not null
        && !string.IsNullOrWhiteSpace(SelectedScenarioModuleId)
        && !string.IsNullOrWhiteSpace(SelectedLevelId)
        && PlayerSlots.Count >= PlayersMin
        && PlayerSlots.Count <= PlayersMax
        && StartingGold >= 0
        && UnitCap >= 1;
}

