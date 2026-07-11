using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative drill working state and team points for online Test Map 1 matches.
/// </summary>
public class NetworkTestMapObjectiveSync : NetworkBehaviour
{
    public static NetworkTestMapObjectiveSync Instance { get; private set; }

    readonly NetworkVariable<bool> _redDrillWorking = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<bool> _blueDrillWorking = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<float> _redPoints = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<float> _bluePoints = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<bool> _matchEnded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<int> _winningTeamIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsNetworkMatch => IsSpawned;
    public bool MatchEnded => _matchEnded.Value;
    public GameSession.Team WinningTeam =>
        _winningTeamIndex.Value < 0 ? GameSession.Team.Red : (GameSession.Team)_winningTeamIndex.Value;

    public static NetworkTestMapObjectiveSync CreateIfNeeded()
    {
        if (!MultiplayerSessionManager.IsNetworkSessionActive)
        {
            return null;
        }

        if (Instance != null)
        {
            return Instance;
        }

        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
        {
            return null;
        }

        var go = new GameObject("Network Test Map Objective Sync");
        var sync = go.AddComponent<NetworkTestMapObjectiveSync>();
        go.AddComponent<NetworkObject>().Spawn();
        return sync;
    }

    void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;

        _redDrillWorking.OnValueChanged += HandleDrillStateChanged;
        _blueDrillWorking.OnValueChanged += HandleDrillStateChanged;
        _redPoints.OnValueChanged += HandlePointsChanged;
        _bluePoints.OnValueChanged += HandlePointsChanged;
        _matchEnded.OnValueChanged += HandleMatchEndedChanged;

        ApplyAllDrillStates();
        if (_matchEnded.Value)
        {
            TestMapObjectiveManager.Instance?.HandleNetworkMatchEnded(WinningTeam);
        }
    }

    public override void OnNetworkDespawn()
    {
        _redDrillWorking.OnValueChanged -= HandleDrillStateChanged;
        _blueDrillWorking.OnValueChanged -= HandleDrillStateChanged;
        _redPoints.OnValueChanged -= HandlePointsChanged;
        _bluePoints.OnValueChanged -= HandlePointsChanged;
        _matchEnded.OnValueChanged -= HandleMatchEndedChanged;
        base.OnNetworkDespawn();
    }

    void Update()
    {
        if (!IsSpawned || !IsServer || _matchEnded.Value)
        {
            return;
        }

        TickDrills();
    }

    public bool GetDrillWorking(GameSession.Team team)
    {
        switch (team)
        {
            case GameSession.Team.Blue:
                return _blueDrillWorking.Value;
            default:
                return _redDrillWorking.Value;
        }
    }

    public float GetTeamPoints(GameSession.Team team)
    {
        switch (team)
        {
            case GameSession.Team.Blue:
                return _bluePoints.Value;
            default:
                return _redPoints.Value;
        }
    }

    public void RequestDrillInteraction(GameSession.Team drillTeam)
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            TryApplyDrillInteraction(NetworkManager.Singleton.LocalClientId, drillTeam);
            return;
        }

        RequestDrillInteractionServerRpc((int)drillTeam);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestDrillInteractionServerRpc(int drillTeamIndex, RpcParams rpcParams = default)
    {
        if (_matchEnded.Value)
        {
            return;
        }

        ulong clientId = rpcParams.Receive.SenderClientId;
        TryApplyDrillInteraction(clientId, (GameSession.Team)drillTeamIndex);
    }

    void TryApplyDrillInteraction(ulong clientId, GameSession.Team drillTeam)
    {
        var playerTeam = ResolvePlayerTeam(clientId);
        if (playerTeam == null)
        {
            return;
        }

        var drill = TestMapObjectiveManager.Instance?.FindDrill(drillTeam);
        if (drill == null)
        {
            return;
        }

        bool working = GetDrillWorking(drillTeam);
        if (!TestMapObjectiveManager.CanInteractWithDrill(playerTeam.Value, drillTeam, working))
        {
            Debug.Log(
                $"[Objective] Rejected drill interaction client={clientId} playerTeam={playerTeam} " +
                $"drillTeam={drillTeam} working={working}");
            return;
        }

        bool newWorking = TestMapObjectiveManager.ResolveWorkingAfterInteraction(
            playerTeam.Value,
            drillTeam,
            working);
        SetDrillWorking(drillTeam, newWorking);
        Debug.Log(
            $"[Objective] Drill interaction client={clientId} playerTeam={playerTeam} " +
            $"drillTeam={drillTeam} {working} -> {newWorking}");
    }

    void TickDrills()
    {
        TickTeamDrill(GameSession.Team.Red);
        if (_matchEnded.Value)
        {
            return;
        }

        TickTeamDrill(GameSession.Team.Blue);
    }

    void TickTeamDrill(GameSession.Team team)
    {
        bool working = GetDrillWorking(team);
        if (!working || _matchEnded.Value)
        {
            return;
        }

        float points = GetTeamPoints(team);
        points = Mathf.Min(
            TestMapObjectiveManager.VictoryPoints,
            points + TestMapObjectiveManager.DrillPointsPerSecond * Time.deltaTime);

        switch (team)
        {
            case GameSession.Team.Blue:
                _bluePoints.Value = points;
                break;
            default:
                _redPoints.Value = points;
                break;
        }

        if (points >= TestMapObjectiveManager.VictoryPoints)
        {
            EndNetworkMatch(team);
        }
    }

    void EndNetworkMatch(GameSession.Team winner)
    {
        if (_matchEnded.Value)
        {
            return;
        }

        _matchEnded.Value = true;
        _winningTeamIndex.Value = (int)winner;
        _redDrillWorking.Value = false;
        _blueDrillWorking.Value = false;
        TestMapObjectiveManager.Instance?.HandleNetworkMatchEnded(winner);
    }

    void SetDrillWorking(GameSession.Team team, bool working)
    {
        switch (team)
        {
            case GameSession.Team.Blue:
                _blueDrillWorking.Value = working;
                break;
            default:
                _redDrillWorking.Value = working;
                break;
        }
    }

    static GameSession.Team? ResolvePlayerTeam(ulong clientId)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return null;
        }

        if (client.PlayerObject == null)
        {
            return null;
        }

        var avatar = client.PlayerObject.GetComponent<NetworkPlayerAvatar>();
        return avatar != null ? avatar.PlayerTeam : null;
    }

    void HandleDrillStateChanged(bool previous, bool current)
    {
        ApplyAllDrillStates();
    }

    void HandlePointsChanged(float previous, float current)
    {
        // HUD reads synced values directly.
    }

    void HandleMatchEndedChanged(bool previous, bool current)
    {
        if (!current)
        {
            return;
        }

        TestMapObjectiveManager.Instance?.HandleNetworkMatchEnded(WinningTeam);
    }

    void ApplyAllDrillStates()
    {
        var objective = TestMapObjectiveManager.Instance;
        if (objective == null)
        {
            return;
        }

        objective.ApplyDrillWorking(GameSession.Team.Red, _redDrillWorking.Value);
        objective.ApplyDrillWorking(GameSession.Team.Blue, _blueDrillWorking.Value);
    }
}
