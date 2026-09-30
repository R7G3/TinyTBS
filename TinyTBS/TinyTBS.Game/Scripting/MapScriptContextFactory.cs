using TinyTBS.Game.Maps;
using TinyTBS.Game.Match;
using TinyTBS.Scripting.Api;

namespace TinyTBS.Game.Scripting;

/// <summary>Builds <see cref="MapScriptContext"/> snapshots from match state (on the game thread).</summary>
public static class MapScriptContextFactory
{
    public static MapScriptContext Create(MatchState match, MapScriptLastAction? lastAction = null)
    {
        ArgumentNullException.ThrowIfNull(match);

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
                Type = unit.TypeId.Full,
                X = unit.Cell.X,
                Y = unit.Cell.Y,
                OwnerPlayerIndex = unit.PlayerIndex,
                Hp = unit.HitPoints,
                MaxHp = unit.MaxHealth,
            });
        }

        var buildings = new List<MapScriptBuildingView>(match.Buildings.Count);
        foreach (var building in match.Buildings)
        {
            buildings.Add(new MapScriptBuildingView
            {
                Type = building.TypeId.Full,
                X = building.Cell.X,
                Y = building.Cell.Y,
                OwnerPlayerIndex = building.OwnerPlayerIndex,
                State = building.IsRuined ? MapSurfaceIds.RuinedBuildingState : null,
            });
        }

        return new MapScriptContext(
            playerId: match.CurrentPlayer,
            moneyByPlayer: match.MoneyByPlayer,
            winnerPlayerIndex: match.WinnerPlayerIndex,
            victoryReason: match.VictoryReason,
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
        if (action is null)
            return null;

        return new MapScriptLastAction
        {
            Kind = action.Kind switch
            {
                MatchActionKind.MoveUnit => MapScriptActionKind.MoveUnit,
                MatchActionKind.AttackUnit => MapScriptActionKind.AttackUnit,
                MatchActionKind.DestroyBuilding => MapScriptActionKind.DestroyBuilding,
                MatchActionKind.CaptureBuilding => MapScriptActionKind.CaptureBuilding,
                MatchActionKind.RepairBuilding => MapScriptActionKind.RepairBuilding,
                MatchActionKind.RaiseSkeleton => MapScriptActionKind.RaiseSkeleton,
                MatchActionKind.WaitUnit => MapScriptActionKind.WaitUnit,
                MatchActionKind.RecruitUnit => MapScriptActionKind.RecruitUnit,
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
