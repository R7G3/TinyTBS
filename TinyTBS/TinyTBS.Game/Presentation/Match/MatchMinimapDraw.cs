using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Presentation.Match;

/// <summary>Draws the full-screen minimap overlay from live match state.</summary>
public static class MatchMinimapDraw
{
    public static void Draw(
        GameplaySession session,
        SpriteBatch spriteBatch,
        Viewport viewport)
    {
        var match = session.State;
        var size = Math.Min(viewport.Width, viewport.Height) / 2;
        var destination = new Rectangle(
            (viewport.Width - size) / 2,
            (viewport.Height - size) / 2,
            size,
            size);

        var buildings = match.Buildings.Select(building => (
            building.Cell.X,
            building.Cell.Y,
            PlayerPalette.ForOwner(building.OwnerPlayerIndex)));

        var units = match.Units.Select(unit => (
            unit.Cell.X,
            unit.Cell.Y,
            PlayerPalette.ForPlayer(unit.PlayerIndex)));

        session.Minimap.Draw(
            spriteBatch,
            destination,
            match.Width,
            match.Height,
            (x, y) => MatchMinimapColors.ForTerrain(match.GetTerrain(x, y)),
            buildings,
            units);
    }
}
