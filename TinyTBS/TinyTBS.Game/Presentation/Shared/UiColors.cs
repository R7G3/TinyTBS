using Microsoft.Xna.Framework;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>
/// Shared UI palette. Field names are prefixed by surface:
/// <c>Menu*</c> (Content / New Game / About / Editor shells), <c>Editor*</c>, <c>Match*</c>.
/// </summary>
public static class UiColors
{
    /// <summary>Clear color behind the animated menu background (menus, loading, editor forms).</summary>
    public static readonly Color MenuBackground = new(24, 28, 38);

    /// <summary>Standard dark panel (Content, New Game, About, Editor, match pause/detail).</summary>
    public static readonly Color MenuPanel = new(16, 18, 28, 235);

    /// <summary>Scroll / list well behind library rows (Content, About, Editor hub).</summary>
    public static readonly Color MenuListWell = new(12, 14, 22, 200);

    /// <summary>Dim scrim behind modal detail / choice cards.</summary>
    public static readonly Color MenuDetailScrim = new(8, 10, 16, 180);

    /// <summary>Editor shell panel (same fill as <see cref="MenuPanel"/>).</summary>
    public static readonly Color EditorPanel = MenuPanel;

    /// <summary>Editor hub list well.</summary>
    public static readonly Color EditorListWell = MenuListWell;

    /// <summary>Editable TextBox fill — white so fields read clearly as inputs.</summary>
    public static readonly Color EditorTextField = Color.White;

    /// <summary>Text on <see cref="EditorTextField"/>.</summary>
    public static readonly Color EditorTextFieldForeground = new(20, 24, 32);

    public static readonly Color MatchOverlayDark = MenuPanel;

    public static readonly Color MatchOverlayShop = new(18, 22, 34, 235);

    public static readonly Color MatchBottomBar = new(16, 18, 28, 220);

    public static readonly Color MatchCompactInfo = new(20, 24, 36, 200);

    public static readonly Color MatchIconSlot = new(40, 44, 56, 255);

    public static readonly Color MatchSceneClear = new(18, 20, 28);
}
