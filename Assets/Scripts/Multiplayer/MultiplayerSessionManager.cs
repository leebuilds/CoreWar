using System;
using System.IO;
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
    const int MaxPlayers = 2;
    const string SessionType = "corewar-relay-test";
    public const string PlayerPrefabResourceName = "NetworkPlayer";

    static MultiplayerSessionManager _instance;

    [SerializeField] GameObject networkManagerPrefab;
    [SerializeField] GameObject playerPrefab;

    ISession _activeSession;
    NetworkManager _networkManager;
    bool _networkManagerConfigured;
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
            BootTrace.Log("SERVICES", "HostAsync EnsureServicesAsync begin");
            await EnsureServicesAsync();
            BootTrace.Log("SERVICES", "HostAsync EnsureServicesAsync complete");
            PrepareLocalGameSession();
            BootTrace.Log("NETWORK", "HostAsync TryEnsureNetworkManager begin");
            if (!TryEnsureNetworkManager())
            {
                BootTrace.LogError("NETWORK", "HostAsync TryEnsureNetworkManager FAILED");
                _isBusy = false;
                RaiseChanged();
                return;
            }
            BootTrace.Log("NETWORK", "HostAsync TryEnsureNetworkManager ok");

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

            BootTrace.Log("SERVICES", "CreateSessionAsync begin");
            _activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            JoinCode = _activeSession.Code;
            BootTrace.Log("NETWORK", $"Session created JoinCode={JoinCode}");
            Debug.Log($"[Multiplayer] Session created. Join code: {JoinCode}");

            Status = $"hosting {JoinCode}";
            _isBusy = false;
            RaiseChanged();

            LoadGameForHost();
        }
        catch (Exception ex)
        {
            BootTrace.LogError("NETWORK", $"HostAsync exception: {ex}");
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
            BootTrace.Log("SERVICES", "JoinAsync EnsureServicesAsync begin");
            await EnsureServicesAsync();
            BootTrace.Log("SERVICES", "JoinAsync EnsureServicesAsync complete");
            PrepareLocalGameSession();
            BootTrace.Log("NETWORK", "JoinAsync TryEnsureNetworkManager begin");
            if (!TryEnsureNetworkManager())
            {
                BootTrace.LogError("NETWORK", "JoinAsync TryEnsureNetworkManager FAILED");
                _isBusy = false;
                RaiseChanged();
                return;
            }
            BootTrace.Log("NETWORK", "JoinAsync TryEnsureNetworkManager ok");

            ConfigureConnectionPayload();

            Status = "joining session";
            Error = string.Empty;
            RaiseChanged();

            var options = new JoinSessionOptions
            {
                Type = SessionType
            }.WithNetworkOptions(new NetworkOptions());

            BootTrace.Log("SERVICES", $"JoinSessionByCodeAsync begin code={joinCode}");
            _activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, options);
            JoinCode = _activeSession.Code;
            BootTrace.Log("NETWORK", $"Joined session code={JoinCode}; waiting for host-driven Game scene load");
            Debug.Log($"[Multiplayer] Joined session with code: {JoinCode}");

            Status = $"joined {JoinCode}";
            _isBusy = false;
            RaiseChanged();
        }
        catch (Exception ex)
        {
            BootTrace.LogError("NETWORK", $"JoinAsync exception: {ex}");
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
        BootTrace.Log("SERVICES", $"UnityServices.State={UnityServices.State}");
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
            BootTrace.Log("SERVICES", "UnityServices.InitializeAsync complete");
            Debug.Log("[Multiplayer] Unity Services initialized.");
        }

        BootTrace.Log("SERVICES", $"Auth signedIn={AuthenticationService.Instance.IsSignedIn}");
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            BootTrace.Log("SERVICES", $"Auth anonymous ok playerId={AuthenticationService.Instance.PlayerId}");
            Debug.Log($"[Multiplayer] Authentication successful: {AuthenticationService.Instance.PlayerId}");
        }
    }

    bool TryEnsureNetworkManager()
    {
        if (_networkManagerConfigured && _networkManager != null)
        {
            return ConfigureNetworkManager(_networkManager);
        }

        _networkManager = NetworkManager.Singleton;
        if (_networkManager != null)
        {
            DontDestroyOnLoad(_networkManager.gameObject);
            if (ConfigureNetworkManager(_networkManager))
            {
                _networkManagerConfigured = true;
                return true;
            }

            ClearInvalidNetworkManager("Existing NetworkManager.Singleton failed validation.");
            return false;
        }

        GameObject prefab = networkManagerPrefab != null
            ? networkManagerPrefab
            : Resources.Load<GameObject>("NetworkManager");
        BootTrace.Log(
            "NETWORK",
            $"TryEnsureNetworkManager prefabSource={(networkManagerPrefab != null ? "serialized" : "Resources.Load")} " +
            $"prefab={(prefab == null ? "NULL" : prefab.name)}");
        if (prefab == null)
        {
            Error = "Missing NetworkManager prefab. Assign it on MultiplayerSessionManager or place Assets/Resources/NetworkManager.prefab in the project.";
            Debug.LogError($"[Multiplayer] {Error} Rebuild with CoreWar/Multiplayer/Rebuild Multiplayer Prefabs in the Unity Editor.");
            BootTrace.LogError("NETWORK", Error);
            return false;
        }

        var instance = Instantiate(prefab);
        instance.name = "NetworkManager";
        DontDestroyOnLoad(instance);
        _networkManager = instance.GetComponent<NetworkManager>();
        if (_networkManager == null)
        {
            Error = $"NetworkManager prefab '{prefab.name}' is missing a NetworkManager component.";
            Debug.LogError($"[Multiplayer] {Error}");
            Destroy(instance);
            _networkManager = null;
            return false;
        }

        if (!ConfigureNetworkManager(_networkManager))
        {
            ClearInvalidNetworkManager("Instantiated NetworkManager prefab failed validation.");
            return false;
        }

        _networkManagerConfigured = true;
        return true;
    }

    void ClearInvalidNetworkManager(string reason)
    {
        Debug.LogError($"[Multiplayer] {reason} Destroying invalid NetworkManager instance.");
        UnsubscribeNetworkEvents();
        _networkManagerConfigured = false;

        if (_networkManager == null)
        {
            return;
        }

        if (_networkManager.IsListening)
        {
            _networkManager.Shutdown();
        }

        Destroy(_networkManager.gameObject);
        _networkManager = null;
    }

    bool ConfigureNetworkManager(NetworkManager manager)
    {
        if (manager == null)
        {
            Error = "NetworkManager reference is null.";
            Debug.LogError("[Multiplayer] ConfigureNetworkManager called with a null NetworkManager.");
            return false;
        }

        if (manager.NetworkConfig == null)
        {
            Error = "NetworkManager.NetworkConfig is null. Runtime AddComponent<NetworkManager>() does not create NetworkConfig in standalone builds. Use Assets/Resources/NetworkManager.prefab.";
            Debug.LogError($"[Multiplayer] {Error} GameObject='{manager.gameObject.name}'.");
            return false;
        }

        UnityTransport transport = manager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Error = "UnityTransport is missing on the NetworkManager GameObject.";
            Debug.LogError($"[Multiplayer] {Error} GameObject='{manager.gameObject.name}'. Add UnityTransport to the same object as NetworkManager in the prefab.");
            return false;
        }

        GameObject resolvedPlayerPrefab = ResolvePlayerPrefab(manager);
        if (resolvedPlayerPrefab == null)
        {
            Error = "Player prefab is missing. Assign it on MultiplayerSessionManager, on NetworkManager.NetworkConfig.PlayerPrefab, or place Assets/Resources/NetworkPlayer.prefab in the project.";
            Debug.LogError($"[Multiplayer] {Error}");
            return false;
        }

        if (resolvedPlayerPrefab.GetComponent<NetworkObject>() == null)
        {
            Error = $"Player prefab '{resolvedPlayerPrefab.name}' is missing a NetworkObject component.";
            Debug.LogError($"[Multiplayer] {Error}");
            return false;
        }

        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = resolvedPlayerPrefab;
        manager.NetworkConfig.EnableSceneManagement = true;
        manager.NetworkConfig.ConnectionApproval = true;
        manager.NetworkConfig.ForceSamePrefabs = true;
        manager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        manager.ConnectionApprovalCallback = ApproveConnection;

        UnsubscribeNetworkEvents();
        manager.OnClientConnectedCallback += HandleClientConnected;
        manager.OnClientDisconnectCallback += HandleClientDisconnected;
        return true;
    }

    GameObject ResolvePlayerPrefab(NetworkManager manager)
    {
        if (playerPrefab != null)
        {
            return playerPrefab;
        }

        if (manager != null && manager.NetworkConfig != null && manager.NetworkConfig.PlayerPrefab != null)
        {
            return manager.NetworkConfig.PlayerPrefab;
        }

        return Resources.Load<GameObject>(PlayerPrefabResourceName);
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
        if (_networkManager == null || _networkManager.NetworkConfig == null)
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
        GameSession.LogDiagnostics("MultiplayerSessionManager.PrepareLocalGameSession after BeginMatch");
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

        GameSession.LogDiagnostics("MultiplayerSessionManager.LoadGameForHost (before Netcode LoadScene)");
        BootTrace.Log("SCENES", "LoadGameForHost -> Netcode SceneManager.LoadScene(Game)");
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
