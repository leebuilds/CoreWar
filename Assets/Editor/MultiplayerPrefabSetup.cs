using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the NetworkPlayer prefab required by Netcode from the existing runtime player components.
/// </summary>
[InitializeOnLoad]
public static class MultiplayerPrefabSetup
{
    const string ResourcesPath = "Assets/Resources";
    const string PrefabPath = "Assets/Resources/NetworkPlayer.prefab";

    static MultiplayerPrefabSetup()
    {
        EditorApplication.delayCall += EnsureNetworkPlayerPrefab;
    }

    [MenuItem("CoreWar/Multiplayer/Rebuild Network Player Prefab")]
    public static void RebuildNetworkPlayerPrefab()
    {
        EnsureNetworkPlayerPrefab(force: true);
    }

    static void EnsureNetworkPlayerPrefab()
    {
        EnsureNetworkPlayerPrefab(force: false);
    }

    static void EnsureNetworkPlayerPrefab(bool force)
    {
        if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(ResourcesPath))
        {
            if (Directory.Exists(ResourcesPath))
            {
                AssetDatabase.ImportAsset(ResourcesPath);
            }
            else
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
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

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Multiplayer] Network player prefab ready: {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
