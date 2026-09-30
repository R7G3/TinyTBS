namespace TinyTBS.Scripting.Api;

/// <summary>
/// Single context object passed to map script hooks: a snapshot of the match taken before the hook.
/// API methods update the snapshot and queue commands; the host applies them only after the hook
/// finishes within its limits.
/// </summary>
public sealed class MapScriptContext
{
    private readonly Dictionary<int, int> _moneyByPlayer;
    private readonly List<MapScriptCommand> _commands = [];

    internal MapScriptContext(
        int playerId,
        IReadOnlyDictionary<int, int> moneyByPlayer,
        int? winnerPlayerIndex,
        string? victoryReason,
        MapScriptMapView map,
        IReadOnlyList<MapScriptUnitView> units,
        IReadOnlyList<MapScriptBuildingView> buildings,
        MapScriptLastAction? lastAction)
    {
        PlayerId = playerId;
        _moneyByPlayer = new Dictionary<int, int>(moneyByPlayer);
        WinnerPlayerIndex = winnerPlayerIndex;
        VictoryReason = victoryReason;
        Map = map;
        Units = units;
        Buildings = buildings;
        LastAction = lastAction;
    }

    public int PlayerId { get; }

    public int Money => GetMoney(PlayerId);

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _moneyByPlayer;

    public MapScriptMapView Map { get; }

    public IReadOnlyList<MapScriptUnitView> Units { get; }

    public IReadOnlyList<MapScriptBuildingView> Buildings { get; }

    /// <summary>Set only for <c>OnAfterPlayerAction</c>.</summary>
    public MapScriptLastAction? LastAction { get; }

    public int? WinnerPlayerIndex { get; private set; }

    public string? VictoryReason { get; private set; }

    internal IReadOnlyList<MapScriptCommand> Commands => _commands;

    public int GetMoney(int playerId)
    {
        EnsureKnownPlayer(playerId);
        return _moneyByPlayer[playerId];
    }

    public void AddMoney(int playerId, int amount)
    {
        EnsureKnownPlayer(playerId);
        _moneyByPlayer[playerId] = checked(_moneyByPlayer[playerId] + amount);
        _commands.Add(MapScriptCommand.AddMoney(playerId, amount));
    }

    /// <summary>Declares the winner; ignored once a winner is already set.</summary>
    public void SetVictory(int playerId, string reason)
    {
        EnsureKnownPlayer(playerId);
        if (WinnerPlayerIndex is not null)
            return;

        WinnerPlayerIndex = playerId;
        VictoryReason = string.IsNullOrWhiteSpace(reason) ? "victory" : reason.Trim();
        _commands.Add(MapScriptCommand.SetVictory(playerId, VictoryReason));
    }

    private void EnsureKnownPlayer(int playerId)
    {
        if (!_moneyByPlayer.ContainsKey(playerId))
            throw new ArgumentOutOfRangeException(nameof(playerId), playerId, "Unknown player.");
    }
}
