using TinyTBS.Game.Match;
using TinyTBS.Game.Units.Models;

namespace TinyTBS.Game.Ai;

/// <summary>
/// Статическая оценка позиции глазами бота (больше = лучше для него).
/// Не симулирует ходы — только «насколько доска хороша прямо сейчас».
/// </summary>
public static class PositionEvaluator
{
    private const int AloneAllyDistance = 99;
    private const string CastleTag = "castle";

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
        score += ScoreKingCastleObjective(match, botPlayerIndex, profile.CastleObjectiveWeight);
        score -= MeasureKingSafetyPenalty(match, botPlayerIndex, opponent, profile.KingSafetyWeight);

        return score;
    }

    /// <summary>
    /// Manhattan king → nearest enemy castle, or <see cref="int.MaxValue"/> if none.
    /// Used by root search tie-break.
    /// </summary>
    public static int MeasureKingEnemyCastleDistance(MatchState match, int botPlayerIndex)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (!TryFindCastleCapturer(match, botPlayerIndex, out var king))
            return int.MaxValue;

        var best = int.MaxValue;
        foreach (var building in match.Buildings)
        {
            if (!IsEnemyCastle(match, building, botPlayerIndex))
                continue;
            var distance = king.Cell.ManhattanDistanceTo(building.Cell);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    /// <summary>
    /// Non-negative penalty: higher = more exposed king. Used by eval and root tie-break.
    /// </summary>
    public static int MeasureKingSafetyPenalty(
        MatchState match,
        int botPlayerIndex,
        int opponentIndex,
        int kingSafetyWeight)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (kingSafetyWeight <= 0 || opponentIndex < 0)
            return 0;
        if (!TryFindCastleCapturer(match, botPlayerIndex, out var king))
            return 0;

        var enemyNear = MinDistanceToPlayerUnits(match, king, opponentIndex, exceptUnitId: null);
        if (enemyNear == int.MaxValue)
            return 0;

        var allyNear = MinDistanceToPlayerUnits(match, king, botPlayerIndex, exceptUnitId: king.Id);
        if (allyNear == int.MaxValue)
            allyNear = AloneAllyDistance;

        var penalty = 0;
        if (enemyNear < allyNear)
            penalty += (allyNear - enemyNear) * kingSafetyWeight;

        if (enemyNear <= 2)
            penalty += (3 - enemyNear) * kingSafetyWeight;

        return penalty;
    }

    public static int OpponentIndex(MatchState match, int botPlayerIndex)
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
    /// Штраф за расстояние до врагов и до зданий, которые этот юнит реально может захватить.
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
            if (!match.ContentCatalog.TryGetUnit(unit.TypeId, out var unitDefinition))
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
                if (building.OwnerPlayerIndex == botPlayerIndex)
                    continue;
                if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var buildingDefinition))
                    continue;
                if (!MatchUnitAbilities.IsCapturable(building, buildingDefinition))
                    continue;
                if (!MatchUnitAbilities.CanCapture(unitDefinition, buildingDefinition))
                    continue;

                var distance = unit.Cell.ManhattanDistanceTo(building.Cell);
                if (distance < bestCapture)
                    bestCapture = distance;
            }

            if (bestCapture != int.MaxValue)
                score -= Math.Min(bestCapture, rangeCap) * profile.AggressionWeight;
        }

        return score;
    }

    private static int ScoreKingCastleObjective(MatchState match, int botPlayerIndex, int castleObjectiveWeight)
    {
        if (castleObjectiveWeight <= 0)
            return 0;

        var distance = MeasureKingEnemyCastleDistance(match, botPlayerIndex);
        if (distance == int.MaxValue)
            return 0;

        return -distance * castleObjectiveWeight;
    }

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

    private static bool TryFindCastleCapturer(MatchState match, int botPlayerIndex, out MatchUnit king)
    {
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != botPlayerIndex)
                continue;
            if (!match.ContentCatalog.TryGetUnit(unit.TypeId, out var definition))
                continue;
            if (!UnitCanCaptureCastleTag(definition))
                continue;

            king = unit;
            return true;
        }

        king = null!;
        return false;
    }

    private static bool UnitCanCaptureCastleTag(UnitDefinition unit) =>
        unit.Abilities.Any(ability =>
            string.Equals(ability.Type, "captureBuilding", StringComparison.OrdinalIgnoreCase)
            && ability.Tags.Any(tag => string.Equals(tag, CastleTag, StringComparison.OrdinalIgnoreCase)));

    private static bool IsEnemyCastle(MatchState match, MatchBuilding building, int botPlayerIndex)
    {
        if (building.OwnerPlayerIndex == botPlayerIndex)
            return false;
        if (!match.ContentCatalog.TryGetBuilding(building.TypeId, out var definition))
            return false;
        if (!MatchUnitAbilities.IsCapturable(building, definition))
            return false;
        return definition.Tags.Any(tag => string.Equals(tag, CastleTag, StringComparison.OrdinalIgnoreCase));
    }

    private static int MinDistanceToPlayerUnits(
        MatchState match,
        MatchUnit from,
        int playerIndex,
        int? exceptUnitId)
    {
        var best = int.MaxValue;
        foreach (var unit in match.Units)
        {
            if (unit.PlayerIndex != playerIndex)
                continue;
            if (exceptUnitId is int exceptId && unit.Id == exceptId)
                continue;
            var distance = from.Cell.ManhattanDistanceTo(unit.Cell);
            if (distance < best)
                best = distance;
        }

        return best;
    }
}
