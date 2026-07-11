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
    float _activeUseTimer;
    bool _matchEnded;
    GameSession.Team _winningTeam;

    public static TestMapObjectiveManager Instance => _instance;

    public float LocalTeamProgress => ProgressForTeam(GameSession.SelectedTeam);
    public bool HasEnded => _matchEnded;
    public GameSession.Team WinningTeam => _winningTeam;
    public bool LocalTeamWon => _matchEnded && _winningTeam == GameSession.SelectedTeam;
    public TestMapDrill ActiveUseDrill => _activeUseDrill;
    public float ActiveUseFraction => Mathf.Clamp01(_activeUseTimer / DrillHoldSeconds);

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

    void Update()
    {
        if (!GameSession.IsMatchActive || GameSession.IsShootingRange || GameSession.IsInPrepPhase || _matchEnded)
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

        TestMapDrill nearest = FindNearestUsableDrill(player.transform.position);
        if (nearest == null || !Input.GetKey(KeyCode.T))
        {
            ResetUseProgress();
            return;
        }

        if (_activeUseDrill != nearest)
        {
            _activeUseDrill = nearest;
            _activeUseTimer = 0f;
        }

        _activeUseTimer += Time.deltaTime;
        if (_activeUseTimer >= DrillHoldSeconds)
        {
            nearest.ToggleWorking();
            ResetUseProgress();
        }
    }

    TestMapDrill FindNearestUsableDrill(Vector3 playerPosition)
    {
        TestMapDrill nearest = null;
        float bestDistance = DrillUseDistanceMeters;
        for (int i = 0; i < _drills.Count; i++)
        {
            var drill = _drills[i];
            if (drill == null)
            {
                continue;
            }

            float distance = Vector3.Distance(playerPosition, drill.UsePoint);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                nearest = drill;
            }
        }

        return nearest;
    }

    void ResetUseProgress()
    {
        _activeUseDrill = null;
        _activeUseTimer = 0f;
    }

    float ProgressForTeam(GameSession.Team team)
    {
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

    public static int ActiveTeamCount()
    {
        return Mathf.Clamp(GameSession.RequiredPlayers, 1, TeamOrder.Length);
    }

    public static GameSession.Team TeamAt(int index)
    {
        return TeamOrder[Mathf.Clamp(index, 0, TeamOrder.Length - 1)];
    }
}
