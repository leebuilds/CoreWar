using System;
using UnityEngine;

/// <summary>
/// Facade for menu UI to start, observe, and cancel matchmaking.
/// </summary>
public static class MatchmakingSession
{
    static IMatchmakingBackend _backend;
    static LocalSimMatchmakingBackend _localBackend;
    static MonoBehaviour _runner;
    static MatchmakingSnapshot _snapshot = MatchmakingSnapshot.Idle();
    static float _startedAt;

    public static MatchmakingSnapshot Snapshot => _snapshot;
    public static float ElapsedSeconds =>
        IsActive ? Time.unscaledTime - _startedAt : _snapshot.elapsedSeconds;
    public static bool IsActive =>
        _snapshot.phase == MatchmakingPhase.Searching ||
        _snapshot.phase == MatchmakingPhase.Found ||
        _snapshot.phase == MatchmakingPhase.Loading;

    public static event Action<MatchmakingSnapshot> Changed;
    public static event Action Completed;
    public static event Action Cancelled;

    public static void BindRunner(MonoBehaviour runner)
    {
        _runner = runner;
    }

    public static void Start(GameModeDefinition mode)
    {
        if (mode == null || _runner == null)
        {
            return;
        }

        Cancel(silent: true);

        _startedAt = Time.unscaledTime;
        _localBackend = new LocalSimMatchmakingBackend(_runner);
        _backend = _localBackend;
        _backend.StateChanged += HandleStateChanged;
        _backend.Completed += HandleCompleted;
        _backend.Cancelled += HandleCancelled;
        _backend.Start(mode);
    }

    public static void Cancel(bool silent = false)
    {
        if (_backend == null)
        {
            _snapshot = MatchmakingSnapshot.Idle();
            if (!silent)
            {
                Cancelled?.Invoke();
            }

            return;
        }

        _backend.Cancel();
    }

    /// <summary>
    /// Future networking hook for two-player test mode.
    /// </summary>
    public static void NotifyRemotePlayerJoined(int pingMs)
    {
        _localBackend?.NotifyRemotePlayerJoined(pingMs);
    }

    public static void Reset()
    {
        if (_backend != null)
        {
            _backend.StateChanged -= HandleStateChanged;
            _backend.Completed -= HandleCompleted;
            _backend.Cancelled -= HandleCancelled;
            _backend = null;
        }

        _localBackend = null;
        _snapshot = MatchmakingSnapshot.Idle();
    }

    static void HandleStateChanged(MatchmakingSnapshot snapshot)
    {
        _snapshot = snapshot;
        Changed?.Invoke(snapshot);
    }

    static void HandleCompleted()
    {
        DetachBackend();
        Completed?.Invoke();
    }

    static void HandleCancelled()
    {
        DetachBackend();
        _snapshot = MatchmakingSnapshot.Idle();
        Cancelled?.Invoke();
    }

    static void DetachBackend()
    {
        if (_backend != null)
        {
            _backend.StateChanged -= HandleStateChanged;
            _backend.Completed -= HandleCompleted;
            _backend.Cancelled -= HandleCancelled;
            _backend = null;
        }

        _localBackend = null;
    }
}
