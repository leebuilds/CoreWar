using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary scripted objective loop for Test Map 1.
/// </summary>
public class TestMapObjectiveManager : MonoBehaviour
{
    public const string MapName = "Test Map 1";
    public const float VictoryPoints = 100f;
    public const float DrillPointsPerSecond = 1f;
    public const float DrillUseDistanceMeters = 2.5f;
    public const float DrillHoldSeconds = 5f;

    public enum DrillInteractionType
    {
        None,
        ToggleDrill,
        RestartOwnDrill,
        SabotageEnemyDrill
    }

    static readonly GameSession.Team[] TeamOrder =
    {
        GameSession.Team.Red,
        GameSession.Team.Blue,
        GameSession.Team.Yellow,
        GameSession.Team.Green
    };

    static TestMapObjectiveManager _instance;

    readonly List<TestMapDrill> _drills = new List<TestMapDrill>();
    readonly Dictionary<GameSession.Team, float> _teamPoints = new Dictionary<GameSession.Team, float>();

    TestMapDrill _activeUseDrill;
    DrillInteractionType _activeInteractionType;
    float _activeUseTimer;
    bool _matchEnded;
    GameSession.Team _winningTeam;

    public static TestMapObjectiveManager Instance => _instance;

    public float LocalTeamProgress => ProgressForTeam(LocalPlayerTeam);
    public bool HasEnded => _matchEnded || (NetworkSync != null && NetworkSync.MatchEnded);
    public GameSession.Team WinningTeam =>
        NetworkSync != null && NetworkSync.MatchEnded ? NetworkSync.WinningTeam : _winningTeam;
    public bool LocalTeamWon => HasEnded && WinningTeam == LocalPlayerTeam;
    public TestMapDrill ActiveUseDrill => _activeUseDrill;
    public DrillInteractionType ActiveInteractionType => _activeInteractionType;
    public float ActiveUseFraction => Mathf.Clamp01(_activeUseTimer / DrillHoldSeconds);
    public string ActiveInteractionLabel =>
        _activeInteractionType == DrillInteractionType.SabotageEnemyDrill
            ? "SABOTAGE"
            : _activeInteractionType == DrillInteractionType.RestartOwnDrill
                ? "RESTART DRILL"
                : _activeInteractionType == DrillInteractionType.ToggleDrill
                    ? "TOGGLE DRILL"
                    : string.Empty;

    static NetworkTestMapObjectiveSync NetworkSync =>
        NetworkTestMapObjectiveSync.Instance != null &&
        NetworkTestMapObjectiveSync.Instance.IsNetworkMatch
            ? NetworkTestMapObjectiveSync.Instance
            : null;

    static GameSession.Team LocalPlayerTeam =>
        ThirdPersonController.Local != null
            ? ThirdPersonController.Local.PlayerTeam
            : GameSession.SelectedTeam;

    static bool UsesTeamSabotageRules =>
        ActiveTeamCount() >= 2 || MultiplayerSessionManager.IsNetworkSessionActive;

    public static TestMapObjectiveManager Create()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var go = new GameObject("Test Map 1 Objective Manager");
        return go.AddComponent<TestMapObjectiveManager>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void RegisterDrill(TestMapDrill drill)
    {
        if (drill == null || _drills.Contains(drill))
        {
            return;
        }

        _drills.Add(drill);
        if (!_teamPoints.ContainsKey(drill.Team))
        {
            _teamPoints[drill.Team] = 0f;
        }
    }

    public TestMapDrill FindDrill(GameSession.Team team)
    {
        for (int i = 0; i < _drills.Count; i++)
        {
            if (_drills[i] != null && _drills[i].Team == team)
            {
                return _drills[i];
            }
        }

        return null;
    }

    public void ApplyDrillWorking(GameSession.Team team, bool working)
    {
        var drill = FindDrill(team);
        if (drill != null)
        {
            drill.SetWorking(working);
        }
    }

    public void HandleNetworkMatchEnded(GameSession.Team winner)
    {
        if (_matchEnded)
        {
            return;
        }

        _winningTeam = winner;
        _matchEnded = true;
        for (int i = 0; i < _drills.Count; i++)
        {
            if (_drills[i] != null)
            {
                _drills[i].SetWorking(false);
            }
        }

        SceneFlow.ApplyMenuInputState();
        TestMatchResultPanel.Create(LocalTeamWon);
        GameSession.EndMatch();
    }

    void Update()
    {
        if (!GameSession.IsMatchActive || GameSession.IsShootingRange || GameSession.IsInPrepPhase || HasEnded)
        {
            ResetUseProgress();
            return;
        }

        if (NetworkSync != null)
        {
            TickPlayerUse();
            return;
        }

        if (MultiplayerSessionManager.IsNetworkSessionActive)
        {
            ResetUseProgress();
            return;
        }

        TickDrills();
        TickPlayerUse();
    }

