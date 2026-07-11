using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side player spawn point for the runtime-built Game scene.
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviour
{
    static NetworkPlayerSpawner _instance;

    VoxelLightingWorld _voxelWorld;
    GameObject _playerPrefab;

    public static VoxelLightingWorld ActiveVoxelWorld =>
        _instance != null ? _instance._voxelWorld : null;

    public static NetworkPlayerSpawner Create(VoxelLightingWorld voxelWorld)
    {
        if (!MultiplayerSessionManager.IsNetworkSessionActive)
        {
            return null;
        }

        var go = new GameObject("Network Player Spawner");
        var spawner = go.AddComponent<NetworkPlayerSpawner>();
        spawner._voxelWorld = voxelWorld;
        spawner._playerPrefab = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.NetworkConfig.PlayerPrefab
            : Resources.Load<GameObject>(MultiplayerSessionManager.PlayerPrefabResourceName);
        return spawner;
    }

    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            return;
        }

        manager.OnClientConnectedCallback += HandleClientConnected;
        manager.OnClientDisconnectCallback += HandleClientDisconnected;

        if (manager.IsServer)
        {
            SpawnMissingPlayers();
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        var manager = NetworkManager.Singleton;
        if (manager != null)
        {
            manager.OnClientConnectedCallback -= HandleClientConnected;
            manager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Multiplayer] Player disconnected: {clientId}");
    }

    void SpawnMissingPlayers()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            return;
        }

        foreach (ulong clientId in manager.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    void SpawnPlayerForClient(ulong clientId)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
        {
            return;
        }

        if (!manager.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return;
        }

        if (client.PlayerObject != null)
        {
            return;
        }

        if (_playerPrefab == null)
        {
            _playerPrefab = manager.NetworkConfig.PlayerPrefab ??
                Resources.Load<GameObject>(MultiplayerSessionManager.PlayerPrefabResourceName);
        }

        if (_playerPrefab == null)
        {
            Debug.LogError("[Multiplayer] Cannot spawn player: NetworkPlayer prefab is missing.");
            return;
        }

        Vector3 spawnPosition = SpawnPositionFor(clientId);
        var player = Instantiate(_playerPrefab, spawnPosition, Quaternion.identity);
        var avatar = player.GetComponent<NetworkPlayerAvatar>();
        if (avatar != null)
        {
            avatar.ServerPrepare(clientId, SpawnIndexFor(clientId));
        }

        var networkObject = player.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
        Debug.Log($"[Multiplayer] Player spawned for client {clientId} at {spawnPosition}.");
    }

    Vector3 SpawnPositionFor(ulong clientId)
    {
        if (GameSession.IsShootingRange)
        {
            return ShootingRangeSession.PlayerSpawnPosition;
        }

        int index = SpawnIndexFor(clientId);
        float side = index % 2 == 0 ? -1f : 1f;
        float row = index / 2;
        return new Vector3(side * (2f + row), 1.1f, -3f + row);
    }

    static int SpawnIndexFor(ulong clientId)
    {
        return Mathf.Clamp((int)clientId, 0, 16);
    }
}
