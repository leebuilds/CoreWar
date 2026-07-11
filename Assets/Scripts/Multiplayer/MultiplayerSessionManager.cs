using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal host/join-code multiplayer test flow using Multiplayer Services Sessions with Relay.
/// </summary>
public class MultiplayerSessionManager : MonoBehaviour
{
    public const string PlayerPrefabResourceName = "NetworkPlayer";
    const int MaxPlayers = 2;
    const string SessionType = "corewar-relay-test";

    static MultiplayerSessionManager _instance;

    ISession _activeSession;
    NetworkManager _networkManager;
    GameObject _playerPrefab;
    bool _isBusy;

    public static MultiplayerSessionManager Instance => EnsureExists();
    public static bool HasInstance => _instance != null;
    public static bool IsNetworkSessionActive =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening;

    public string JoinCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = "offline";
    public string Error { get; private set; } = string.Empty;
    public bool IsBusy => _isBusy;
    public bool HasActiveSession => _activeSession != null;

    public event Action StateChanged;

    public static MultiplayerSessionManager EnsureExists()
    {
        if (_instance != null)
        {
            return _instance;
        }

        var go = new GameObject("Multiplayer Session Manager");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<MultiplayerSessionManager>();
        return _instance;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureNetworkManager();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            UnsubscribeNetworkEvents();
            _instance = null;
        }
    }

    public async Task HostAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (HasActiveSession || IsNetworkSessionActive)
        {
            Error = "Already connected. Disconnect before starting another session.";
            RaiseChanged();
            return;
        }

        SetBusy("initializing services");
        try
        {
            await EnsureServicesAsync();
            PrepareLocalGameSession();
            EnsureNetworkManager();
            ConfigureConnectionPayload();

            Status = "creating session";
            Error = string.Empty;
            RaiseChanged();

            var options = new SessionOptions
            {
                Name = $"CoreWar {UnityEngine.Random.Range(1000, 9999)}",
                MaxPlayers = MaxPlayers,
                IsPrivate = true,
                Type = SessionType
            }.WithRelayNetwork();

            _activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            JoinCode = _activeSession.Code;
            Debug.Log($"[Multiplayer] Session created. Join code: {JoinCode}");

            Status = $"hosting {JoinCode}";
            _isBusy = false;
            RaiseChanged();

            LoadGameForHost();
        }
        catch (Exception ex)
        {
            Fail("Host failed", ex);
        }
    }

    public async Task JoinAsync(string joinCode)
    {
        if (_isBusy)
        {
            return;
        }

        if (HasActiveSession || IsNetworkSessionActive)
        {
            Error = "Already connected. Disconnect before joining another session.";
            RaiseChanged();
            return;
        }

        joinCode = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(joinCode))
        {
            Error = "Enter a join code.";
            RaiseChanged();
            return;
        }

        SetBusy("initializing services");
        try
        {
            await EnsureServicesAsync();
            PrepareLocalGameSession();
            EnsureNetworkManager();
            ConfigureConnectionPayload();

            Status = "joining session";
            Error = string.Empty;
            RaiseChanged();

            var options = new JoinSessionOptions
            {
                Type = SessionType
            }.WithNetworkOptions(new NetworkOptions());

            _activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, options);
            JoinCode = _activeSession.Code;
            Debug.Log($"[Multiplayer] Joined session with code: {JoinCode}");

            Status = $"joined {JoinCode}";
            _isBusy = false;
            RaiseChanged();
        }
        catch (Exception ex)
        {
            Fail("Join failed", ex);
        }
    }

    public async Task LeaveAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        Status = "leaving session";
        Error = string.Empty;
        RaiseChanged();

        try
        {
            if (_activeSession != null)
            {
                await _activeSession.LeaveAsync();
                _activeSession = null;
            }
            else if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Multiplayer] Graceful leave failed: {ex.Message}");
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
        }

        JoinCode = string.Empty;
        Status = "offline";
        Error = string.Empty;
        _isBusy = false;
        RaiseChanged();

        if (!SceneFlow.IsMainMenuActive)
        {
            SceneFlow.EnterMainMenu();
        }
    }

    async Task EnsureServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
            Debug.Log("[Multiplayer] Unity Services initialized.");
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Multiplayer] Authentication successful: {AuthenticationService.Instance.PlayerId}");
        }
    }

    void EnsureNetworkManager()
    {
        if (_networkManager != null)
        {
            ConfigureNetworkManager(_networkManager);
            return;
        }

        _networkManager = NetworkManager.Singleton;
        if (_networkManager == null)
        {
            var go = new GameObject("NetworkManager");
            DontDestroyOnLoad(go);
            _networkManager = go.AddComponent<NetworkManager>();
            go.AddComponent<UnityTransport>();
        }
        else
        {
            DontDestroyOnLoad(_networkManager.gameObject);
        }

        ConfigureNetworkManager(_networkManager);
    }

    void ConfigureNetworkManager(NetworkManager manager)
    {
        var transport = manager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            transport = manager.gameObject.AddComponent<UnityTransport>();
        }

        _playerPrefab = Resources.Load<GameObject>(PlayerPrefabResourceName);
        if (_playerPrefab == null)
        {
            Error = "Missing Resources/NetworkPlayer.prefab. Use CoreWar/Multiplayer/Rebuild Network Player Prefab in Unity.";
            Debug.LogError($"[Multiplayer] {Error}");
        }

        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = _playerPrefab;
        manager.NetworkConfig.EnableSceneManagement = true;
        manager.NetworkConfig.ConnectionApproval = true;
        manager.NetworkConfig.ForceSamePrefabs = true;
        manager.ConnectionApprovalCallback = ApproveConnection;

        UnsubscribeNetworkEvents();
        manager.OnClientConnectedCallback += HandleClientConnected;
        manager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    void UnsubscribeNetworkEvents()
    {
        if (_networkManager == null)
        {
            return;
        }

        _networkManager.OnClientConnectedCallback -= HandleClientConnected;
        _networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
        Debug.Log($"[Multiplayer] Client approved: {request.ClientNetworkId}");
    }

    void ConfigureConnectionPayload()
    {
        if (_networkManager == null)
        {
            return;
        }

        var activeCard = GameSession.ActiveCardId ?? string.Empty;
        var payload = $"{ProfileSession.ActiveProfile?.username ?? "player"}|{activeCard}";
        _networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
    }

    void PrepareLocalGameSession()
    {
        ProfileSession.EnsureInitialized();
        ProfileSession.TouchActivity();

        var profile = ProfileSession.ActiveProfile;
        string loadoutA = profile?.loadoutCardIds != null && profile.loadoutCardIds.Length > 0
            ? profile.loadoutCardIds[0]
            : null;
        string loadoutB = profile?.loadoutCardIds != null && profile.loadoutCardIds.Length > 1
            ? profile.loadoutCardIds[1]
            : null;
        string activeCard = !string.IsNullOrEmpty(loadoutA) ? loadoutA : null;

        GameSession.BeginMatch(
            GameSession.Team.Red,
            loadoutA,
            loadoutB,
            activeCard,
            "test_two_player",
            MaxPlayers);
    }

    void LoadGameForHost()
    {
        if (_networkManager == null || !_networkManager.IsServer)
        {
            return;
        }

        if (SceneFlow.IsGameActive)
        {
            return;
        }

        Debug.Log("[Multiplayer] Loading gameplay scene through Netcode scene management.");
        SceneFlow.ApplyGameInputState();
        _networkManager.SceneManager.LoadScene(SceneFlow.GameSceneName, LoadSceneMode.Single);
    }

    void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[Multiplayer] Client joined: {clientId}");
        Status = _networkManager != null && _networkManager.IsHost
            ? $"hosting {JoinCode}"
            : $"connected {JoinCode}";
        RaiseChanged();
    }

    void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Multiplayer] Client disconnected: {clientId}");
        RaiseChanged();
    }

    void SetBusy(string status)
    {
        _isBusy = true;
        Status = status;
        Error = string.Empty;
        RaiseChanged();
    }

    void Fail(string prefix, Exception ex)
    {
        Debug.LogError($"[Multiplayer] {prefix}: {ex}");
        Error = $"{prefix}: {ex.Message}";
        Status = "offline";
        JoinCode = string.Empty;
        _activeSession = null;
        _isBusy = false;
        RaiseChanged();

        if (_networkManager != null && _networkManager.IsListening)
        {
            _networkManager.Shutdown();
        }
    }

    void RaiseChanged()
    {
        StateChanged?.Invoke();
    }
}
