using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Simulates matchmaking locally. Two-player mode stalls at 1/2 until a future network hook fills the lobby.
/// </summary>
public class LocalSimMatchmakingBackend : IMatchmakingBackend
{
    const float OnePlayerJoinDelay = 2f;
    const float LocalJoinDelay = 0.35f;
    const float FoundPauseDuration = 1f;

    readonly MonoBehaviour _runner;
    Coroutine _routine;
    GameModeDefinition _mode;
    MatchmakingSnapshot _snapshot;
    float _startedAt;

    public event Action<MatchmakingSnapshot> StateChanged;
    public event Action Completed;
    public event Action Cancelled;

    public LocalSimMatchmakingBackend(MonoBehaviour runner)
    {
        _runner = runner;
    }

    public void Start(GameModeDefinition mode)
    {
        _mode = mode;
        _startedAt = Time.unscaledTime;
        Publish(MatchmakingPhase.Searching, mode.requiredPlayers, 0, "searching for players");
        _routine = _runner.StartCoroutine(Run(mode));
    }

    public void Cancel()
    {
        StopRoutine();
        Publish(MatchmakingPhase.Cancelled, _snapshot.requiredPlayers, _snapshot.connectedPlayers, "matchmaking cancelled");
        Cancelled?.Invoke();
    }

    /// <summary>
    /// Future networking hook: call when a remote client joins a two-player lobby.
    /// </summary>
    public void NotifyRemotePlayerJoined(int pingMs)
    {
        if (_mode == null || _mode.requiredPlayers != 2)
        {
            return;
        }

        if (_snapshot.connectedPlayers >= _mode.requiredPlayers)
        {
            return;
        }

        StopRoutine();
        _routine = _runner.StartCoroutine(CompleteAfterJoin(_mode.requiredPlayers, pingMs));
    }

    IEnumerator Run(GameModeDefinition mode)
    {
        if (mode.skipMatchmakingDelay)
        {
            yield return InstantCompleteSequence(mode);
            yield break;
        }

        if (mode.requiredPlayers <= 1)
        {
            yield return new WaitForSecondsRealtime(OnePlayerJoinDelay);
            if (_mode == null)
            {
                yield break;
            }

            int ping = UnityEngine.Random.Range(12, 48);
            Publish(MatchmakingPhase.Searching, mode.requiredPlayers, 1, $"player connected · {ping}ms", ping);
            yield return new WaitForSecondsRealtime(FoundPauseDuration);
            if (_mode == null)
            {
                yield break;
            }

            yield return CompleteSequence(mode);
            yield break;
        }

        yield return new WaitForSecondsRealtime(LocalJoinDelay);
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Searching, mode.requiredPlayers, 1, "searching for players");
        _routine = null;
    }

    IEnumerator CompleteAfterJoin(int requiredPlayers, int pingMs)
    {
        Publish(MatchmakingPhase.Searching, requiredPlayers, requiredPlayers, $"player connected · {pingMs}ms", pingMs);
        yield return new WaitForSecondsRealtime(FoundPauseDuration);
        if (_mode == null)
        {
            yield break;
        }

        yield return CompleteSequence(_mode);
    }

    IEnumerator CompleteSequence(GameModeDefinition mode)
    {
        Publish(MatchmakingPhase.Found, mode.requiredPlayers, mode.requiredPlayers, "found players");
        yield return new WaitForSecondsRealtime(0.35f);
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Loading, mode.requiredPlayers, mode.requiredPlayers, "loading match");
        yield return new WaitForSecondsRealtime(FoundPauseDuration);
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Complete, mode.requiredPlayers, mode.requiredPlayers, "found match");
        _routine = null;
        Completed?.Invoke();
    }

    IEnumerator InstantCompleteSequence(GameModeDefinition mode)
    {
        int ping = UnityEngine.Random.Range(12, 48);
        Publish(MatchmakingPhase.Searching, mode.requiredPlayers, 1, $"player connected · {ping}ms", ping);
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Found, mode.requiredPlayers, mode.requiredPlayers, "found players");
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Loading, mode.requiredPlayers, mode.requiredPlayers, "loading match");
        if (_mode == null)
        {
            yield break;
        }

        Publish(MatchmakingPhase.Complete, mode.requiredPlayers, mode.requiredPlayers, "found match");
        _routine = null;
        Completed?.Invoke();
        yield break;
    }

    void Publish(MatchmakingPhase phase, int required, int connected, string feed, int pingMs = 0)
    {
        _snapshot = new MatchmakingSnapshot
        {
            phase = phase,
            modeId = _mode?.id,
            feedLine = feed,
            elapsedSeconds = Time.unscaledTime - _startedAt,
            connectedPlayers = connected,
            requiredPlayers = required,
            lastPingMs = pingMs,
            hasPing = pingMs > 0
        };
        StateChanged?.Invoke(_snapshot);
    }

    void StopRoutine()
    {
        if (_routine != null && _runner != null)
        {
            _runner.StopCoroutine(_routine);
            _routine = null;
        }

        _mode = null;
    }
}
