using Microsoft.Xna.Framework;
using TinyTBS.Engine.Rendering;

namespace TinyTBS.Game.Presentation.Shared;

/// <summary>
/// Board cursor and action-tint styles. Match highlights and the map editor both read this palette;
/// <see cref="CursorHighlightRenderer"/> only paints the styles it is given.
/// </summary>
public static class CursorPalette
{
    private static readonly Color CursorFill = Color.White * 0.18f;

    public static readonly CellHighlightStyle Idle = new(
        CursorFill,
        new Color(255, 220, 80),
        borderThickness: 4);

    public static readonly CellHighlightStyle Selected = new(
        CursorFill,
        new Color(120, 255, 160),
        borderThickness: 4);

    public static readonly CellHighlightStyle Move = new(
        new Color(150, 175, 230),
        new Color(90, 150, 230, 180),
        borderThickness: 1);

    public static readonly CellHighlightStyle Attack = new(
        new Color(255, 140, 140),
        new Color(255, 80, 80, 210),
        borderThickness: 2);

    public static readonly CellHighlightStyle Capture = new(
        new Color(70, 255, 110),
        new Color(40, 255, 100, 255),
        borderThickness: 2);

    public static readonly CellHighlightStyle Repair = new(
        new Color(255, 200, 70),
        new Color(255, 210, 60, 255),
        borderThickness: 2);

    public static readonly CellHighlightStyle Raise = new(
        new Color(210, 150, 255),
        new Color(200, 120, 255, 210),
        borderThickness: 2);
}
