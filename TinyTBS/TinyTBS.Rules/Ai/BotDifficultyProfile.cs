namespace TinyTBS.Rules.Ai;

/// <summary>
/// Настройки «силы мысли» бота для одной сложности.
/// Не читерство: те же правила матча, меняются только лимиты поиска и веса оценки.
/// </summary>
public sealed class BotDifficultyProfile
{
    /// <summary>
    /// Сколько атомарных действий смотрим вперёд от корня (ply).
    /// 1 ≈ «сделал ход — сразу Evaluate»; 2 ≈ «ход + один ответ/продолжение».
    /// </summary>
    public required int MaxDepth { get; init; }

    /// <summary>Жёсткий потолок узлов дерева за одно ChooseAction (защита от тормозов).</summary>
    public required int NodeLimit { get; init; }

    /// <summary>Включать ли доп. глубину после атак/найма (см. QuiescencePlies).</summary>
    public required bool UseQuiescence { get; init; }

    /// <summary>
    /// Сколько ещё ply смотреть в режиме quiescence после «шумного» хода,
    /// когда обычный depthLeft уже 0. Нужно, чтобы не останавливаться на полубое.
    /// </summary>
    public int QuiescencePlies { get; init; }

    /// <summary>Более детальная оценка (уровни, Atk/Def, золото врага).</summary>
    public required bool RichEvaluation { get; init; }

    /// <summary>Множитель штрафа за шаг до врага (агрессия).</summary>
    public int AggressionWeight { get; init; } = 3;

    /// <summary>
    /// Дистанции дальше Cap считаются равными Cap.
    /// У Easy маленький Cap → нет смысла бежать через всю карту.
    /// </summary>
    public int AggressionRangeCap { get; init; } = 99;

    /// <summary>Тяга к своему замку; выше = спокойнее / «черепаха».</summary>
    public int HomeBiasWeight { get; init; }

    /// <summary>Бонус за каждого живого юнита (стимул вербовать).</summary>
    public int UnitCountWeight { get; init; } = 120;

    /// <summary>
    /// Тяга VIP-юнита (<c>uniquePerPlayer</c>) к вражеским строениям
    /// <c>countsTowardPlayerDefeat</c>. Умеренная: не должна перебивать safety / армию.
    /// </summary>
    public int CastleObjectiveWeight { get; init; }

    /// <summary>Штраф за оголённого VIP-юнита (враг ближе своих / зона ≤2).</summary>
    public int KingSafetyWeight { get; init; }

    /// <summary>
    /// true (Easy): Wait может конкурировать с чистым перемещением,
    /// если нет атаки/захвата/найма. false (Normal): Wait вырезается фильтром.
    /// </summary>
    public bool AllowWaitWithMoves { get; init; }

    public static BotDifficultyProfile For(BotDifficulty difficulty) =>
        difficulty switch
        {
            BotDifficulty.Normal => new BotDifficultyProfile
            {
                MaxDepth = 2,
                NodeLimit = 2500,
                UseQuiescence = true,
                QuiescencePlies = 2,
                RichEvaluation = true,
                AggressionWeight = 3,
                AggressionRangeCap = 99,
                HomeBiasWeight = 0,
                UnitCountWeight = 120,
                CastleObjectiveWeight = 13, // was 10
                KingSafetyWeight = 12,
                AllowWaitWithMoves = false,
            },
            BotDifficulty.Hard => new BotDifficultyProfile
            {
                MaxDepth = 3,
                NodeLimit = 8000,
                UseQuiescence = true,
                QuiescencePlies = 3,
                RichEvaluation = true,
                AggressionWeight = 3,
                AggressionRangeCap = 99,
                HomeBiasWeight = 0,
                UnitCountWeight = 120,
                CastleObjectiveWeight = 10,
                KingSafetyWeight = 12,
                AllowWaitWithMoves = false,
            },
            _ => new BotDifficultyProfile
            {
                MaxDepth = 2,
                NodeLimit = 800,
                UseQuiescence = false,
                QuiescencePlies = 0,
                RichEvaluation = false,
                AggressionWeight = 1,
                AggressionRangeCap = 4,
                HomeBiasWeight = 2,
                UnitCountWeight = 70,
                CastleObjectiveWeight = 6,
                KingSafetyWeight = 8,
                AllowWaitWithMoves = true,
            },
        };
}
