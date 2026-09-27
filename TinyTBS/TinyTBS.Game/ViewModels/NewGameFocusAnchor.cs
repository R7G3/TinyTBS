namespace TinyTBS.Game.ViewModels;

/// <summary>Where to place focus after a New Game UI rebuild.</summary>
public enum NewGameFocusAnchor
{
    Auto,
    SelectedMode,
    SelectedScenario,
    SelectedLevel,
    SelectedComposition,
    AddPlayer,
    AddPlayerTypeLocal,
    CancelAddPlayerType,
    RemovePlayer,
    GoldDecrease,
    GoldIncrease,
    UnitCapDecrease,
    UnitCapIncrease,
    Start,
}
