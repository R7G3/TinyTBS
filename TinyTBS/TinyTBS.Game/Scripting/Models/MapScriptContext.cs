namespace TinyTBS.Game.Scripting.Models;

/// <summary>
/// Single context object passed to map script hooks.
/// Read via properties; mutate only through API methods.
/// </summary>
public sealed class MapScriptContext
{
    private readonly IMapScriptWorld _world;

    internal MapScriptContext(
        IMapScriptWorld world,
        int playerId,
        MapScriptMapView map,
        IReadOnlyList<MapScriptUnitView> units,
        IReadOnlyList<MapScriptBuildingView> buildings,
        MapScriptLastAction? lastAction)
    {
        _world = world;
        PlayerId = playerId;
        Map = map;
        Units = units;
        Buildings = buildings;
        LastAction = lastAction;
    }

    public int PlayerId { get; }

    public int Money => _world.GetMoney(PlayerId);

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _world.MoneyByPlayer;

    public MapScriptMapView Map { get; }

    public IReadOnlyList<MapScriptUnitView> Units { get; }

    public IReadOnlyList<MapScriptBuildingView> Buildings { get; }

    /// <summary>Set only for <c>OnAfterPlayerAction</c>.</summary>
    public MapScriptLastAction? LastAction { get; }

    public int? WinnerPlayerIndex => _world.WinnerPlayerIndex;

    public string? VictoryReason => _world.VictoryReason;

    public void AddMoney(int playerId, int amount) => _world.AddMoney(playerId, amount);

    public void SetVictory(int playerId, string reason) => _world.SetVictory(playerId, reason);
}
