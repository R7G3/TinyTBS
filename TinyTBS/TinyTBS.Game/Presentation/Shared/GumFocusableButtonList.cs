using Gum.Forms.Controls;
using TinyTBS.Game.Input;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>Result of <see cref="GumFocusableButtonList.HandleVerticalInput"/>.</summary>
public enum GumFocusListResult
{
    None,
    Navigated,
    Activated,
}

/// <summary>
/// Vertical (optionally L/R-as-vertical) focus over Gum buttons + Confirm activate.
/// Used by Main Menu, pause, detail overlays, and similar lists.
/// </summary>
public static class GumFocusableButtonList
{
    public static GumFocusListResult HandleVerticalInput(
        IGameCommandSource commands,
        IReadOnlyList<(Button Button, Action Activate)> entries,
        ref int focusIndex,
        MenuVerticalNavigateRepeat navigateRepeat,
        float elapsedSeconds,
        bool mapHorizontalToVertical = false)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(navigateRepeat);

        if (entries.Count == 0)
            return GumFocusListResult.None;

        var delta = navigateRepeat.TryGetDelta(commands, elapsedSeconds, mapHorizontalToVertical);
        if (delta != 0)
        {
            focusIndex = Math.Clamp(focusIndex + delta, 0, entries.Count - 1);
            ApplyFocus(entries, ref focusIndex);
            return GumFocusListResult.Navigated;
        }

        if (commands.WasPressed(GameCommand.Confirm))
        {
            focusIndex = Math.Clamp(focusIndex, 0, entries.Count - 1);
            entries[focusIndex].Activate();
            return GumFocusListResult.Activated;
        }

        MaintainFocus(entries, ref focusIndex);
        return GumFocusListResult.None;
    }

    /// <summary>Navigate only (no Confirm) over a bare button list — e.g. shop offers + Close.</summary>
    public static bool TryHandleVerticalNavigate(
        IGameCommandSource commands,
        int slotCount,
        ref int focusIndex,
        MenuVerticalNavigateRepeat navigateRepeat,
        float elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(navigateRepeat);
        if (slotCount <= 0)
            return false;

        var delta = navigateRepeat.TryGetDelta(commands, elapsedSeconds);
        if (delta == 0)
            return false;

        focusIndex = Math.Clamp(focusIndex + delta, 0, slotCount - 1);
        return true;
    }

    public static void ApplyFocus(
        IReadOnlyList<(Button Button, Action Activate)> entries,
        ref int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
            return;

        focusIndex = Math.Clamp(focusIndex, 0, entries.Count - 1);
        foreach (var (button, _) in entries)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        entries[focusIndex].Button.IsFocused = true;
    }

    public static void ApplyFocus(IReadOnlyList<Button> buttons, ref int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        if (buttons.Count == 0)
            return;

        focusIndex = Math.Clamp(focusIndex, 0, buttons.Count - 1);
        foreach (var button in buttons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }

        buttons[focusIndex].IsFocused = true;
    }

    public static void ClearFocus(IReadOnlyList<(Button Button, Action Activate)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (var (button, _) in entries)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }
    }

    public static void ClearFocus(IReadOnlyList<Button> buttons)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        foreach (var button in buttons)
        {
            if (button.IsFocused)
                button.IsFocused = false;
        }
    }

    public static void MaintainFocus(
        IReadOnlyList<(Button Button, Action Activate)> entries,
        ref int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
            return;

        if (SyncFocusIndexFromUi(entries, ref focusIndex)
            && IsFocusIntact(entries, focusIndex))
        {
            return;
        }

        ApplyFocus(entries, ref focusIndex);
    }

    public static void MaintainFocus(IReadOnlyList<Button> buttons, ref int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(buttons);
        if (buttons.Count == 0)
            return;

        for (var index = 0; index < buttons.Count; index++)
        {
            if (!buttons[index].IsFocused)
                continue;
            focusIndex = index;
            return;
        }

        ApplyFocus(buttons, ref focusIndex);
    }

    public static bool SyncFocusIndexFromUi(
        IReadOnlyList<(Button Button, Action Activate)> entries,
        ref int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(entries);
        for (var index = 0; index < entries.Count; index++)
        {
            if (!entries[index].Button.IsFocused)
                continue;
            focusIndex = index;
            return true;
        }

        return false;
    }

    public static bool IsFocusIntact(
        IReadOnlyList<(Button Button, Action Activate)> entries,
        int focusIndex)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (focusIndex < 0 || focusIndex >= entries.Count)
            return false;
        return entries[focusIndex].Button.IsFocused;
    }
}
