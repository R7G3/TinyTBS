namespace TinyTBS.Game.Match.Ai;

/// <summary>Deterministic static evaluation from <paramref name="botPlayerIndex"/>'s perspective (higher = better).</summary>
public static class PositionEvaluator
{
    public static int Evaluate(MatchState match, int botPlayerIndex, BotDifficultyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (match.IsMatchOver)
        {
            if (match.WinnerPlayerIndex == botPlayerIndex)
                return 1_000_000;
            if (match.WinnerPlayerIndex is int)
                return -1_000_000;
        }

        if (match.IsPlayerEliminated(botPlayerIndex))
            return -900_000;

        var opponent = OpponentIndex(match, botPlayerIndex);
        var rich = profile.RichEvaluation;

        // Army HP ×4 so recruiting (cost often > HP) still looks good vs keeping gold.
        var score = 0;
        score += ScoreArmy(match, botPlayerIndex, rich) * 4;
        score -= ScoreArmy(match, opponent, rich) * 4;

        score += CountUnits(match, botPlayerIndex) * profile.UnitCountWeight;
        score -= CountUnits(match, opponent) * profile.UnitCountWeight;

        score += match.GetMoney(botPlayerIndex);
        if (rich && opponent >= 0)
            score -= match.GetMoney(opponent) / 2;

        score += ScoreBuildings(match, botPlayerIndex) * (rich ? 80 : 50);
        score -= ScoreBuildings(match, opponent) * (rich ? 80 : 50);

        score += ScoreAggression(match, botPlayerIndex, opponent, profile);
        score += ScoreHomeBias(match, botPlayerIndex, profile.HomeBiasWeight);

        return score;
    }

    private static int CountUnits(MatchState match, int playerIndex)
    {
        if (playerIndex < 0)
            return 0;

        var count = 0;
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex == playerIndex)
                count++;
        }

        return count;
    }

    private static int ScoreArmy(MatchState match, int playerIndex, bool rich)
    {
        if (playerIndex < 0)
            return 0;

        var total = 0;
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != playerIndex)
                continue;

            var unitScore = unit.HitPoints;
            if (rich)
            {
                unitScore += unit.Level * 8;
                if (match.ContentCatalog.TryGetUnit(unit.TypeId, out var definition))
                    unitScore += definition.Attack + definition.Defence;
            }

            total += unitScore;
        }

        return total;
    }

    private static int ScoreBuildings(MatchState match, int playerIndex)
    {
        if (playerIndex < 0)
            return 0;

        var total = 0;
        foreach (var building in match.Buildings)
        {
            if (building.OwnerPlayerIndex != playerIndex || building.IsRuined)
                continue;
            total += building.AllowsRecruit ? 3 : 1;
        }

        return total;
    }

    /// <summary>
    /// Closer to enemies and capturable buildings is better (negative capped distance).
    /// </summary>
    private static int ScoreAggression(
        MatchState match,
        int botPlayerIndex,
        int opponentIndex,
        BotDifficultyProfile profile)
    {
        if (opponentIndex < 0 || profile.AggressionWeight <= 0)
            return 0;

        var rangeCap = Math.Max(0, profile.AggressionRangeCap);
        var score = 0;
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != botPlayerIndex)
                continue;

            var bestEnemy = int.MaxValue;
            foreach (var enemy in match.Units)
            {
                if (enemy.PlayerIndex != opponentIndex)
                    continue;
                var distance = unit.Cell.ManhattanDistanceTo(enemy.Cell);
                if (distance < bestEnemy)
                    bestEnemy = distance;
            }

            if (bestEnemy != int.MaxValue)
                score -= Math.Min(bestEnemy, rangeCap) * profile.AggressionWeight;

            var bestCapture = int.MaxValue;
            foreach (var building in match.Buildings)
            {
                if (building.IsRuined)
                    continue;
                if (building.OwnerPlayerIndex == botPlayerIndex)
                    continue;
                var distance = unit.Cell.ManhattanDistanceTo(building.Cell);
                if (distance < bestCapture)
                    bestCapture = distance;
            }

            if (bestCapture != int.MaxValue)
                score -= Math.Min(bestCapture, rangeCap);
        }

        return score;
    }

    /// <summary>Prefer staying near own castle / recruit buildings (Easy turtling).</summary>
    private static int ScoreHomeBias(MatchState match, int botPlayerIndex, int homeBiasWeight)
    {
        if (homeBiasWeight <= 0)
            return 0;

        var score = 0;
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != botPlayerIndex)
                continue;

            var bestHome = int.MaxValue;
            foreach (var building in match.Buildings)
            {
                if (building.IsRuined || !building.AllowsRecruit)
                    continue;
                if (building.OwnerPlayerIndex != botPlayerIndex)
                    continue;
                var distance = unit.Cell.ManhattanDistanceTo(building.Cell);
                if (distance < bestHome)
                    bestHome = distance;
            }

            if (bestHome != int.MaxValue)
                score -= bestHome * homeBiasWeight;
        }

        return score;
    }

    private static int OpponentIndex(MatchState match, int botPlayerIndex)
    {
        foreach (var playerIndex in match.MoneyByPlayer.Keys)
        {
            if (playerIndex == botPlayerIndex)
                continue;
            if (!match.IsPlayerEliminated(playerIndex))
                return playerIndex;
        }

        return -1;
    }
}
