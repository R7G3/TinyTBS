namespace TinyTBS.Game.Scripting.Models;

/// <summary>Mutable world surface exposed to <see cref="MapScriptContext"/> API methods.</summary>
public interface IMapScriptWorld
{
    IReadOnlyDictionary<int, int> MoneyByPlayer { get; }

    int? WinnerPlayerIndex { get; }

    string? VictoryReason { get; }

    int GetMoney(int playerId);

    void AddMoney(int playerId, int amount);

    void SetVictory(int playerId, string reason);
}
