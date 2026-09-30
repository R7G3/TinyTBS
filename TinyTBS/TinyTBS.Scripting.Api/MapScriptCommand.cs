namespace TinyTBS.Scripting.Api;

/// <summary>One state change requested by a map script hook, applied by the host afterwards.</summary>
internal sealed class MapScriptCommand
{
    private MapScriptCommand(MapScriptCommandKind kind, int playerId, int amount, string? reason)
    {
        Kind = kind;
        PlayerId = playerId;
        Amount = amount;
        Reason = reason;
    }

    public MapScriptCommandKind Kind { get; }

    public int PlayerId { get; }

    public int Amount { get; }

    public string? Reason { get; }

    public static MapScriptCommand AddMoney(int playerId, int amount) =>
        new(MapScriptCommandKind.AddMoney, playerId, amount, reason: null);

    public static MapScriptCommand SetVictory(int playerId, string reason) =>
        new(MapScriptCommandKind.SetVictory, playerId, amount: 0, reason);
}
