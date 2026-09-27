using Gum.DataTypes;
using Gum.Forms.Controls;
using TinyTBS.Engine.GumLayout;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>
/// Tab bar and bottom action-bar button registration for New Game / Content shells
/// (not a shared mega-View — each screen still owns layout and handlers).
/// </summary>
public static class GumMenuChrome
{
    public static Panel CreateButtonBarHost(Panel parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var host = new Panel();
        GumUiLayout.FillParentWidth(host);
        host.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.AddChild(host);
        return host;
    }

    public static void AddTabButton(
        ICollection<Button> tabButtons,
        List<(Button Button, Action Activate)> focusableEntries,
        string text,
        bool isActive,
        Action onActivate)
    {
        ArgumentNullException.ThrowIfNull(tabButtons);
        ArgumentNullException.ThrowIfNull(focusableEntries);
        ArgumentNullException.ThrowIfNull(onActivate);

        var button = new Button
        {
            Text = isActive ? $"[{text}]" : text,
            IsEnabled = true,
        };
        button.Click += (_, _) => onActivate();
        tabButtons.Add(button);
        focusableEntries.Add((button, onActivate));
    }

    public static void AddActionButton(
        ICollection<Button> actionButtons,
        List<(Button Button, Action Activate)> focusableEntries,
        string text,
        bool isEnabled,
        Action onActivate,
        Action<int>? onRegistered = null)
    {
        ArgumentNullException.ThrowIfNull(actionButtons);
        ArgumentNullException.ThrowIfNull(focusableEntries);
        ArgumentNullException.ThrowIfNull(onActivate);

        var button = new Button { Text = text, IsEnabled = isEnabled };
        if (isEnabled)
        {
            button.Click += (_, _) => onActivate();
            focusableEntries.Add((button, onActivate));
            onRegistered?.Invoke(focusableEntries.Count - 1);
        }
        else
        {
            button.Visual.HasEvents = false;
        }

        actionButtons.Add(button);
    }

    public static bool IsFocusIndexInRange(int focusIndex, int rangeStart, int rangeCount) =>
        rangeCount > 0
        && rangeStart >= 0
        && focusIndex >= rangeStart
        && focusIndex < rangeStart + rangeCount;

    public static void MoveFocusInRange(
        ref int focusIndex,
        int rangeStart,
        int rangeCount,
        int delta)
    {
        if (rangeCount <= 0 || rangeStart < 0)
            return;

        var localIndex = focusIndex - rangeStart;
        localIndex = Math.Clamp(localIndex + delta, 0, rangeCount - 1);
        focusIndex = rangeStart + localIndex;
    }
}
