using TinyTBS.Rules.Match;
using TinyTBS.Game.Match.Session;

namespace TinyTBS.Game.Saves;

/// <summary>
/// In-memory match kept after Pause → Main menu (Continue resumes without disk).
/// </summary>
public sealed class SuspendedMatchHold
{
    public SuspendedMatchHold(GameplaySession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public GameplaySession Session { get; }
}
