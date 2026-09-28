using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>
/// Cell corner labels for units: level (bottom-left, L0 hidden) and current HP (bottom-right).
/// </summary>
public sealed class UnitLevelLabelRenderer
{
    private static readonly Color LevelFill = new(255, 240, 200);
    private static readonly Color HitPointsFill = new(230, 250, 255);
    private static readonly Color Outline = new(20, 22, 28, 220);

    private readonly SpriteFont _font;

    public UnitLevelLabelRenderer(SpriteFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _font = font;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        MatchState match,
        Func<MatchUnit, Vector2>? resolveVisualTopLeft = null)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(match);

        var inset = Math.Max(2f, layout.TileSize * 0.06f);
        var scale = Math.Clamp(layout.TileSize / 64f, 0.55f, 1.35f);

        foreach (var unit in match.Units)
        {
            Vector2 topLeft;
            if (resolveVisualTopLeft is not null)
                topLeft = resolveVisualTopLeft(unit);
            else
                topLeft = layout.Origin + new Vector2(unit.Cell.X * layout.TileSize, unit.Cell.Y * layout.TileSize);

            var bottomLeft = topLeft + new Vector2(0f, layout.TileSize);
            var bottomRight = topLeft + new Vector2(layout.TileSize, layout.TileSize);

            if (unit.Level > 0)
            {
                var levelText = unit.Level.ToString();
                var levelSize = _font.MeasureString(levelText) * scale;
                DrawOutlined(
                    spriteBatch,
                    levelText,
                    new Vector2(bottomLeft.X + inset, bottomLeft.Y - inset - levelSize.Y),
                    scale,
                    LevelFill);
            }

            var hitPointsText = unit.HitPoints.ToString();
            var hitPointsSize = _font.MeasureString(hitPointsText) * scale;
            DrawOutlined(
                spriteBatch,
                hitPointsText,
                new Vector2(bottomRight.X - inset - hitPointsSize.X, bottomRight.Y - inset - hitPointsSize.Y),
                scale,
                HitPointsFill);
        }
    }

    private void DrawOutlined(
        SpriteBatch spriteBatch,
        string text,
        Vector2 position,
        float scale,
        Color fill)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                spriteBatch.DrawString(
                    _font,
                    text,
                    position + new Vector2(dx, dy),
                    Outline,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    layerDepth: 0f);
            }
        }

        spriteBatch.DrawString(
            _font,
            text,
            position,
            fill,
            rotation: 0f,
            origin: Vector2.Zero,
            scale,
            SpriteEffects.None,
            layerDepth: 0f);
    }
}