    void TickDrills()
    {
        for (int i = 0; i < _drills.Count; i++)
        {
            var drill = _drills[i];
            if (drill == null || !drill.IsWorking)
            {
                continue;
            }

            float current = ProgressForTeam(drill.Team);
            current = Mathf.Min(VictoryPoints, current + DrillPointsPerSecond * Time.deltaTime);
            _teamPoints[drill.Team] = current;

            if (current >= VictoryPoints)
            {
                EndMatch();
                return;
            }
        }
    }

    void TickPlayerUse()
    {
        var player = ThirdPersonController.Local;
        if (player == null || player.IsHudOverlayBlocking || player.IsHudGameplayBlocked)
        {
            ResetUseProgress();
            return;
        }

        if (!TryFindNearestInteraction(player.PlayerTeam, player.transform.position,
                out TestMapDrill nearest, out DrillInteractionType interactionType) ||
            !Input.GetKey(KeyCode.T))
        {
            ResetUseProgress();
            return;
        }

        if (_activeUseDrill != nearest || _activeInteractionType != interactionType)
        {
            _activeUseDrill = nearest;
            _activeInteractionType = interactionType;
            _activeUseTimer = 0f;
        }

        _activeUseTimer += Time.deltaTime;
        if (_activeUseTimer < DrillHoldSeconds)
        {
            return;
        }

        CompleteDrillInteraction(player.PlayerTeam, nearest.Team);
        ResetUseProgress();
    }

    void CompleteDrillInteraction(GameSession.Team playerTeam, GameSession.Team drillTeam)
    {
        if (NetworkSync != null)
        {
            NetworkSync.RequestDrillInteraction(drillTeam);
            return;
        }

        var drill = FindDrill(drillTeam);
        if (drill == null)
        {
            return;
        }

        bool working = drill.IsWorking;
        if (!CanInteractWithDrill(playerTeam, drillTeam, working))
        {
            return;
        }

        drill.SetWorking(ResolveWorkingAfterInteraction(playerTeam, drillTeam, working));
    }

    public static bool CanInteractWithDrill(GameSession.Team playerTeam, GameSession.Team drillTeam, bool drillWorking)
    {
        return GetInteractionType(playerTeam, drillTeam, drillWorking) != DrillInteractionType.None;
    }

    public static DrillInteractionType GetInteractionType(
        GameSession.Team playerTeam,
        GameSession.Team drillTeam,
        bool drillWorking)
    {
        if (!UsesTeamSabotageRules)
        {
            return DrillInteractionType.ToggleDrill;
        }

        if (playerTeam == drillTeam)
        {
            return drillWorking ? DrillInteractionType.None : DrillInteractionType.RestartOwnDrill;
        }

        return drillWorking ? DrillInteractionType.SabotageEnemyDrill : DrillInteractionType.None;
    }

    public static bool ResolveWorkingAfterInteraction(
        GameSession.Team playerTeam,
        GameSession.Team drillTeam,
        bool drillWorking)
    {
        var interaction = GetInteractionType(playerTeam, drillTeam, drillWorking);
        switch (interaction)
        {
            case DrillInteractionType.ToggleDrill:
                return !drillWorking;
            case DrillInteractionType.RestartOwnDrill:
                return true;
            case DrillInteractionType.SabotageEnemyDrill:
                return false;
            default:
                return drillWorking;
        }
    }

    static bool TryFindNearestInteraction(
        GameSession.Team playerTeam,
        Vector3 playerPosition,
        out TestMapDrill nearest,
        out DrillInteractionType interactionType)
    {
        nearest = null;
        interactionType = DrillInteractionType.None;
        float bestDistance = DrillUseDistanceMeters;

        for (int i = 0; i < _instance._drills.Count; i++)
        {
            var drill = _instance._drills[i];
            if (drill == null)
            {
                continue;
            }

            bool working = NetworkSync != null
                ? NetworkSync.GetDrillWorking(drill.Team)
                : drill.IsWorking;
            var type = GetInteractionType(playerTeam, drill.Team, working);
            if (type == DrillInteractionType.None)
            {
                continue;
            }

            float distance = Vector3.Distance(playerPosition, drill.UsePoint);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                nearest = drill;
                interactionType = type;
            }
        }

        return nearest != null;
    }

    void ResetUseProgress()
    {
        _activeUseDrill = null;
        _activeInteractionType = DrillInteractionType.None;
        _activeUseTimer = 0f;
    }

    float ProgressForTeam(GameSession.Team team)
    {
        if (NetworkSync != null)
        {
            return NetworkSync.GetTeamPoints(team);
        }

        return _teamPoints.TryGetValue(team, out float points) ? points : 0f;
    }

    void EndMatch()
    {
        GameSession.Team winner = GameSession.Team.Red;
        float winningPoints = -1f;
        foreach (var pair in _teamPoints)
        {
            if (pair.Value > winningPoints)
            {
                winner = pair.Key;
                winningPoints = pair.Value;
            }
        }

        HandleNetworkMatchEnded(winner);
    }

    public static int ActiveTeamCount()
    {
        return Mathf.Clamp(GameSession.RequiredPlayers, 1, TeamOrder.Length);
    }

    public static GameSession.Team TeamAt(int index)
    {
        return TeamOrder[Mathf.Clamp(index, 0, TeamOrder.Length - 1)];
    }
}
