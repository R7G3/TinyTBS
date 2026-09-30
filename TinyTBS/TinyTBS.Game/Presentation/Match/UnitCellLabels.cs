using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Engine.Rendering;
using TinyTBS.Game.Presentation.Shared;
using TinyTBS.Rules.Match;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>
/// Asks <see cref="CellLabelRenderer"/> to paint level and hit points for each unit.
/// Level 0 is omitted. Placement and pixels stay in the renderer.
/// </summary>
public static class UnitCellLabels
{
    private static readonly CellLabelStyle LevelStyle = new(UiColors.MatchUnitLevel, UiColors.MatchUnitLabelOutline);

    private static readonly CellLabelStyle HitPointsStyle = new(UiColors.MatchUnitHitPoints, UiColors.MatchUnitLabelOutline);

    public static void Draw(
        CellLabelRenderer renderer,
        SpriteBatch spriteBatch,
        MatchBoardLayout layout,
        MatchState match,
        Func<MatchUnit, Vector2> visualTopLeft)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(visualTopLeft);
        ArgumentNullException.ThrowIfNull(match);

        foreach (var unit in match.Units)
        {
            var topLeft = visualTopLeft(unit);
            if (unit.Level > 0)
            {
                renderer.Draw(
                    spriteBatch,
                    layout,
                    topLeft,
                    unit.Level.ToString(),
                    CellLabelCorner.BottomLeft,
                    LevelStyle);
            }

            renderer.Draw(
                spriteBatch,
                layout,
                topLeft,
                unit.HitPoints.ToString(),
                CellLabelCorner.BottomRight,
                HitPointsStyle);
        }
    }
}
