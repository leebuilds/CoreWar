using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the multiplayer prefabs required by Netcode for standalone and editor builds.
/// </summary>
[InitializeOnLoad]
public static class MultiplayerPrefabSetup
{
    const string ResourcesPath = "Assets/Resources";
    const string PlayerPrefabPath = "Assets/Resources/NetworkPlayer.prefab";
    const string NetworkManagerPrefabPath = "Assets/Resources/NetworkManager.prefab";
    const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

    static MultiplayerPrefabSetup()
    {
        EditorApplication.delayCall += EnsureMultiplayerPrefabs;
    }

    [MenuItem("CoreWar/Multiplayer/Rebuild Multiplayer Prefabs")]
    public static void RebuildAllMultiplayerPrefabs()
    {
        EnsureMultiplayerPrefabs(force: true);
    }

    [MenuItem("CoreWar/Multiplayer/Rebuild Network Player Prefab")]
    public static void RebuildNetworkPlayerPrefab()
    {
        EnsureNetworkPlayerPrefab(force: true);
    }

    static void EnsureMultiplayerPrefabs()
    {
        EnsureMultiplayerPrefabs(force: false);
    }

    static void EnsureMultiplayerPrefabs(bool force)
    {
        EnsureResourcesFolder();
        EnsureNetworkPlayerPrefab(force);
        EnsureNetworkManagerPrefab(force);
    }

    static void EnsureResourcesFolder()
    {
        if (AssetDatabase.IsValidFolder(ResourcesPath))
        {
            return;
        }

        if (Directory.Exists(ResourcesPath))
        {
            AssetDatabase.ImportAsset(ResourcesPath);
            return;
        }

        AssetDatabase.CreateFolder("Assets", "Resources");
    }

    static void EnsureNetworkPlayerPrefab(bool force)
    {
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null)
        {
            return;
        }

        var root = new GameObject("NetworkPlayer");
        try
        {
            root.AddComponent<NetworkObject>();
            var networkTransform = root.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.Interpolate = true;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 70f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            root.AddComponent<PlayerHealth>();
            var controller = root.AddComponent<ThirdPersonController>();
            controller.deferStartUntilNetworkSpawn = true;
            controller.hideLocalCharacterVisual = false;
            root.AddComponent<NetworkPlayerAvatar>();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Multiplayer] Network player prefab ready: {PlayerPrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void EnsureNetworkManagerPrefab(bool force)
    {
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(NetworkManagerPrefabPath) != null)
        {
            return;
        }

        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError("[Multiplayer] Cannot build NetworkManager prefab because NetworkPlayer.prefab is missing.");
            return;
        }

        if (playerPrefab.GetComponent<NetworkObject>() == null)
        {
            Debug.LogError("[Multiplayer] NetworkPlayer.prefab is missing NetworkObject.");
            return;
        }

        var defaultPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(DefaultNetworkPrefabsPath);
        var root = new GameObject("NetworkManager");
        try
        {
            var transport = root.AddComponent<UnityTransport>();
            var manager = root.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = playerPrefab,
                EnableSceneManagement = true,
                ConnectionApproval = true,
                ForceSamePrefabs = true,
                AutoSpawnPlayerPrefabClientSide = false
            };

            if (defaultPrefabs != null)
            {
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList> { defaultPrefabs };
            }

            PrefabUtility.SaveAsPrefabAsset(root, NetworkManagerPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Multiplayer] Network manager prefab ready: {NetworkManagerPrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
