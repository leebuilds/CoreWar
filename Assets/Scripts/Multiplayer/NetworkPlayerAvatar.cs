using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Ownership and presentation bridge for the existing runtime-created player controller.
/// </summary>
[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
[RequireComponent(typeof(PlayerHealth), typeof(ThirdPersonController))]
public class NetworkPlayerAvatar : NetworkBehaviour
{
    readonly NetworkVariable<float> _aimYaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    readonly NetworkVariable<float> _aimPitch = new NetworkVariable<float>(
        18f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    readonly NetworkVariable<int> _teamIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    readonly NetworkVariable<int> _jerseyNumber = new NetworkVariable<int>(
        7,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    ThirdPersonController _controller;
    Camera _camera;
    AudioListener _audioListener;
    Transform _visualRoot;
    int _builtTeam = -1;
    int _builtJersey = -1;

    public bool IsServerInstance => IsSpawned && IsServer;
    public GameSession.Team PlayerTeam => (GameSession.Team)Mathf.Clamp(_teamIndex.Value, 0, 3);

    void Awake()
    {
        EnsureComponents();
        EnsurePresentationObjects();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _aimYaw.OnValueChanged += HandleAimChanged;
        _aimPitch.OnValueChanged += HandleAimChanged;
        _teamIndex.OnValueChanged += HandleTeamChanged;
        _jerseyNumber.OnValueChanged += HandleTeamChanged;

        if (IsServer)
        {
            _aimYaw.Value = transform.eulerAngles.y;
            _aimPitch.Value = 18f;
        }

        ApplyTeamVisual();
        ApplyOwnershipPresentation();
        Debug.Log($"[Multiplayer] Player spawned. Owner={OwnerClientId}, LocalOwner={IsOwner}");
    }

    public override void OnNetworkDespawn()
    {
        _aimYaw.OnValueChanged -= HandleAimChanged;
        _aimPitch.OnValueChanged -= HandleAimChanged;
        _teamIndex.OnValueChanged -= HandleTeamChanged;
        _jerseyNumber.OnValueChanged -= HandleTeamChanged;
        base.OnNetworkDespawn();
    }

    void LateUpdate()
    {
        if (!IsSpawned || _controller == null)
        {
            return;
        }

        if (IsOwner)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(_aimYaw.Value, _controller.NetworkAimYaw)) > 0.1f)
            {
                _aimYaw.Value = _controller.NetworkAimYaw;
            }

            if (Mathf.Abs(_aimPitch.Value - _controller.NetworkAimPitch) > 0.1f)
            {
                _aimPitch.Value = _controller.NetworkAimPitch;
            }
        }
        else
        {
            _controller.SetRemoteAim(_aimYaw.Value, _aimPitch.Value);
        }
    }

    public void ServerPrepare(ulong ownerClientId, int spawnIndex)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        _teamIndex.Value = GameSession.RequiredPlayers <= 2
            ? Mathf.Clamp(spawnIndex, 0, 1)
            : Mathf.Abs(spawnIndex) % 4;
        _jerseyNumber.Value = Mathf.Clamp((int)((ownerClientId + 7) % 100), 1, 99);
    }

    public void RequestProjectileFire(Vector3 spawnPosition, Vector3 direction, float muzzleSpeed,
        ProjectileWeaponType weaponType)
    {
        if (!IsSpawned || !IsOwner)
        {
            return;
        }

        direction = direction.sqrMagnitude <= 0.0001f ? transform.forward : direction.normalized;
        if (!IsServer)
        {
            SpawnVisualProjectile(spawnPosition, direction, muzzleSpeed, weaponType);
        }

        FireProjectileServerRpc(spawnPosition, direction, muzzleSpeed, weaponType);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void FireProjectileServerRpc(Vector3 spawnPosition, Vector3 direction, float muzzleSpeed,
        ProjectileWeaponType weaponType)
    {
        direction = direction.sqrMagnitude <= 0.0001f ? transform.forward : direction.normalized;
        SpawnAuthoritativeProjectile(spawnPosition, direction, muzzleSpeed, weaponType);
        SpawnProjectileVisualRpc(spawnPosition, direction, muzzleSpeed, weaponType);
    }

    [Rpc(SendTo.NotServer)]
    void SpawnProjectileVisualRpc(Vector3 spawnPosition, Vector3 direction, float muzzleSpeed,
        ProjectileWeaponType weaponType)
    {
        if (IsOwner)
        {
            return;
        }

        SpawnVisualProjectile(spawnPosition, direction, muzzleSpeed, weaponType);
    }

    void SpawnAuthoritativeProjectile(Vector3 spawnPosition, Vector3 direction, float muzzleSpeed,
        ProjectileWeaponType weaponType)
    {
        var bullet = new GameObject("Projectile Bullet");
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        bullet.AddComponent<ProjectileBullet>().Initialize(direction * muzzleSpeed, weaponType, gameObject);
    }

    static void SpawnVisualProjectile(Vector3 spawnPosition, Vector3 direction, float muzzleSpeed,
        ProjectileWeaponType weaponType)
    {
        var bullet = new GameObject("Projectile Bullet Visual");
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        bullet.AddComponent<ProjectileBullet>().InitializeVisualOnly(direction * muzzleSpeed, weaponType);
    }

    void EnsureComponents()
    {
        _controller = GetComponent<ThirdPersonController>();

        var rb = GetComponent<Rigidbody>();
        rb.mass = 70f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var capsule = GetComponent<CapsuleCollider>();
        capsule.height = 1.8f;
        capsule.radius = 0.35f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        var networkTransform = GetComponent<NetworkTransform>();
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
    }

    void EnsurePresentationObjects()
    {
        if (_visualRoot == null)
        {
            var existing = transform.Find("Character Visual");
            _visualRoot = existing != null ? existing : new GameObject("Character Visual").transform;
            _visualRoot.SetParent(transform, false);
        }

        Transform cameraRoot = transform.Find("Camera Rig");
        if (cameraRoot == null)
        {
            cameraRoot = new GameObject("Camera Rig").transform;
            cameraRoot.SetParent(transform, false);
        }

        Transform yawPivot = cameraRoot.Find("Camera Yaw Pivot");
        if (yawPivot == null)
        {
            yawPivot = new GameObject("Camera Yaw Pivot").transform;
            yawPivot.SetParent(cameraRoot, false);
        }

        Transform pitchPivot = yawPivot.Find("Camera Pitch Pivot");
        if (pitchPivot == null)
        {
            pitchPivot = new GameObject("Camera Pitch Pivot").transform;
            pitchPivot.SetParent(yawPivot, false);
        }

        Transform cameraTransform = pitchPivot.Find("Main Camera");
        if (cameraTransform == null)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraTransform = cameraObject.transform;
            cameraTransform.SetParent(pitchPivot, false);
        }

        _camera = cameraTransform.GetComponent<Camera>() ?? cameraTransform.gameObject.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.92f, 0.94f, 0.96f);
        _camera.farClipPlane = GameSession.IsShootingRange ? 750f : 500f;
        _camera.nearClipPlane = 0.03f;

        _audioListener = cameraTransform.GetComponent<AudioListener>() ??
            cameraTransform.gameObject.AddComponent<AudioListener>();

        var penEffect = cameraTransform.GetComponent<PenInkShadowEffect>() ??
            cameraTransform.gameObject.AddComponent<PenInkShadowEffect>();
        var activeWorld = NetworkPlayerSpawner.ActiveVoxelWorld != null
            ? NetworkPlayerSpawner.ActiveVoxelWorld
            : VoxelLightingWorld.Active;
        penEffect.voxelSize = activeWorld != null
            ? activeWorld.VoxelSize
            : 1f;
        penEffect.shadowThreshold = 0.26f;
        penEffect.centerDarkness = 0.78f;
        penEffect.circularFalloff = 3.2f;
        penEffect.topSurfaceThreshold = 0.84f;
        penEffect.hatchScale = 24f;
        penEffect.paperBlend = 0.03f;
        penEffect.inkColor = new Color(0.34f, 0.34f, 0.36f, 1f);
        penEffect.paperTint = new Color(0.985f, 0.985f, 0.985f, 1f);

        if (cameraTransform.GetComponent<SniperScopePostEffect>() == null)
        {
            cameraTransform.gameObject.AddComponent<SniperScopePostEffect>();
        }

        _controller.viewCamera = _camera;
        _controller.cameraYawPivot = yawPivot;
        _controller.cameraPitchPivot = pitchPivot;
        _controller.characterVisual = _visualRoot;
        _controller.voxelWorld = activeWorld;
        _controller.deferStartUntilNetworkSpawn = true;
    }

    void ApplyOwnershipPresentation()
    {
        EnsurePresentationObjects();

        bool isLocalOwner = IsOwner;
        _controller.hideLocalCharacterVisual = isLocalOwner;
        if (_camera != null)
        {
            _camera.enabled = isLocalOwner;
            var penEffect = _camera.GetComponent<PenInkShadowEffect>();
            if (penEffect != null)
            {
                penEffect.enabled = isLocalOwner;
            }

            var scopeEffect = _camera.GetComponent<SniperScopePostEffect>();
            if (scopeEffect != null)
            {
                scopeEffect.enabled = isLocalOwner;
                if (isLocalOwner)
                {
                    scopeEffect.ClaimAsLocalInstance();
                }
            }
        }

        if (_audioListener != null)
        {
            _audioListener.enabled = isLocalOwner;
        }

        _controller.InitializeNetworkController(isLocalOwner);

        if (!isLocalOwner)
        {
            _controller.SetRemoteAim(_aimYaw.Value, _aimPitch.Value);
        }
    }

    void ApplyTeamVisual()
    {
        if (_visualRoot == null)
        {
            return;
        }

        int teamIndex = Mathf.Clamp(_teamIndex.Value, 0, 3);
        int jersey = Mathf.Clamp(_jerseyNumber.Value, 1, 99);
        if (_builtTeam == teamIndex && _builtJersey == jersey && _visualRoot.childCount > 0)
        {
            return;
        }

        for (int i = _visualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_visualRoot.GetChild(i).gameObject);
        }

        var robot = _visualRoot.GetComponent<CapsuleRobotVisual>() ??
            _visualRoot.gameObject.AddComponent<CapsuleRobotVisual>();
        var team = (GameSession.Team)teamIndex;
        robot.Build(team, jersey);
        if (_controller != null)
        {
            _controller.SetNetworkTeam(team);
        }

        _builtTeam = teamIndex;
        _builtJersey = jersey;
    }

    void HandleAimChanged(float previous, float current)
    {
        if (!IsOwner && _controller != null)
        {
            _controller.SetRemoteAim(_aimYaw.Value, _aimPitch.Value);
        }
    }

    void HandleTeamChanged(int previous, int current)
    {
        ApplyTeamVisual();
        if (IsOwner)
        {
            GameSession.SetLocalTeam(PlayerTeam);
        }
    }
}
