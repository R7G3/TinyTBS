namespace TinyTBS.Rules.Match;

/// <summary>
/// Outcome of one rules command. <see cref="SelectedUnitId"/> is the activation the caller should keep;
/// the match itself does not store it.
/// </summary>
public readonly record struct MatchApplyResult(bool Applied, int? SelectedUnitId);
