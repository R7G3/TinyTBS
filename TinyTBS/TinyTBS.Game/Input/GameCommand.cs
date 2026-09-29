namespace TinyTBS.Game.Input;

/// <summary>
/// Logical game actions independent of physical devices.
/// </summary>
public enum GameCommand
{
    Confirm,
    Cancel,
    Back,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    Pause,
    Info,
    /// <summary>Finish the selected unit's activation without attack/capture (face north / Y).</summary>
    Wait,
    EndTurn,
    ZoomIn,
    ZoomOut,
    /// <summary>Cycle UI focus region backward (LB / Q) — Editor paint panels.</summary>
    FocusPreviousRegion,
    /// <summary>Cycle UI focus region forward (RB / E) — Editor paint panels.</summary>
    FocusNextRegion,
    /// <summary>Editor undo (Ctrl+Z).</summary>
    Undo,
    /// <summary>Editor redo (Ctrl+Y).</summary>
    Redo,
}
