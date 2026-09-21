using TinyTBS.Game.Match;
using TinyTBS.Game.Scripting.Models;

namespace TinyTBS.Game.Scripting;

/// <summary>Adapts <see cref="MatchState"/> economy / victory for map scripts.</summary>
public sealed class MatchMapScriptWorld : IMapScriptWorld
{
    private readonly MatchState _match;

    public MatchMapScriptWorld(MatchState match)
    {
        _match = match;
    }

    public IReadOnlyDictionary<int, int> MoneyByPlayer => _match.MoneyByPlayer;

    public int? WinnerPlayerIndex => _match.WinnerPlayerIndex;

    public string? VictoryReason => _match.VictoryReason;

    public int GetMoney(int playerId) => _match.GetMoney(playerId);

    public void AddMoney(int playerId, int amount) => _match.AddMoney(playerId, amount);

    public void SetVictory(int playerId, string reason) => _match.SetVictory(playerId, reason);
}
