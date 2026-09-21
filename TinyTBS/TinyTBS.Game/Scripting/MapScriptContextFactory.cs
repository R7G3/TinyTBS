using TinyTBS.Game.Match;
using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>Builds <see cref="MapScriptContext"/> snapshots from match state.</summary>
public static class MapScriptContextFactory
{
    public static MapScriptContext Create(
        MatchState match,
        IMapScriptWorld world,
        MapScriptLastAction? lastAction = null)
    {
        var surface = new string[match.Width, match.Height];
        for (var y = 0; y < match.Height; y++)
        {
            for (var x = 0; x < match.Width; x++)
                surface[x, y] = match.GetTerrain(x, y).ToString().ToLowerInvariant();
        }

        var units = new List<MapScriptUnitView>(match.Units.Count);
        foreach (var unit in match.Units)
        {
            units.Add(new MapScriptUnitView
            {
                Id = unit.Id,
                Type = unit.Kind.ToString().ToLowerInvariant(),
                X = unit.Cell.X,
                Y = unit.Cell.Y,
                OwnerPlayerIndex = unit.PlayerIndex,
            });
        }

        var buildings = new List<MapScriptBuildingView>(match.Buildings.Count);
        foreach (var building in match.Buildings)
        {
            buildings.Add(new MapScriptBuildingView
            {
                Type = building.Kind.ToString().ToLowerInvariant(),
                X = building.Cell.X,
                Y = building.Cell.Y,
                OwnerPlayerIndex = building.OwnerPlayerIndex,
            });
        }

        return new MapScriptContext(
            world,
            playerId: match.CurrentPlayer,
            map: new MapScriptMapView
            {
                Width = match.Width,
                Height = match.Height,
                Surface = surface,
            },
            units: units,
            buildings: buildings,
            lastAction: lastAction);
    }

    public static MapScriptLastAction? FromMatchAction(MatchPlayerAction? action)
    {
        if (action is null || action.Kind == MatchPlayerActionKind.None)
            return null;

        return new MapScriptLastAction
        {
            Kind = action.Kind switch
            {
                MatchPlayerActionKind.SelectUnit => MapScriptActionKind.SelectUnit,
                MatchPlayerActionKind.MoveUnit => MapScriptActionKind.MoveUnit,
                _ => MapScriptActionKind.SelectUnit,
            },
            PlayerIndex = action.PlayerIndex,
            UnitId = action.UnitId,
            SourceX = action.Source?.X,
            SourceY = action.Source?.Y,
            TargetX = action.Target?.X,
            TargetY = action.Target?.Y,
        };
    }
}
