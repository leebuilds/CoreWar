using System;

/// <summary>
/// Pluggable matchmaking backend (local simulation today, networking later).
/// </summary>
public interface IMatchmakingBackend
{
    event Action<MatchmakingSnapshot> StateChanged;
    event Action Completed;
    event Action Cancelled;

    void Start(GameModeDefinition mode);
    void Cancel();
}
