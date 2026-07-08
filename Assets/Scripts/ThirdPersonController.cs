using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First-person Rigidbody controller with camera-relative movement and grid building.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    static readonly VoxelLightingWorld.BuildPieceType[] BuildPieceOptions =
    {
        VoxelLightingWorld.BuildPieceType.Wall,
        VoxelLightingWorld.BuildPieceType.Window,
        VoxelLightingWorld.BuildPieceType.Ceiling,
        VoxelLightingWorld.BuildPieceType.Door,
        VoxelLightingWorld.BuildPieceType.TrapDoor,
        VoxelLightingWorld.BuildPieceType.Ladder
    };

    static readonly Vector3Int[] BuildFaceNormals =
    {
        new Vector3Int(0, 0, 1),
        Vector3Int.right,
        new Vector3Int(0, 0, -1),
        Vector3Int.left
    };

    static readonly Vector3Int[] HorizontalAxes =
    {
        Vector3Int.right,
        new Vector3Int(0, 0, 1)
    };

    [Header("References")]
    public Camera viewCamera;
    public Transform cameraYawPivot;
    public Transform cameraPitchPivot;
    public Transform characterVisual;
    public VoxelLightingWorld voxelWorld;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;
    public float airAcceleration = 14f;
    public float jumpVelocity = 6f;
    public float lookSensitivity = 2.8f;
    public float turnSpeed = 12f;

    [Header("Camera")]
    public Vector3 firstPersonCameraOffset = Vector3.zero;
    public float eyeHeight = 1.62f;
    public float fieldOfView = 75f;
    public bool hideLocalCharacterVisual = true;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Building")]
    public float buildRange = 8f;
    public int buildSnapRadius = 2;
    public int rectangleMaxCells = 12;

    [Header("Tools")]
    public float hammerRangeVoxels = 1.5f;
    public float pistolBulletSpeed = 325f;
    public float assaultRifleBulletSpeed = 850f;
    public float assaultRifleRpm = 400f;
    public float sniperBulletSpeed = 950f;
    public float sniperFireCooldownSeconds = 1.15f;
    public float adsIronSightFov = 55f;
    public float ads4xFov = 19f;
    public float ads10xFov = 7.5f;
    public float adsIronTransitionSeconds = 0.07f;
    public float ads4xTransitionSeconds = 0.38f;
    public float ads10xTransitionSeconds = 0.58f;
    public float adsExitTransitionSeconds = 0.12f;
    public float sniperScopeSwapDropSeconds = 0.16f;
    public float infantrySpeedBoostMultiplier = 1.35f;
    public float infantrySpeedBoostDurationSeconds = 10f;
    public float infantrySpeedBoostCooldownSeconds = 30f;
    public float sniperHipFireCrosshairGap = 36f;
    public float sniperHipFireCrosshairLength = 32f;
    public float sniperScopeSwayDegrees = 0.12f;
    public float sniperAdsMinSpreadPixels = 1.5f;
    public float gunRecoilVerticalRandomness = 3.2f;
    public float gunRecoilHorizontalRandomness = 0.18f;
    public float gunRecoilKickDuration = 0.11f;
    public float gunMuzzleForwardOffset = 0.55f;
    public float reloadDipDegrees = 7f;
    public float sniperRoundPulseDuration = 0.14f;
    public float sniperRoundPulseHeight = 0.045f;
    public float sniperRoundPulsePitch = 9f;
    public float pistolDrawSeconds = 0.6f;
    public float assaultRifleDrawSeconds = 1.1f;
    public float sniperDrawSeconds = 2f;
    public float weaponDrawHiddenLocalY = -0.95f;

    [Header("Reticle")]
    public float crosshairGap = 5f;
    public float crosshairLength = 10f;
    public float weaponCrosshairGap = 3f;
    public float weaponCrosshairLength = 6f;
    public float crosshairThickness = 2f;
    public Color crosshairColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    public Color redDotColor = new Color(0.92f, 0.12f, 0.1f, 0.95f);
    public float redDotSize = 6f;
    public Color scopeLabelColor = new Color(0.92f, 0.12f, 0.1f, 0.98f);
    public Color scopeLabelPanelColor = new Color(0.04f, 0.04f, 0.04f, 0.72f);

    [Header("Build Mode")]
    public Color validPreviewColor = new Color(0.08f, 0.62f, 0.16f, 0.9f);
    public Color invalidPreviewColor = new Color(0.72f, 0.06f, 0.06f, 0.9f);
    public float selectorMouseScale = 20f;
    public float selectorActivationDistance = 18f;
    public float selectorRadius = 84f;

    Rigidbody _rb;
    CapsuleCollider _capsule;
    float _yaw;
    float _pitch;
    float _baseLookSensitivity;
    bool _grounded;
    bool _selectorOpen;
    Vector2 _selectorDirection;
    CardKitDefinition _activeKit;
    int _selectedHotbarIndex;
    RespawnClassPicker _respawnPicker;
    ShootingRangeCharacterPicker _characterPicker;
    GamePauseMenu _pauseMenu;
    VoxelLightingWorld.BuildPieceType _selectedPiece = VoxelLightingWorld.BuildPieceType.Wall;
    VoxelLightingWorld.BuildPieceCandidate _buildCandidate;
    bool _hasBuildCandidate;
    bool _orientationLocked;
    bool _scrollTargetLocked;
    Vector3Int _scrollLockedCell;
    bool _mouseMovedThisFrame;
    GameObject _pistolRoot;
    GameObject _assaultRifleRoot;
    GameObject _sniperRifleRoot;
    GameObject _hammerRoot;
    GameObject _blueprintRoot;
    GameObject _pistolMuzzleFlashRoot;
    GameObject _assaultRifleMuzzleFlashRoot;
    GameObject _sniperMuzzleFlashRoot;
    float _weaponFireCooldown;
    bool _sniperAimingHeld;
    bool _sniperAdsActive;
    int _sniperScopeIndex = DefaultSniperMagnificationIndex;
    float _sniperDisplayedFov;
    float _sniperFovTransitionStart;
    float _sniperFovTransitionTarget;
    float _sniperFovTransitionElapsed;
    float _sniperFovTransitionDuration;
    float _sniperScopeOverlayBlend;
    int _sniperScopeSwapPhase;
    int _sniperPendingScopeIndex;
    float _sniperScopeSwapTimer;
    Vector2 _sniperScopeSway;
    float _baseCardMoveSpeed = 8f;
    float _abilityCooldownRemaining;
    float _speedBoostRemaining;
    float _gunKickTimer;
    float _muzzleFlashTimer;
    float _hammerSwingTimer;
    Vector2 _gunRecoilPeak;
    Vector2 _gunRecoilResidual;
    float _gunRecoilKickTimer;
    bool _gunRecoilAimApplied;
    float _sessionHeartbeat;
    bool _wasInPrepPhase;
    bool _wasPrepReady;
    WeaponAmmoPool _pistolAmmo;
    WeaponAmmoPool _assaultRifleAmmo;
    WeaponAmmoPool _sniperAmmo;
    bool _isReloading;
    CardHotbarTool _reloadWeapon;
    float _reloadTimer;
    int _sniperReloadPhase;
    bool _sniperReloadLocked;
    bool _suppressNextShotAfterReloadCancel;
    float _reloadDipBlend;
    float _sniperRoundPulseTimer;
    bool _blockWeaponFireUntilMouseRelease;
    bool _weaponMouseHeldDuringPrep;
    CardHotbarTool _drawingWeapon;
    float _weaponDrawTimer;
    float _weaponDrawDuration;
    Vector3 _initialSpawnPosition;
    readonly Collider[] _respawnOverlapBuffer = new Collider[32];
    readonly List<GameObject> _previewRoots = new List<GameObject>();
    readonly List<VoxelLightingWorld.BuildPieceCandidate> _rectangleCandidates =
        new List<VoxelLightingWorld.BuildPieceCandidate>();
    readonly List<VoxelLightingWorld.BuildPieceCandidate> _rectanglePlaceCandidates =
        new List<VoxelLightingWorld.BuildPieceCandidate>();
    bool[] _rectangleValidFlags = new bool[0];
    bool _rectangleDragActive;
    bool _rectangleAllValid;
    VoxelLightingWorld.BuildPieceCandidate _rectangleStartCandidate;
    VoxelLightingWorld.BuildPieceCandidate _rectangleEndCandidate;
    Vector3Int _rectangleFaceNormal;
    Vector3Int _rectangleWidthAxis;
    int _buildOrientationIndex;
    Texture2D _radialTexture;
    VoxelLightingWorld.BuildPieceType _radialTexturePiece;
    PhysicsMaterial _slipperyMaterial;

    const int DefaultSniperMagnificationIndex = 1;

    bool BuildModeActive => SelectedTool == CardHotbarTool.Blueprint;

    CardHotbarTool SelectedTool =>
        _activeKit == null ? CardHotbarTool.AssaultRifle : _activeKit.GetToolAt(_selectedHotbarIndex);

    int HotbarSlotCount => _activeKit == null ? 4 : _activeKit.SlotCount;

    float AssaultRifleFireInterval => 60f / Mathf.Max(1f, assaultRifleRpm);

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _baseLookSensitivity = lookSensitivity;

        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        _slipperyMaterial = new PhysicsMaterial("PlayerSlide")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        _capsule.material = _slipperyMaterial;

        _yaw = transform.eulerAngles.y;
        _pitch = 18f;

        MenuSettings.EnsureLoaded();
        ApplyMenuSettings();
        MenuSettings.Changed += ApplyMenuSettings;
    }

    void OnDestroy()
    {
        MenuSettings.Changed -= ApplyMenuSettings;
        ProfileSession.TouchActivity();
    }

    void ApplyMenuSettings()
    {
        lookSensitivity = _baseLookSensitivity * MenuSettings.LookSensitivity;
    }

    float CurrentLookSensitivity()
    {
        float sensitivity = lookSensitivity;
        if (SelectedTool == CardHotbarTool.SniperRifle && _sniperAimingHeld)
        {
            float zoomFactor = Mathf.Clamp(_sniperDisplayedFov / Mathf.Max(1f, fieldOfView), 0.08f, 1f);
            sensitivity *= MenuSettings.AdsSensitivity * zoomFactor;
        }

        return sensitivity;
    }

    void LateUpdate()
    {
        if (!GameSession.IsMatchActive || !SceneFlow.IsGameActive)
        {
            return;
        }

        if (IsUiOverlayBlocking())
        {
            _selectorOpen = false;
            ExitSniperAds();
            HidePreviewRoots();
            UpdateCameraTransform();
            return;
        }

        if (GameSession.IsInPrepPhase && !GameSession.IsPrepReady)
        {
            UpdateCameraTransform();
            return;
        }

        if (GameSession.IsInPrepPhase && GameSession.IsPrepReady)
        {
            UpdateCameraTransform();
            UpdateHeldToolVisuals();
            UpdateCharacterAim();
            return;
        }

        UpdateCameraTransform();
        UpdateHeldToolVisuals();
        UpdateCharacterAim();
        UpdateBuildPreview();
    }

    void Start()
    {
        _initialSpawnPosition = transform.position;

        if (viewCamera != null)
        {
            viewCamera.fieldOfView = fieldOfView;
            _sniperDisplayedFov = fieldOfView;
            _sniperFovTransitionTarget = fieldOfView;
            _sniperFovTransitionStart = fieldOfView;
        }

        if (hideLocalCharacterVisual && characterVisual != null)
        {
            foreach (var renderer in characterVisual.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }

        ApplyKitFromSession();
        ResetAmmoPools();
        _weaponMouseHeldDuringPrep = false;
        _blockWeaponFireUntilMouseRelease = false;
        _sniperScopeIndex = DefaultSniperMagnificationIndex;
        ProfileSession.EnsureInitialized();
        ProfileSession.TouchActivity();
        _wasInPrepPhase = GameSession.IsInPrepPhase;
        _wasPrepReady = GameSession.IsPrepReady;

        _respawnPicker = RespawnClassPicker.Create(transform, cardId =>
        {
            GameSession.SetActiveCard(cardId);
            ApplyKitFromSession();
            RespawnAtValidMapPosition();
        });

        if (GameSession.IsShootingRange)
        {
            _characterPicker = ShootingRangeCharacterPicker.Create(transform, cardId =>
            {
                GameSession.SetActiveCard(cardId);
                ApplyKitFromSession();
                ResetToSpawn();
            });
        }

        _pauseMenu = GamePauseMenu.Create(transform, _respawnPicker, _characterPicker, this);

        CreateHeldToolVisuals();
        RefreshHeldToolVisibility();
        BeginWeaponDraw(SelectedTool);
    }

    void ApplyKitFromSession()
    {
        _activeKit = GameSession.ActiveKit ?? CardKitDefinition.DefaultInfantryPlaceholder();
        _selectedHotbarIndex = Mathf.Clamp(_selectedHotbarIndex, 0, Mathf.Max(0, HotbarSlotCount - 1));
        ExitSniperAds();
        RefreshCardMoveSpeed();
        ResetAbilityState();
        RefreshHeldToolVisibility();
    }

    void RespawnAtValidMapPosition()
    {
        if (_rb == null || _capsule == null || voxelWorld == null ||
            !TryFindValidRespawnPosition(out var respawnPosition))
        {
            return;
        }

        ApplySpawnReset(respawnPosition);
    }

    public void ResetToSpawn()
    {
        if (GameSession.IsShootingRange)
        {
            ApplySpawnReset(ShootingRangeSession.PlayerSpawnPosition);
            return;
        }

        RespawnAtValidMapPosition();
    }

    void ApplySpawnReset(Vector3 respawnPosition)
    {
        _selectorOpen = false;
        _rectangleDragActive = false;
        _scrollTargetLocked = false;
        _hasBuildCandidate = false;
        HidePreviewRoots();

        _gunKickTimer = 0f;
        _muzzleFlashTimer = 0f;
        _hammerSwingTimer = 0f;
        _gunRecoilPeak = Vector2.zero;
        _gunRecoilResidual = Vector2.zero;
        _gunRecoilKickTimer = 0f;
        _gunRecoilAimApplied = true;
        ExitSniperAds();
        _sniperScopeIndex = DefaultSniperMagnificationIndex;
        ResetAbilityState();
        ResetAmmoPools();
        CancelReload();
        SelectHotbarIndex(0);

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.position = respawnPosition;
        transform.position = respawnPosition;
        _grounded = false;
        Physics.SyncTransforms();

        GetComponent<PlayerHealth>()?.RefillHealth();
    }

    bool TryFindValidRespawnPosition(out Vector3 respawnPosition)
    {
        respawnPosition = _initialSpawnPosition;
        Physics.SyncTransforms();

        int width = voxelWorld.GridWidth;
        int length = voxelWorld.GridLength;
        if (width <= 0 || length <= 0)
        {
            return false;
        }

        Vector3Int preferredCell = voxelWorld.WorldToCell(_initialSpawnPosition);
        preferredCell.x = Mathf.Clamp(preferredCell.x, 0, width - 1);
        preferredCell.z = Mathf.Clamp(preferredCell.z, 0, length - 1);

        int maxRadius = Mathf.Max(width, length);
        for (int radius = 0; radius <= maxRadius; radius++)
        {
            int minX = Mathf.Max(0, preferredCell.x - radius);
            int maxX = Mathf.Min(width - 1, preferredCell.x + radius);
            int minZ = Mathf.Max(0, preferredCell.z - radius);
            int maxZ = Mathf.Min(length - 1, preferredCell.z + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (radius > 0 &&
                        Mathf.Abs(x - preferredCell.x) < radius &&
                        Mathf.Abs(z - preferredCell.z) < radius)
                    {
                        continue;
                    }

                    Vector3 cellCenter = voxelWorld.CellToWorld(new Vector3Int(x, 0, z));
                    float groundTop = cellCenter.y + (voxelWorld.VoxelSize * 0.5f);
                    var candidate = new Vector3(
                        cellCenter.x,
                        groundTop + Mathf.Max(0.05f, voxelWorld.VoxelSize * 0.05f),
                        cellCenter.z);

                    if (IsRespawnCapsuleClear(candidate))
                    {
                        respawnPosition = candidate;
                        return true;
                    }
                }
            }
        }

        // If construction has obstructed every ground tile, re-enter above the
        // preferred map cell and fall onto the highest available surface.
        Vector3 preferredCenter = voxelWorld.CellToWorld(new Vector3Int(preferredCell.x, 0, preferredCell.z));
        float fallbackHeight = preferredCenter.y +
            ((voxelWorld.MaxBuildHeight + 4f) * voxelWorld.VoxelSize);
        var fallback = new Vector3(preferredCenter.x, fallbackHeight, preferredCenter.z);
        if (!IsRespawnCapsuleClear(fallback))
        {
            return false;
        }

        respawnPosition = fallback;
        return true;
    }

    bool IsRespawnCapsuleClear(Vector3 candidatePosition)
    {
        Vector3 scale = transform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        Vector3 axis;
        float axisScale;
        float radiusScale;
        switch (_capsule.direction)
        {
            case 0:
                axis = transform.right;
                axisScale = scale.x;
                radiusScale = Mathf.Max(scale.y, scale.z);
                break;
            case 2:
                axis = transform.forward;
                axisScale = scale.z;
                radiusScale = Mathf.Max(scale.x, scale.y);
                break;
            default:
                axis = transform.up;
                axisScale = scale.y;
                radiusScale = Mathf.Max(scale.x, scale.z);
                break;
        }

        float radius = _capsule.radius * radiusScale;
        float height = Mathf.Max(_capsule.height * axisScale, radius * 2f);
        float segmentHalfLength = Mathf.Max(0f, (height * 0.5f) - radius);
        Vector3 scaledCenter = Vector3.Scale(_capsule.center, scale);
        Vector3 center = candidatePosition + (transform.rotation * scaledCenter);
        Vector3 pointA = center + (axis * segmentHalfLength);
        Vector3 pointB = center - (axis * segmentHalfLength);

        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            pointA,
            pointB,
            radius,
            _respawnOverlapBuffer,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = _respawnOverlapBuffer[i];
            if (overlap != null && overlap != _capsule && !overlap.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        // A full buffer means additional uninspected colliders may exist.
        return overlapCount < _respawnOverlapBuffer.Length;
    }

    void Update()
    {
        if (!GameSession.IsMatchActive || !SceneFlow.IsGameActive)
        {
            return;
        }

        if (_wasInPrepPhase && !GameSession.IsInPrepPhase)
        {
            ApplyKitFromSession();
            FinalizePrepWeaponInputGate();
        }
        else if (GameSession.IsPrepReady && !_wasPrepReady)
        {
            ApplyKitFromSession();
        }

        _wasInPrepPhase = GameSession.IsInPrepPhase;
        _wasPrepReady = GameSession.IsPrepReady;

        if (GameSession.IsInPrepPhase && Input.GetMouseButton(0))
        {
            _weaponMouseHeldDuringPrep = true;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_characterPicker != null && _characterPicker.IsOpen)
            {
                _characterPicker.Hide();
                return;
            }

            if (_respawnPicker != null && _respawnPicker.IsOpen)
            {
                _respawnPicker.Hide();
                return;
            }

            if (_pauseMenu != null && _pauseMenu.TryHandleEscape())
            {
                return;
            }

            if (_pauseMenu != null)
            {
                _pauseMenu.Toggle();
                return;
            }
        }

        if (_pauseMenu != null && _pauseMenu.IsOpen)
        {
            return;
        }

        if (_characterPicker != null && _characterPicker.IsOpen)
        {
            return;
        }

        if (_respawnPicker != null && _respawnPicker.IsOpen)
        {
            return;
        }

        UpdateSessionHeartbeat();

        if (GameSession.IsInPrepPhase && !GameSession.IsPrepReady)
        {
            return;
        }

        HandleLook();

        if (GameSession.IsInPrepPhase && GameSession.IsPrepReady)
        {
            UpdateReloadState();
            UpdateWeaponDrawTimer();
            HandleHotbarInput();
            HandleReloadInput();
            HandleAbilityInput();
            return;
        }

        UpdateReloadState();
        UpdateWeaponDrawTimer();
        HandleHotbarInput();
        HandleReloadInput();
        HandleAbilityInput();
        UpdateWeaponFireInputGate();
        HandleSelectedToolInput();

        if (Input.GetButtonDown("Jump") && CanJump())
        {
            var velocity = _rb.linearVelocity;
            velocity.y = jumpVelocity;
            _rb.linearVelocity = velocity;
        }
    }

    void FixedUpdate()
    {
        if (!GameSession.IsMatchActive || !SceneFlow.IsGameActive)
        {
            return;
        }

        if (IsMovementBlocked())
        {
            StopHorizontalMovement();
            return;
        }

        UpdateGrounded();
        HandleMovement();
    }

    bool IsUiOverlayBlocking()
    {
        return (_pauseMenu != null && _pauseMenu.IsOpen) ||
               (_respawnPicker != null && _respawnPicker.IsOpen) ||
               (_characterPicker != null && _characterPicker.IsOpen);
    }

    bool IsMovementBlocked()
    {
        return IsUiOverlayBlocking() || GameSession.IsInPrepPhase;
    }

    bool IsGameplayBlocked()
    {
        return IsMovementBlocked();
    }

    void StopHorizontalMovement()
    {
        var velocity = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    void UpdateSessionHeartbeat()
    {
        _sessionHeartbeat += Time.unscaledDeltaTime;
        if (_sessionHeartbeat < 300f)
        {
            return;
        }

        _sessionHeartbeat = 0f;
        ProfileSession.TouchActivity();
    }

    void HandleLook()
    {
        Vector2 lookDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        _mouseMovedThisFrame = lookDelta.sqrMagnitude > 0.0001f;

        if (_selectorOpen)
        {
            return;
        }

        _yaw += lookDelta.x * CurrentLookSensitivity();
        _pitch = Mathf.Clamp(_pitch - lookDelta.y * CurrentLookSensitivity(), minPitch, maxPitch);
    }

    void UpdateCameraTransform()
    {
        if (cameraYawPivot == null || cameraPitchPivot == null || viewCamera == null)
        {
            return;
        }

        cameraYawPivot.position = transform.position + Vector3.up * eyeHeight;
        cameraYawPivot.rotation = Quaternion.Euler(0f, _yaw, 0f);
        cameraPitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        viewCamera.transform.localPosition = firstPersonCameraOffset;
        UpdateSniperScopeSway();
        viewCamera.transform.localRotation = CurrentGunRecoilRotation() *
            Quaternion.Euler(_sniperScopeSway.y, _sniperScopeSway.x, 0f);
    }

    void UpdateSniperScopeSway()
    {
        _sniperScopeSway = Vector2.zero;
        if (SelectedTool != CardHotbarTool.SniperRifle || !_sniperAimingHeld || viewCamera == null || _rb == null)
        {
            return;
        }

        var horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float moveRatio = Mathf.Clamp01(horizontalVelocity.magnitude / Mathf.Max(0.01f, moveSpeed));
        if (moveRatio <= 0.01f)
        {
            return;
        }

        float sway = sniperScopeSwayDegrees * moveRatio;
        _sniperScopeSway = new Vector2(
            Mathf.Sin(Time.time * 7.6f) * sway,
            Mathf.Cos(Time.time * 5.9f) * sway * 0.75f);
    }

    Quaternion CurrentGunRecoilRotation()
    {
        if (_gunRecoilKickTimer <= 0f || _gunRecoilPeak.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        float duration = Mathf.Max(0.001f, gunRecoilKickDuration);
        float normalizedTime = 1f - Mathf.Clamp01(_gunRecoilKickTimer / duration);
        float verticalScale = RecoilVerticalVisualScale(normalizedTime);
        float horizontalScale = RecoilHorizontalVisualScale(normalizedTime);
        return Quaternion.Euler(
            -_gunRecoilPeak.y * verticalScale,
            _gunRecoilPeak.x * horizontalScale,
            0f);
    }

    float RecoilVerticalVisualScale(float normalizedTime)
    {
        if (normalizedTime <= 0.5f)
        {
            return Mathf.Sin(normalizedTime * Mathf.PI);
        }

        float peakY = Mathf.Max(0.001f, _gunRecoilPeak.y);
        float settleRatio = _gunRecoilResidual.y / peakY;
        float downT = (normalizedTime - 0.5f) * 2f;
        return Mathf.Lerp(1f, settleRatio, downT);
    }

    float RecoilHorizontalVisualScale(float normalizedTime)
    {
        if (normalizedTime <= 0.5f)
        {
            return Mathf.Sin(normalizedTime * Mathf.PI);
        }

        return 1f;
    }

    Ray BuildCenterAimRay()
    {
        Vector3 origin = cameraYawPivot != null
            ? cameraYawPivot.position
            : transform.position + Vector3.up * eyeHeight;
        Vector3 forward = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(_pitch, 0f, 0f) * Vector3.forward;
        return new Ray(origin, forward);
    }

    void ApplyGunRecoilToAim()
    {
        _yaw += _gunRecoilResidual.x;
        _pitch = Mathf.Clamp(_pitch - _gunRecoilResidual.y, minPitch, maxPitch);
    }

    void UpdateCharacterAim()
    {
        if (characterVisual == null || viewCamera == null)
        {
            return;
        }

        Vector3 aimDirection = viewCamera.transform.forward;
        aimDirection.y = 0f;
        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        characterVisual.rotation = Quaternion.Slerp(characterVisual.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void HandleMovement()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 wishDirection = Vector3.zero;

        if (viewCamera != null && input.sqrMagnitude > 0.0001f)
        {
            Vector3 camForward = viewCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = viewCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();
            wishDirection = camForward * input.y + camRight * input.x;
            if (wishDirection.sqrMagnitude > 1f)
            {
                wishDirection.Normalize();
            }
        }

        wishDirection = ProjectAgainstWall(wishDirection);
        var targetHorizontal = wishDirection * moveSpeed;

        var velocity = _rb.linearVelocity;
        var horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float accel = _grounded ? acceleration : airAcceleration;
        horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, accel * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        // Character facing follows the crosshair in LateUpdate.
    }

    void UpdateGrounded()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        _grounded = Physics.CapsuleCast(
            top, bottom, radius, Vector3.down, out _, 0.08f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    bool CanJump() => _grounded && !HasCeilingClearanceBlocked();

    bool HasCeilingClearanceBlocked()
    {
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.92f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        return Physics.CapsuleCast(
            top, bottom, radius, Vector3.up, out _, 0.12f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    Vector3 ProjectAgainstWall(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return direction;
        }

        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);
        float half = Mathf.Max(0f, (_capsule.height * 0.5f) - _capsule.radius);
        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        Vector3 top = worldCenter + Vector3.up * half;
        Vector3 bottom = worldCenter - Vector3.up * half;

        if (Physics.CapsuleCast(top, bottom, radius, direction, out var hit, 0.3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y < 0.35f && hit.normal.y > -0.2f)
            {
                return Vector3.ProjectOnPlane(direction, hit.normal).normalized;
            }
        }

        return direction;
    }

    void ResetAmmoPools()
    {
        _pistolAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.PistolStartReserve,
            WeaponAmmoDefaults.PistolMagSize,
            WeaponAmmoDefaults.PistolMagSize,
            WeaponAmmoDefaults.PistolMaxTotal);
        _assaultRifleAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.AssaultRifleStartReserve,
            WeaponAmmoDefaults.AssaultRifleMagSize,
            WeaponAmmoDefaults.AssaultRifleMagSize,
            WeaponAmmoDefaults.AssaultRifleMaxTotal);
        _sniperAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.SniperStartReserve,
            WeaponAmmoDefaults.SniperMagSize,
            WeaponAmmoDefaults.SniperMagSize,
            WeaponAmmoDefaults.SniperMaxTotal);
    }

    ref WeaponAmmoPool GetAmmoPoolRef(CardHotbarTool weapon)
    {
        switch (weapon)
        {
            case CardHotbarTool.AssaultRifle:
                return ref _assaultRifleAmmo;
            case CardHotbarTool.SniperRifle:
                return ref _sniperAmmo;
            default:
                return ref _pistolAmmo;
        }
    }

    WeaponAmmoPool GetAmmoPoolForSelectedTool()
    {
        switch (SelectedTool)
        {
            case CardHotbarTool.AssaultRifle:
                return _assaultRifleAmmo;
            case CardHotbarTool.SniperRifle:
                return _sniperAmmo;
            case CardHotbarTool.Pistol:
                return _pistolAmmo;
            default:
                return default;
        }
    }

    static bool IsFirearmTool(CardHotbarTool tool)
    {
        return tool == CardHotbarTool.AssaultRifle ||
            tool == CardHotbarTool.Pistol ||
            tool == CardHotbarTool.SniperRifle;
    }

    bool IsReloadFullyLocked()
    {
        return _isReloading && (_reloadWeapon != CardHotbarTool.SniperRifle || _sniperReloadLocked);
    }

    float ReloadOverlayFill()
    {
        if (!_isReloading)
        {
            return 0f;
        }

        switch (_reloadWeapon)
        {
            case CardHotbarTool.Pistol:
                return WeaponAmmoDefaults.PistolReloadSeconds <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / WeaponAmmoDefaults.PistolReloadSeconds);
            case CardHotbarTool.AssaultRifle:
                return WeaponAmmoDefaults.AssaultRifleReloadSeconds <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / WeaponAmmoDefaults.AssaultRifleReloadSeconds);
            case CardHotbarTool.SniperRifle:
                if (_sniperReloadPhase == 0)
                {
                    float lockedTotal = WeaponAmmoDefaults.SniperReloadStartSeconds +
                        WeaponAmmoDefaults.SniperRoundReloadSeconds;
                    float remaining = _reloadTimer + WeaponAmmoDefaults.SniperRoundReloadSeconds;
                    return lockedTotal <= 0f ? 0f : Mathf.Clamp01(remaining / lockedTotal);
                }

                return WeaponAmmoDefaults.SniperRoundReloadSeconds <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / WeaponAmmoDefaults.SniperRoundReloadSeconds);
            default:
                return 0f;
        }
    }

    void HandleReloadInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
        {
            return;
        }

        if (_isReloading || _weaponFireCooldown > 0f || !IsFirearmTool(SelectedTool) || IsWeaponDrawInProgress())
        {
            return;
        }

        ref WeaponAmmoPool pool = ref GetAmmoPoolRef(SelectedTool);
        if (pool.IsMagFull || !pool.HasReserve)
        {
            return;
        }

        BeginReload(SelectedTool);
    }

    void BeginReload(CardHotbarTool weapon)
    {
        _isReloading = true;
        _reloadWeapon = weapon;
        _suppressNextShotAfterReloadCancel = false;

        if (weapon == CardHotbarTool.SniperRifle)
        {
            ExitSniperAds();
            _sniperReloadPhase = 0;
            _sniperReloadLocked = true;
            _reloadTimer = WeaponAmmoDefaults.SniperReloadStartSeconds;
            return;
        }

        _sniperReloadPhase = 0;
        _sniperReloadLocked = false;
        _reloadTimer = weapon == CardHotbarTool.Pistol
            ? WeaponAmmoDefaults.PistolReloadSeconds
            : WeaponAmmoDefaults.AssaultRifleReloadSeconds;
    }

    void CancelReload()
    {
        _isReloading = false;
        _reloadWeapon = default;
        _reloadTimer = 0f;
        _sniperReloadPhase = 0;
        _sniperReloadLocked = false;
        _sniperRoundPulseTimer = 0f;
    }

    void CompleteReload()
    {
        CancelReload();
    }

    bool ShouldShowReloadGunDip()
    {
        return _reloadWeapon != CardHotbarTool.SniperRifle || !_sniperAimingHeld;
    }

    void ApplyReloadDipToGun(CardHotbarTool weapon, ref Vector3 localPosition, ref Quaternion localRotation)
    {
        if (!_isReloading || _reloadWeapon != weapon || !ShouldShowReloadGunDip())
        {
            return;
        }

        float dip = _reloadDipBlend;
        localPosition += new Vector3(0f, -0.05f * dip, 0.02f * dip);
        localRotation *= Quaternion.Euler(reloadDipDegrees * dip, 0f, 0f);

        if (weapon != CardHotbarTool.SniperRifle)
        {
            return;
        }

        float pulse = SniperRoundPulseAmount();
        localPosition += new Vector3(0f, pulse, -0.015f * pulse);
        localRotation *= Quaternion.Euler(-sniperRoundPulsePitch * (pulse / Mathf.Max(0.001f, sniperRoundPulseHeight)), 0f, 0f);
    }

    float SniperRoundPulseAmount()
    {
        if (_sniperRoundPulseTimer <= 0f || sniperRoundPulseDuration <= 0f)
        {
            return 0f;
        }

        float normalized = 1f - (_sniperRoundPulseTimer / sniperRoundPulseDuration);
        return Mathf.Sin(normalized * Mathf.PI) * sniperRoundPulseHeight;
    }

    void TriggerSniperRoundPulse()
    {
        _sniperRoundPulseTimer = sniperRoundPulseDuration;
    }

    void UpdateReloadState()
    {
        if (_sniperRoundPulseTimer > 0f)
        {
            _sniperRoundPulseTimer = Mathf.Max(0f, _sniperRoundPulseTimer - Time.deltaTime);
        }

        float dipTarget = _isReloading && ShouldShowReloadGunDip() ? 1f : 0f;
        _reloadDipBlend = Mathf.MoveTowards(_reloadDipBlend, dipTarget, Time.deltaTime * 8f);

        if (!_isReloading)
        {
            return;
        }

        _reloadTimer -= Time.deltaTime;
        if (_reloadTimer > 0f)
        {
            return;
        }

        switch (_reloadWeapon)
        {
            case CardHotbarTool.Pistol:
            case CardHotbarTool.AssaultRifle:
                GetAmmoPoolRef(_reloadWeapon).FillMagFromReserve();
                CompleteReload();
                break;
            case CardHotbarTool.SniperRifle:
                AdvanceSniperReload();
                break;
        }
    }

    void AdvanceSniperReload()
    {
        if (_sniperReloadPhase == 0)
        {
            _sniperReloadPhase = 1;
            _reloadTimer = WeaponAmmoDefaults.SniperRoundReloadSeconds;
            return;
        }

        _sniperAmmo.LoadSingleRound();
        TriggerSniperRoundPulse();
        if (_sniperReloadLocked)
        {
            _sniperReloadLocked = false;
        }

        if (_sniperAmmo.NeedsReload)
        {
            _sniperReloadPhase = 1;
            _reloadTimer = WeaponAmmoDefaults.SniperRoundReloadSeconds;
            return;
        }

        CompleteReload();
    }

    void HandleHotbarInput()
    {
        if (IsReloadFullyLocked())
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f)
        {
            SelectHotbarIndex(NextHotbarIndex(1));
        }
        else if (scroll < -0.01f)
        {
            SelectHotbarIndex(NextHotbarIndex(-1));
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectHotbarIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectHotbarIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            SelectHotbarIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            SelectHotbarIndex(3);
        }
    }

    int NextHotbarIndex(int direction)
    {
        int slotCount = Mathf.Max(1, HotbarSlotCount);
        return (_selectedHotbarIndex + direction + slotCount) % slotCount;
    }

    void SelectHotbarIndex(int index)
    {
        index = Mathf.Clamp(index, 0, Mathf.Max(0, HotbarSlotCount - 1));
        if (_selectedHotbarIndex == index)
        {
            return;
        }

        bool wasBuilding = BuildModeActive;
        if (_isReloading && _reloadWeapon == CardHotbarTool.SniperRifle && !_sniperReloadLocked)
        {
            CancelReload();
        }

        _selectedHotbarIndex = index;
        _weaponFireCooldown = 0f;
        ExitSniperAds();
        if (wasBuilding || BuildModeActive)
        {
            ClearBuildInteractionState();
        }

        RefreshHeldToolVisibility();
        BeginWeaponDraw(SelectedTool);
    }

    void HandleAbilityInput()
    {
        UpdateAbilityTimers();

        if (IsReloadFullyLocked())
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        switch (ActiveCardSpecialty())
        {
            case "sniper":
                TrySniperScopeAbility();
                break;
            case "infantry":
                TryInfantrySpeedBoost();
                break;
        }
    }

    void UpdateAbilityTimers()
    {
        if (_speedBoostRemaining > 0f)
        {
            _speedBoostRemaining = Mathf.Max(0f, _speedBoostRemaining - Time.deltaTime);
            if (_speedBoostRemaining <= 0f)
            {
                ApplyCurrentMoveSpeed();
            }
        }

        if (_abilityCooldownRemaining > 0f)
        {
            _abilityCooldownRemaining = Mathf.Max(0f, _abilityCooldownRemaining - Time.deltaTime);
        }
    }

    void TrySniperScopeAbility()
    {
        if (SelectedTool != CardHotbarTool.SniperRifle || _sniperScopeSwapPhase != 0)
        {
            return;
        }

        int nextScopeIndex = (_sniperScopeIndex + 1) % 3;
        if (_sniperAimingHeld)
        {
            BeginSniperScopeSwap(nextScopeIndex);
            return;
        }

        _sniperScopeIndex = nextScopeIndex;
        RefreshHeldToolVisibility();
    }

    void TryInfantrySpeedBoost()
    {
        if (_speedBoostRemaining > 0f || _abilityCooldownRemaining > 0f)
        {
            return;
        }

        _speedBoostRemaining = infantrySpeedBoostDurationSeconds;
        _abilityCooldownRemaining = infantrySpeedBoostCooldownSeconds;
        ApplyCurrentMoveSpeed();
    }

    void RefreshCardMoveSpeed()
    {
        var card = CardCatalog.Get(GameSession.ActiveCardId);
        _baseCardMoveSpeed = card?.preview != null ? card.preview.moveSpeed : moveSpeed;
        ApplyCurrentMoveSpeed();
    }

    void ApplyCurrentMoveSpeed()
    {
        bool boostActive = _speedBoostRemaining > 0f && ActiveCardSpecialty() == "infantry";
        moveSpeed = boostActive
            ? _baseCardMoveSpeed * infantrySpeedBoostMultiplier
            : _baseCardMoveSpeed;
    }

    void ResetAbilityState()
    {
        _abilityCooldownRemaining = 0f;
        _speedBoostRemaining = 0f;
        ApplyCurrentMoveSpeed();
    }

    bool IsAbilityReady()
    {
        switch (ActiveCardSpecialty())
        {
            case "infantry":
                return _abilityCooldownRemaining <= 0f && _speedBoostRemaining <= 0f;
            case "sniper":
                return _sniperScopeSwapPhase == 0;
            default:
                return false;
        }
    }

    float AbilityCooldownOverlayFill()
    {
        switch (ActiveCardSpecialty())
        {
            case "infantry":
                if (_speedBoostRemaining > 0f)
                {
                    return Mathf.Clamp01(_speedBoostRemaining / infantrySpeedBoostDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / infantrySpeedBoostCooldownSeconds);
                }

                return 0f;
            case "sniper":
                return _sniperScopeSwapPhase != 0 ? 1f : 0f;
            default:
                return 0f;
        }
    }

    static string ActiveCardSpecialty()
    {
        return CardCatalog.Get(GameSession.ActiveCardId)?.specialty;
    }

    bool IsGameplayWeaponInputAllowed()
    {
        return !GameSession.IsInPrepPhase;
    }

    void FinalizePrepWeaponInputGate()
    {
        _blockWeaponFireUntilMouseRelease = _weaponMouseHeldDuringPrep;
        _weaponMouseHeldDuringPrep = false;
    }

    void UpdateWeaponFireInputGate()
    {
        if (!_blockWeaponFireUntilMouseRelease)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            _blockWeaponFireUntilMouseRelease = false;
        }
    }

    bool IsWeaponFireInputBlocked()
    {
        return !IsGameplayWeaponInputAllowed() ||
            _blockWeaponFireUntilMouseRelease ||
            IsWeaponDrawInProgress();
    }

    float WeaponDrawDuration(CardHotbarTool weapon)
    {
        switch (weapon)
        {
            case CardHotbarTool.Pistol:
                return pistolDrawSeconds;
            case CardHotbarTool.AssaultRifle:
                return assaultRifleDrawSeconds;
            case CardHotbarTool.SniperRifle:
                return sniperDrawSeconds;
            default:
                return 0f;
        }
    }

    void BeginWeaponDraw(CardHotbarTool weapon)
    {
        if (!IsFirearmTool(weapon))
        {
            _drawingWeapon = default;
            _weaponDrawTimer = 0f;
            _weaponDrawDuration = 0f;
            return;
        }

        _drawingWeapon = weapon;
        _weaponDrawDuration = WeaponDrawDuration(weapon);
        _weaponDrawTimer = _weaponDrawDuration;
    }

    void UpdateWeaponDrawTimer()
    {
        if (_weaponDrawTimer <= 0f)
        {
            return;
        }

        _weaponDrawTimer = Mathf.Max(0f, _weaponDrawTimer - Time.deltaTime);
    }

    bool IsWeaponDrawInProgress()
    {
        return IsFirearmTool(SelectedTool) &&
            _weaponDrawTimer > 0f &&
            _drawingWeapon == SelectedTool;
    }

    float WeaponDrawProgress(CardHotbarTool weapon)
    {
        if (_drawingWeapon != weapon || _weaponDrawDuration <= 0f)
        {
            return 1f;
        }

        float normalized = 1f - (_weaponDrawTimer / _weaponDrawDuration);
        normalized = Mathf.Clamp01(normalized);
        return normalized * normalized * (3f - (2f * normalized));
    }

    void ApplyWeaponDrawOffset(CardHotbarTool weapon, ref Vector3 localPosition)
    {
        float progress = WeaponDrawProgress(weapon);
        if (progress >= 0.999f)
        {
            return;
        }

        float hiddenBlend = 1f - progress;
        localPosition += new Vector3(0f, weaponDrawHiddenLocalY * hiddenBlend, 0.05f * hiddenBlend);
    }

    void HandleSelectedToolInput()
    {
        if (SelectedTool != CardHotbarTool.SniperRifle)
        {
            ExitSniperAds();
        }

        if (IsReloadFullyLocked())
        {
            if (SelectedTool == CardHotbarTool.SniperRifle && !IsWeaponDrawInProgress())
            {
                UpdateSniperAdsState();
            }

            return;
        }

        if (IsWeaponDrawInProgress())
        {
            return;
        }

        switch (SelectedTool)
        {
            case CardHotbarTool.AssaultRifle:
                HandleAssaultRifleInput();
                break;
            case CardHotbarTool.SniperRifle:
                HandleSniperRifleInput();
                break;
            case CardHotbarTool.Pistol:
                HandlePistolInput();
                break;
            case CardHotbarTool.Hammer:
                HandleHammerInput();
                break;
            case CardHotbarTool.Blueprint:
                HandleBuildingInput();
                break;
        }
    }

    void HandleAssaultRifleInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            _weaponFireCooldown = Mathf.Max(0f, _weaponFireCooldown - Time.deltaTime);
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(CardHotbarTool.AssaultRifle, assaultRifleBulletSpeed, 0.75f, ProjectileWeaponType.AssaultRifle))
        {
            return;
        }

        _weaponFireCooldown = AssaultRifleFireInterval;
    }

    void HandlePistolInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        TryFireWeapon(CardHotbarTool.Pistol, pistolBulletSpeed, 0.5f, ProjectileWeaponType.Pistol);
    }

    void HandleSniperRifleInput()
    {
        UpdateSniperAdsState();

        if (_weaponFireCooldown > 0f)
        {
            _weaponFireCooldown = Mathf.Max(0f, _weaponFireCooldown - Time.deltaTime);
        }

        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0) || _weaponFireCooldown > 0f)
        {
            return;
        }

        if (_isReloading)
        {
            if (!_sniperReloadLocked)
            {
                CancelReload();
                _suppressNextShotAfterReloadCancel = true;
            }

            return;
        }

        if (_suppressNextShotAfterReloadCancel)
        {
            _suppressNextShotAfterReloadCancel = false;
            return;
        }

        float recoilScale = _sniperAimingHeld ? 2.7f : 4.8f;
        if (!TryFireSniperWeapon(sniperBulletSpeed, recoilScale))
        {
            return;
        }

        _weaponFireCooldown = sniperFireCooldownSeconds;
    }

    bool TryFireSniperWeapon(float muzzleSpeed, float recoilScale)
    {
        if (!_sniperAmmo.CanFire)
        {
            return false;
        }

        FireWeapon(BuildSniperAimRay(), muzzleSpeed, recoilScale, ProjectileWeaponType.SniperRifle);
        _sniperAmmo.ConsumeRound();
        MenuUiSounds.PlayWeaponGunshot(ProjectileWeaponType.SniperRifle);
        return true;
    }

    bool TryFireWeapon(
        CardHotbarTool weapon,
        float muzzleSpeed,
        float recoilScale,
        ProjectileWeaponType weaponType)
    {
        ref WeaponAmmoPool pool = ref GetAmmoPoolRef(weapon);
        if (!pool.CanFire)
        {
            return false;
        }

        FireWeapon(muzzleSpeed, recoilScale, weaponType);
        pool.ConsumeRound();
        MenuUiSounds.PlayWeaponGunshot(weaponType);
        return true;
    }

    Ray BuildSniperAimRay()
    {
        float spreadHalf = SniperCurrentSpreadHalfPixels();
        if (spreadHalf <= 0.01f || viewCamera == null)
        {
            return BuildCenterAimRay();
        }

        Vector2 offset = SampleSniperSpreadOffset(spreadHalf);
        return viewCamera.ScreenPointToRay(new Vector3(
            (Screen.width * 0.5f) + offset.x,
            (Screen.height * 0.5f) + offset.y,
            0f));
    }

    static Vector2 SampleSniperSpreadOffset(float spreadHalf)
    {
        // Triangular distribution (average of two uniforms) peaks at center but
        // still reaches the inner crosshair tips fairly often.
        float offsetX = (UnityEngine.Random.Range(-spreadHalf, spreadHalf) +
            UnityEngine.Random.Range(-spreadHalf, spreadHalf)) * 0.5f;
        float offsetY = (UnityEngine.Random.Range(-spreadHalf, spreadHalf) +
            UnityEngine.Random.Range(-spreadHalf, spreadHalf)) * 0.5f;
        return new Vector2(offsetX, offsetY);
    }

    float SniperCurrentSpreadHalfPixels()
    {
        float hipSpread = sniperHipFireCrosshairGap;
        if (!_sniperAimingHeld || _sniperScopeSwapPhase == 1)
        {
            return hipSpread;
        }

        float accuracy = SniperAdsAccuracyFactor();
        if (accuracy >= 0.999f)
        {
            return IsMagnifiedSniperScope(_sniperScopeIndex) ? 0f : sniperAdsMinSpreadPixels;
        }

        return Mathf.Lerp(hipSpread, IsMagnifiedSniperScope(_sniperScopeIndex) ? 0f : sniperAdsMinSpreadPixels, accuracy);
    }

    float SniperAdsAccuracyFactor()
    {
        if (!_sniperAimingHeld)
        {
            return 0f;
        }

        if (_sniperScopeSwapPhase == 1)
        {
            return 0f;
        }

        if (_sniperScopeSwapPhase == 2 &&
            _sniperFovTransitionDuration > 0f)
        {
            return Mathf.Clamp01(_sniperFovTransitionElapsed / _sniperFovTransitionDuration);
        }

        if (_sniperFovTransitionDuration > 0f &&
            _sniperFovTransitionElapsed < _sniperFovTransitionDuration)
        {
            return Mathf.Clamp01(_sniperFovTransitionElapsed / _sniperFovTransitionDuration);
        }

        if (_sniperScopeOverlayBlend < 0.98f && IsMagnifiedSniperScope(_sniperScopeIndex))
        {
            return _sniperScopeOverlayBlend;
        }

        return 1f;
    }

    void UpdateSniperAdsState()
    {
        if (SelectedTool != CardHotbarTool.SniperRifle || viewCamera == null)
        {
            ExitSniperAds();
            return;
        }

        bool wantAds = Input.GetMouseButton(1);
        bool wasAlreadyAiming = _sniperAimingHeld;
        if (_sniperScopeSwapPhase != 0)
        {
            TickSniperScopeSwap(wantAds);
        }
        else if (wantAds)
        {
            if (!wasAlreadyAiming)
            {
                _sniperAimingHeld = true;
                _sniperAdsActive = true;
                BeginSniperFovTransition(SniperScopeFieldOfView(_sniperScopeIndex));
            }
        }
        else if (_sniperAimingHeld)
        {
            _sniperAimingHeld = false;
            _sniperAdsActive = false;
            _sniperScopeSwapPhase = 0;
            BeginSniperFovTransition(fieldOfView);
        }

        if (_sniperScopeSwapPhase == 0)
        {
            TickSniperFovTransition();
        }

        UpdateSniperScopeOverlay();
        RefreshHeldToolVisibility();
    }

    void BeginSniperScopeSwap(int targetScopeIndex)
    {
        targetScopeIndex = Mathf.Clamp(targetScopeIndex, 0, 2);
        if (targetScopeIndex == _sniperScopeIndex || !_sniperAimingHeld)
        {
            return;
        }

        _sniperPendingScopeIndex = targetScopeIndex;
        _sniperScopeSwapPhase = 1;
        _sniperScopeSwapTimer = 0f;
        BeginSniperFovTransition(fieldOfView);
    }

    void TickSniperScopeSwap(bool wantAds)
    {
        if (!wantAds)
        {
            _sniperScopeSwapPhase = 0;
            _sniperAimingHeld = false;
            _sniperAdsActive = false;
            BeginSniperFovTransition(fieldOfView);
            return;
        }

        _sniperScopeSwapTimer += Time.deltaTime;

        if (_sniperScopeSwapPhase == 1)
        {
            TickSniperFovTransition();
            _sniperScopeOverlayBlend = Mathf.MoveTowards(
                _sniperScopeOverlayBlend,
                0f,
                Time.deltaTime / Mathf.Max(0.01f, sniperScopeSwapDropSeconds));

            if (_sniperScopeSwapTimer < sniperScopeSwapDropSeconds)
            {
                PushSniperScopePostEffect();
                return;
            }

            _sniperScopeIndex = _sniperPendingScopeIndex;
            _sniperScopeSwapPhase = 2;
            _sniperScopeSwapTimer = 0f;
            BeginSniperFovTransition(SniperScopeFieldOfView(_sniperScopeIndex));
            RefreshHeldToolVisibility();
        }

        if (_sniperScopeSwapPhase == 2)
        {
            TickSniperFovTransition();
            UpdateSniperScopeOverlay();

            if (_sniperFovTransitionDuration <= 0f ||
                _sniperFovTransitionElapsed >= _sniperFovTransitionDuration)
            {
                _sniperScopeSwapPhase = 0;
                _sniperScopeSwapTimer = 0f;
                RefreshHeldToolVisibility();
            }
        }
    }

    void BeginSniperFovTransition(float targetFov)
    {
        _sniperFovTransitionStart = _sniperDisplayedFov > 0f ? _sniperDisplayedFov : fieldOfView;
        _sniperFovTransitionTarget = targetFov;
        _sniperFovTransitionElapsed = 0f;

        if (_sniperAimingHeld && targetFov < fieldOfView - 0.01f)
        {
            _sniperFovTransitionDuration = SniperAdsTransitionDuration(_sniperScopeIndex);
        }
        else
        {
            _sniperFovTransitionDuration = adsExitTransitionSeconds;
        }
    }

    void TickSniperFovTransition()
    {
        if (_sniperFovTransitionDuration <= 0f)
        {
            _sniperDisplayedFov = _sniperFovTransitionTarget;
            viewCamera.fieldOfView = _sniperDisplayedFov;
            return;
        }

        _sniperFovTransitionElapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(_sniperFovTransitionElapsed / _sniperFovTransitionDuration);
        normalized = normalized * normalized * (3f - (2f * normalized));
        _sniperDisplayedFov = Mathf.Lerp(_sniperFovTransitionStart, _sniperFovTransitionTarget, normalized);
        viewCamera.fieldOfView = _sniperDisplayedFov;
    }

    void UpdateSniperScopeOverlay()
    {
        if (_sniperScopeSwapPhase == 1)
        {
            PushSniperScopePostEffect();
            return;
        }

        bool wantOverlay = _sniperAimingHeld;
        float fadeDuration = wantOverlay
            ? SniperAdsTransitionDuration(_sniperScopeIndex)
            : adsExitTransitionSeconds;
        float step = Time.deltaTime / Mathf.Max(0.01f, fadeDuration);
        _sniperScopeOverlayBlend = Mathf.MoveTowards(
            _sniperScopeOverlayBlend,
            wantOverlay ? 1f : 0f,
            step);

        PushSniperScopePostEffect();
    }

    void PushSniperScopePostEffect()
    {
        if (SniperScopePostEffect.Instance == null)
        {
            return;
        }

        bool active = _sniperScopeOverlayBlend > 0.001f && _sniperAimingHeld;
        SniperScopePostEffect.Instance.SetActive(
            active,
            _sniperScopeOverlayBlend,
            _sniperScopeIndex);
    }

    float SniperAdsTransitionDuration(int scopeIndex)
    {
        switch (scopeIndex)
        {
            case 1:
                return ads4xTransitionSeconds;
            case 2:
                return ads10xTransitionSeconds;
            default:
                return adsIronTransitionSeconds;
        }
    }

    static bool IsMagnifiedSniperScope(int scopeIndex)
    {
        return scopeIndex == 1 || scopeIndex == 2;
    }

    float SniperScopeFieldOfView(int scopeIndex)
    {
        switch (scopeIndex)
        {
            case 1:
                return ads4xFov;
            case 2:
                return ads10xFov;
            default:
                return adsIronSightFov;
        }
    }

    void ExitSniperAds()
    {
        _sniperAimingHeld = false;
        _sniperAdsActive = false;
        _sniperScopeSwapPhase = 0;
        _sniperScopeSwapTimer = 0f;
        _sniperScopeOverlayBlend = 0f;
        _sniperScopeSway = Vector2.zero;
        _sniperFovTransitionElapsed = 0f;
        _sniperFovTransitionDuration = 0f;
        _sniperFovTransitionTarget = fieldOfView;
        _sniperFovTransitionStart = fieldOfView;
        _sniperDisplayedFov = fieldOfView;
        if (viewCamera != null)
        {
            viewCamera.fieldOfView = fieldOfView;
        }

        if (SniperScopePostEffect.Instance != null)
        {
            SniperScopePostEffect.Instance.SetActive(false, 0f, 0);
        }
    }

    void FireWeapon(float muzzleSpeed, float recoilScale, ProjectileWeaponType weaponType)
    {
        FireWeapon(BuildCenterAimRay(), muzzleSpeed, recoilScale, weaponType);
    }

    void FireWeapon(Ray shotRay, float muzzleSpeed, float recoilScale, ProjectileWeaponType weaponType)
    {
        if (viewCamera == null)
        {
            return;
        }

        Vector3 spawnPosition = BulletSpawnPosition(shotRay);
        var bullet = new GameObject("Projectile Bullet");
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.LookRotation(shotRay.direction, Vector3.up);
        bullet.AddComponent<ProjectileBullet>().Initialize(shotRay.direction * muzzleSpeed, weaponType);

        _gunKickTimer = 0.08f;
        _muzzleFlashTimer = 0.045f;
        _gunRecoilPeak = new Vector2(
            UnityEngine.Random.Range(-gunRecoilHorizontalRandomness, gunRecoilHorizontalRandomness) * recoilScale,
            UnityEngine.Random.Range(gunRecoilVerticalRandomness * 0.55f, gunRecoilVerticalRandomness) * recoilScale);
        float verticalRetention = UnityEngine.Random.Range(0.35f, 0.58f);
        _gunRecoilResidual = new Vector2(_gunRecoilPeak.x, _gunRecoilPeak.y * verticalRetention);
        _gunRecoilKickTimer = gunRecoilKickDuration;
        _gunRecoilAimApplied = false;
    }

    Vector3 BulletSpawnPosition(Ray ray)
    {
        float desiredOffset = Mathf.Max(0.05f, gunMuzzleForwardOffset);
        Vector3 desiredSpawn = ray.origin + (ray.direction * desiredOffset);
        if (Physics.Raycast(ray.origin, ray.direction, out var hit, desiredOffset,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return ray.origin + (ray.direction * Mathf.Max(0.02f, hit.distance - 0.03f));
        }

        return desiredSpawn;
    }

    void HandleHammerInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SwingHammer();
        }
    }

    void SwingHammer()
    {
        _hammerSwingTimer = 0.18f;
        if (viewCamera == null || voxelWorld == null)
        {
            return;
        }

        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, buildRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float hammerRange = Mathf.Max(0.1f, hammerRangeVoxels * (voxelWorld != null ? voxelWorld.VoxelSize : 1f));
        Vector3 closestBodyPoint = _capsule != null
            ? _capsule.ClosestPoint(hit.point)
            : transform.position;
        if (Vector3.Distance(closestBodyPoint, hit.point) > hammerRange)
        {
            return;
        }

        var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
        if (marker != null)
        {
            voxelWorld.TryRemovePlayerBuiltObject(marker);
        }
    }

    void HandleBuildingInput()
    {
        if (viewCamera == null || voxelWorld == null)
        {
            return;
        }

        if (!BuildModeActive)
        {
            ClearBuildInteractionState();
            return;
        }

        if (_mouseMovedThisFrame)
        {
            _scrollTargetLocked = false;
        }

        HandleBuildOrientationInput();
        UpdateBuildCandidate();
        HandleBuildSelectorInput();

        if (HandleRectangleBuildInput())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !_selectorOpen && _hasBuildCandidate && _buildCandidate.CanPlace)
        {
            voxelWorld.TryPlaceBuildPiece(_buildCandidate);
            UpdateBuildCandidate();
        }
    }

    void ClearBuildInteractionState()
    {
        _selectorOpen = false;
        _hasBuildCandidate = false;
        _rectangleDragActive = false;
        _scrollTargetLocked = false;
        _orientationLocked = false;
        DestroyPreviewRootsFrom(0);
    }

    void HandleBuildOrientationInput()
    {
        if (!HasBuildOrientation(_selectedPiece))
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            _orientationLocked = !_orientationLocked;
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            TryLockScrollTarget();
            _buildOrientationIndex = (_buildOrientationIndex + 1) % BuildFaceNormals.Length;
        }
    }

    void TryLockScrollTarget()
    {
        if (_mouseMovedThisFrame || _scrollTargetLocked || !_hasBuildCandidate || !_buildCandidate.HasTarget)
        {
            return;
        }

        _scrollLockedCell = _buildCandidate.Cell;
        _scrollTargetLocked = true;
    }

    bool HandleRectangleBuildInput()
    {
        if (!SupportsRectangleDrag(_selectedPiece))
        {
            _rectangleDragActive = false;
            return false;
        }

        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!ctrlHeld)
        {
            if (_rectangleDragActive)
            {
                DestroyPreviewRootsFrom(1);
            }
            _rectangleDragActive = false;
            return false;
        }

        if (Input.GetMouseButtonDown(0) && !_selectorOpen && _hasBuildCandidate && _buildCandidate.HasTarget)
        {
            _rectangleDragActive = true;
            _rectangleStartCandidate = _buildCandidate;
            _rectangleEndCandidate = _buildCandidate;
            _rectangleFaceNormal = _buildCandidate.FaceNormal;
            _rectangleWidthAxis = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
                ? Vector3Int.zero
                : CameraPreferredWidthAxis(_rectangleFaceNormal);
            RebuildRectangleCandidates();
            return true;
        }

        if (!_rectangleDragActive)
        {
            return false;
        }

        if (TryGetRectangleEndCandidate(out var endCandidate))
        {
            _rectangleEndCandidate = endCandidate;
            RebuildRectangleCandidates();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_rectangleAllValid)
            {
                voxelWorld.TryPlaceBuildPieceBatch(_rectanglePlaceCandidates);
                UpdateBuildCandidate();
            }

            _rectangleDragActive = false;
            DestroyPreviewRootsFrom(1);
        }

        return true;
    }

    void HandleBuildSelectorInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _selectorOpen = true;
            _selectorDirection = Vector2.zero;
        }

        if (_selectorOpen && Input.GetMouseButton(1))
        {
            _selectorDirection += new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")) * selectorMouseScale;

            if (_selectorDirection.magnitude >= selectorActivationDistance)
            {
                SelectPieceFromDirection(_selectorDirection);
            }
        }

        if (_selectorOpen && Input.GetMouseButtonUp(1))
        {
            _selectorOpen = false;
        }
    }

    void SelectPieceFromDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        var nextPiece = PieceFromRadialAngle(angle);
        if (nextPiece != _selectedPiece)
        {
            _scrollTargetLocked = false;
            _selectedPiece = nextPiece;
        }
    }

    void UpdateBuildCandidate()
    {
        Vector3Int faceNormal = HasBuildOrientation(_selectedPiece)
            ? BuildFaceNormals[_buildOrientationIndex]
            : Vector3Int.zero;
        Ray buildRay = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (_scrollTargetLocked && HasBuildOrientation(_selectedPiece))
        {
            _hasBuildCandidate = voxelWorld.TryCreateBuildPieceCandidate(
                _selectedPiece,
                _scrollLockedCell,
                faceNormal,
                out _buildCandidate);

            if (_hasBuildCandidate && _buildCandidate.HasTarget &&
                _buildCandidate.CanPlace &&
                !HasLineOfSightToBuildCandidate(_buildCandidate))
            {
                _buildCandidate.CanPlace = false;
            }
            return;
        }

        _hasBuildCandidate = voxelWorld.TryGetBuildPieceCandidate(
            buildRay,
            buildRange,
            _selectedPiece,
            faceNormal,
            out _buildCandidate);

        if (_hasBuildCandidate && _buildCandidate.HasTarget &&
            (!_buildCandidate.CanPlace || !HasLineOfSightToBuildCandidate(_buildCandidate)) &&
            (TryFindVisibleSideBuildCandidate(buildRay, _buildCandidate, out var snappedCandidate) ||
            TryFindSnappedBuildCandidate(_buildCandidate, out snappedCandidate)))
        {
            _buildCandidate = snappedCandidate;
        }

        if (_hasBuildCandidate && _buildCandidate.HasTarget &&
            (_buildCandidate.CanPlace || voxelWorld.IsBuildSurfaceOccupied(_buildCandidate)) &&
            !HasLineOfSightToBuildCandidate(_buildCandidate))
        {
            _buildCandidate.CanPlace = false;
        }

        if (_hasBuildCandidate && _buildCandidate.HasTarget && !_buildCandidate.CanPlace &&
            voxelWorld.IsBuildSurfaceOccupied(_buildCandidate))
        {
            _hasBuildCandidate = false;
        }
    }

    bool TryFindSnappedBuildCandidate(
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        if (rawCandidate.CanPlace)
        {
            return true;
        }

        float bestDistance = float.MaxValue;
        bool found = false;
        int radius = Mathf.Max(0, buildSnapRadius);

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    var cell = rawCandidate.Cell + new Vector3Int(x, y, z);
                    if (!voxelWorld.TryCreateBuildPieceCandidate(
                        rawCandidate.PieceType,
                        cell,
                        rawCandidate.FaceNormal,
                        out var candidate) ||
                        !candidate.CanPlace ||
                        !HasLineOfSightToBuildCandidate(candidate))
                    {
                        continue;
                    }

                    float distance = CrosshairDistance(candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        snappedCandidate = candidate;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    bool TryFindVisibleSideBuildCandidate(
        Ray buildRay,
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        if (!CanUseVisibleSideSuggestion(rawCandidate.PieceType) ||
            !Physics.Raycast(buildRay, out var hit, buildRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Vector3Int visibleNormal = QuantizeNormal(hit.normal);
        if (rawCandidate.PieceType == VoxelLightingWorld.BuildPieceType.Ceiling ||
            rawCandidate.PieceType == VoxelLightingWorld.BuildPieceType.TrapDoor)
        {
            return TryFindVisibleSideHorizontalCandidate(rawCandidate, visibleNormal, out snappedCandidate);
        }

        if (visibleNormal.y != 0)
        {
            return false;
        }

        Vector3Int startCell = rawCandidate.Cell + visibleNormal;
        Vector3Int faceNormal = _orientationLocked ? rawCandidate.FaceNormal : visibleNormal;
        var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
        if (!_orientationLocked && marker != null && marker.IsPanelPiece && marker.FaceNormal.y == 0)
        {
            startCell = marker.Cell + visibleNormal;
            faceNormal = marker.FaceNormal;
        }

        Vector3Int widthAxis = faceNormal.x != 0 ? new Vector3Int(0, 0, 1) : Vector3Int.right;
        return TrySearchVisibleSideCandidates(
            rawCandidate.PieceType,
            startCell,
            faceNormal,
            visibleNormal,
            widthAxis,
            true,
            out snappedCandidate);
    }

    bool TryFindVisibleSideHorizontalCandidate(
        VoxelLightingWorld.BuildPieceCandidate rawCandidate,
        Vector3Int visibleNormal,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = rawCandidate;

        Vector3Int startCell = rawCandidate.Cell;
        Vector3Int towardPlayer = Vector3Int.zero;
        Vector3Int sideAxis = Vector3Int.right;
        if (visibleNormal.y == 0)
        {
            startCell += visibleNormal;
            towardPlayer = visibleNormal;
            sideAxis = PerpendicularHorizontalAxis(visibleNormal);
        }

        return TrySearchVisibleSideCandidates(
            rawCandidate.PieceType,
            startCell,
            Vector3Int.up,
            towardPlayer,
            sideAxis,
            false,
            out snappedCandidate);
    }

    bool TrySearchVisibleSideCandidates(
        VoxelLightingWorld.BuildPieceType pieceType,
        Vector3Int startCell,
        Vector3Int faceNormal,
        Vector3Int towardPlayerAxis,
        Vector3Int sideSearchAxis,
        bool includeVerticalOffsets,
        out VoxelLightingWorld.BuildPieceCandidate snappedCandidate)
    {
        snappedCandidate = default;

        int radius = Mathf.Max(0, buildSnapRadius);
        int towardPlayerSteps = towardPlayerAxis == Vector3Int.zero ? 0 : Mathf.Max(1, radius + 1);

        float bestDistance = float.MaxValue;
        int bestTowardPlayerStep = int.MaxValue;
        bool found = false;

        for (int step = 0; step <= towardPlayerSteps; step++)
        {
            for (int sideOffset = -radius; sideOffset <= radius; sideOffset++)
            {
                int minY = includeVerticalOffsets ? -radius : 0;
                int maxY = includeVerticalOffsets ? radius : 0;
                for (int yOffset = minY; yOffset <= maxY; yOffset++)
                {
                    Vector3Int cell = startCell + (towardPlayerAxis * step);

                    if (includeVerticalOffsets)
                    {
                        cell += sideSearchAxis * sideOffset;
                        cell += Vector3Int.up * yOffset;
                    }
                    else
                    {
                        cell += sideSearchAxis * sideOffset;
                    }

                    if (!TryUseSuggestedCandidate(pieceType, cell, faceNormal, out var candidate))
                    {
                        continue;
                    }

                    float distance = CrosshairDistance(candidate);
                    if (distance < bestDistance ||
                        (Mathf.Approximately(distance, bestDistance) && step < bestTowardPlayerStep))
                    {
                        bestDistance = distance;
                        bestTowardPlayerStep = step;
                        snappedCandidate = candidate;
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    bool TryUseSuggestedCandidate(
        VoxelLightingWorld.BuildPieceType pieceType,
        Vector3Int cell,
        Vector3Int faceNormal,
        out VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        return voxelWorld.TryCreateBuildPieceCandidate(pieceType, cell, faceNormal, out candidate) &&
            candidate.CanPlace &&
            HasLineOfSightToBuildCandidate(candidate);
    }

    float CrosshairDistance(VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        if (viewCamera == null)
        {
            return 0f;
        }

        Vector3 viewportPoint = viewCamera.WorldToViewportPoint(candidate.Position);
        if (viewportPoint.z < 0f)
        {
            return float.MaxValue;
        }

        float dx = viewportPoint.x - 0.5f;
        float dy = viewportPoint.y - 0.5f;
        return (dx * dx) + (dy * dy);
    }

    bool TryGetRectangleEndCandidate(out VoxelLightingWorld.BuildPieceCandidate endCandidate)
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var plane = new Plane((Vector3)_rectangleFaceNormal, _rectangleStartCandidate.Position);

        if (plane.Raycast(ray, out float enter) && enter >= 0f)
        {
            Vector3 point = ray.GetPoint(enter);
            Vector3 cellPoint = point - ((Vector3)_rectangleFaceNormal * (voxelWorld.VoxelSize * 0.5f));
            Vector3Int cell = voxelWorld.WorldToCell(cellPoint);
            cell = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
                ? new Vector3Int(cell.x, _rectangleStartCandidate.Cell.y, cell.z)
                : ProjectCellToRectangleAxis(_rectangleStartCandidate.Cell, cell, _rectangleWidthAxis);
            return voxelWorld.TryCreateBuildPieceCandidate(_selectedPiece, cell, _rectangleFaceNormal, out endCandidate);
        }

        endCandidate = _rectangleEndCandidate;
        return false;
    }

    static Vector3Int ProjectCellToRectangleAxis(Vector3Int start, Vector3Int raw, Vector3Int widthAxis)
    {
        if (widthAxis.x != 0)
        {
            return new Vector3Int(raw.x, raw.y, start.z);
        }

        return new Vector3Int(start.x, raw.y, raw.z);
    }

    void RebuildRectangleCandidates()
    {
        _rectangleCandidates.Clear();
        _rectanglePlaceCandidates.Clear();

        if (!_rectangleStartCandidate.HasTarget || !_rectangleEndCandidate.HasTarget)
        {
            _rectangleAllValid = false;
            return;
        }

        Vector3Int start = _rectangleStartCandidate.Cell;
        Vector3Int end = _rectangleEndCandidate.Cell;
        Vector3Int faceNormal = _rectangleFaceNormal;

        int maxCells = Mathf.Max(1, rectangleMaxCells);
        int clampedEndY = _selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling
            ? start.y
            : ClampToRange(end.y, start.y, maxCells - 1);
        int minY = Mathf.Max(1, Mathf.Min(start.y, clampedEndY));
        int maxY = Mathf.Max(start.y, clampedEndY);

        if (_selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling)
        {
            int clampedEndX = ClampToRange(end.x, start.x, maxCells - 1);
            int clampedEndZ = ClampToRange(end.z, start.z, maxCells - 1);
            int minX = Mathf.Min(start.x, clampedEndX);
            int maxX = Mathf.Max(start.x, clampedEndX);
            int minZ = Mathf.Min(start.z, clampedEndZ);
            int maxZ = Mathf.Max(start.z, clampedEndZ);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    AddRectangleCandidate(new Vector3Int(x, start.y, z), faceNormal);
                }
            }
        }
        else if (_rectangleWidthAxis.z != 0)
        {
            int clampedEndZ = ClampToRange(end.z, start.z, maxCells - 1);
            int minZ = Mathf.Min(start.z, clampedEndZ);
            int maxZ = Mathf.Max(start.z, clampedEndZ);
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    AddRectangleCandidate(new Vector3Int(start.x, y, z), faceNormal);
                }
            }
        }
        else
        {
            int clampedEndX = ClampToRange(end.x, start.x, maxCells - 1);
            int minX = Mathf.Min(start.x, clampedEndX);
            int maxX = Mathf.Max(start.x, clampedEndX);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    AddRectangleCandidate(new Vector3Int(x, y, start.z), faceNormal);
                }
            }
        }

        if (_rectangleValidFlags.Length < _rectanglePlaceCandidates.Count)
        {
            _rectangleValidFlags = new bool[_rectanglePlaceCandidates.Count];
        }

        _rectangleAllValid =
            _rectanglePlaceCandidates.Count > 0 &&
            voxelWorld.ValidateBuildPieceBatch(_rectanglePlaceCandidates, _rectangleValidFlags) &&
            RectangleCandidatesHaveLineOfSight();
    }

    bool RectangleCandidatesHaveLineOfSight()
    {
        for (int i = 0; i < _rectanglePlaceCandidates.Count; i++)
        {
            if (!HasLineOfSightToBuildCandidate(_rectanglePlaceCandidates[i]))
            {
                return false;
            }
        }

        return true;
    }

    void AddRectangleCandidate(Vector3Int cell, Vector3Int faceNormal)
    {
        if (voxelWorld.TryCreateBuildPieceCandidate(_selectedPiece, cell, faceNormal, out var candidate))
        {
            _rectangleCandidates.Add(candidate);
            if (voxelWorld.IsBuildSurfaceOccupied(candidate))
            {
                return;
            }

            if (!HasLineOfSightToBuildCandidate(candidate))
            {
                candidate.CanPlace = false;
            }

            _rectanglePlaceCandidates.Add(candidate);
        }
    }

    static int ClampToRange(int value, int origin, int maxDistance)
    {
        return Mathf.Clamp(value, origin - maxDistance, origin + maxDistance);
    }

    void UpdateBuildPreview()
    {
        if (!BuildModeActive)
        {
            HidePreviewRoots();
            return;
        }

        if (_rectangleDragActive)
        {
            if (_rectangleCandidates.Count == 0)
            {
                HidePreviewRoots();
                return;
            }

            for (int i = 0; i < _rectangleCandidates.Count; i++)
            {
                UpdatePreviewRoot(i, _rectangleCandidates[i], _rectangleAllValid);
            }

            HidePreviewRootsFrom(_rectangleCandidates.Count);
            return;
        }

        if (!_hasBuildCandidate || !_buildCandidate.HasTarget)
        {
            HidePreviewRoots();
            return;
        }

        UpdatePreviewRoot(0, _buildCandidate, _buildCandidate.CanPlace);
        HidePreviewRootsFrom(1);
    }

    void CreateHeldToolVisuals()
    {
        if (viewCamera == null || _pistolRoot != null)
        {
            return;
        }

        Material gunMaterial = CreateHeldToolMaterial("Held Gun Material", new Color(0.08f, 0.08f, 0.09f, 1f));
        Material hammerMaterial = CreateHeldToolMaterial("Held Hammer Material", new Color(0.32f, 0.23f, 0.14f, 1f));
        Material metalMaterial = CreateHeldToolMaterial("Held Hammer Head Material", new Color(0.62f, 0.62f, 0.64f, 1f));
        Material blueprintMaterial = CreateHeldToolMaterial("Held Blueprint Material", new Color(0.08f, 0.22f, 0.68f, 1f));
        Material flashMaterial = CreateHeldToolMaterial("Muzzle Flash Material", new Color(0.82f, 0.58f, 0.12f, 1f));

        _pistolRoot = new GameObject("Held Pistol");
        _pistolRoot.transform.SetParent(viewCamera.transform, false);
        _pistolRoot.transform.localPosition = new Vector3(0.34f, -0.26f, 0.62f);
        _pistolRoot.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);
        CreateHeldCube(_pistolRoot.transform, "Pistol Body", new Vector3(0f, 0f, 0f), new Vector3(0.24f, 0.16f, 0.28f), gunMaterial);
        CreateHeldCube(_pistolRoot.transform, "Pistol Barrel", new Vector3(0.04f, 0.03f, 0.28f), new Vector3(0.1f, 0.1f, 0.42f), gunMaterial);
        CreateHeldCube(_pistolRoot.transform, "Pistol Grip", new Vector3(-0.04f, -0.17f, -0.04f), new Vector3(0.08f, 0.26f, 0.1f), gunMaterial);
        _pistolMuzzleFlashRoot = CreateHeldCube(_pistolRoot.transform, "Pistol Muzzle Flash", new Vector3(0.04f, 0.03f, 0.52f), new Vector3(0.18f, 0.18f, 0.08f), flashMaterial);
        _pistolMuzzleFlashRoot.SetActive(false);

        _assaultRifleRoot = new GameObject("Held Assault Rifle");
        _assaultRifleRoot.transform.SetParent(viewCamera.transform, false);
        _assaultRifleRoot.transform.localPosition = new Vector3(0.3f, -0.24f, 0.58f);
        _assaultRifleRoot.transform.localRotation = Quaternion.Euler(0f, -4f, 0f);
        CreateHeldCube(_assaultRifleRoot.transform, "AR Body", new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.14f, 0.52f), gunMaterial);
        CreateHeldCube(_assaultRifleRoot.transform, "AR Barrel", new Vector3(0.03f, 0.02f, 0.42f), new Vector3(0.08f, 0.08f, 0.62f), gunMaterial);
        CreateHeldCube(_assaultRifleRoot.transform, "AR Stock", new Vector3(-0.02f, -0.02f, -0.28f), new Vector3(0.1f, 0.12f, 0.22f), gunMaterial);
        CreateHeldCube(_assaultRifleRoot.transform, "AR Grip", new Vector3(0f, -0.14f, -0.02f), new Vector3(0.07f, 0.18f, 0.08f), gunMaterial);
        CreateHeldCube(_assaultRifleRoot.transform, "AR Mag", new Vector3(0f, -0.12f, 0.08f), new Vector3(0.06f, 0.16f, 0.1f), gunMaterial);
        _assaultRifleMuzzleFlashRoot = CreateHeldCube(_assaultRifleRoot.transform, "AR Muzzle Flash", new Vector3(0.03f, 0.02f, 0.74f), new Vector3(0.16f, 0.16f, 0.08f), flashMaterial);
        _assaultRifleMuzzleFlashRoot.SetActive(false);

        _sniperRifleRoot = new GameObject("Held Sniper Rifle");
        _sniperRifleRoot.transform.SetParent(viewCamera.transform, false);
        _sniperRifleRoot.transform.localPosition = new Vector3(0.28f, -0.22f, 0.56f);
        _sniperRifleRoot.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
        CreateHeldCube(_sniperRifleRoot.transform, "Sniper Body", new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.12f, 0.68f), gunMaterial);
        CreateHeldCube(_sniperRifleRoot.transform, "Sniper Barrel", new Vector3(0.02f, 0.02f, 0.52f), new Vector3(0.06f, 0.06f, 0.92f), gunMaterial);
        CreateHeldCube(_sniperRifleRoot.transform, "Sniper Stock", new Vector3(-0.02f, -0.02f, -0.34f), new Vector3(0.1f, 0.11f, 0.24f), gunMaterial);
        CreateHeldCube(_sniperRifleRoot.transform, "Sniper Scope", new Vector3(0f, 0.1f, 0.08f), new Vector3(0.08f, 0.08f, 0.28f), gunMaterial);
        CreateHeldCube(_sniperRifleRoot.transform, "Sniper Grip", new Vector3(0f, -0.13f, -0.04f), new Vector3(0.06f, 0.16f, 0.08f), gunMaterial);
        _sniperMuzzleFlashRoot = CreateHeldCube(_sniperRifleRoot.transform, "Sniper Muzzle Flash", new Vector3(0.02f, 0.02f, 0.98f), new Vector3(0.14f, 0.14f, 0.08f), flashMaterial);
        _sniperMuzzleFlashRoot.SetActive(false);

        _hammerRoot = new GameObject("Held Hammer");
        _hammerRoot.transform.SetParent(viewCamera.transform, false);
        _hammerRoot.transform.localPosition = new Vector3(0.34f, -0.31f, 0.58f);
        CreateHeldCube(_hammerRoot.transform, "Hammer Handle", new Vector3(0f, -0.08f, 0f), new Vector3(0.06f, 0.38f, 0.06f), hammerMaterial);
        CreateHeldCube(_hammerRoot.transform, "Hammer Head", new Vector3(0f, 0.14f, 0.02f), new Vector3(0.3f, 0.1f, 0.12f), metalMaterial);

        _blueprintRoot = new GameObject("Held Blueprint");
        _blueprintRoot.transform.SetParent(viewCamera.transform, false);
        _blueprintRoot.transform.localPosition = new Vector3(0.3f, -0.24f, 0.54f);
        _blueprintRoot.transform.localRotation = Quaternion.Euler(65f, -8f, -8f);
        CreateHeldCube(_blueprintRoot.transform, "Blueprint Page", Vector3.zero, new Vector3(0.42f, 0.02f, 0.3f), blueprintMaterial);
    }

    GameObject CreateHeldCube(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        Destroy(cube.GetComponent<Collider>());
        return cube;
    }

    void RefreshHeldToolVisibility()
    {
        if (_pistolRoot != null)
        {
            _pistolRoot.SetActive(SelectedTool == CardHotbarTool.Pistol);
        }

        if (_assaultRifleRoot != null)
        {
            _assaultRifleRoot.SetActive(SelectedTool == CardHotbarTool.AssaultRifle);
        }

        if (_sniperRifleRoot != null)
        {
            _sniperRifleRoot.SetActive(ShouldShowSniperHeldModel());
        }

        if (_hammerRoot != null)
        {
            _hammerRoot.SetActive(SelectedTool == CardHotbarTool.Hammer);
        }

        if (_blueprintRoot != null)
        {
            _blueprintRoot.SetActive(SelectedTool == CardHotbarTool.Blueprint);
        }
    }

    bool ShouldShowSniperHeldModel()
    {
        return SelectedTool == CardHotbarTool.SniperRifle &&
            (!_sniperAimingHeld || !IsMagnifiedSniperScope(_sniperScopeIndex));
    }

    void UpdateHeldToolVisuals()
    {
        if (_pistolRoot == null || _assaultRifleRoot == null || _sniperRifleRoot == null || _hammerRoot == null)
        {
            return;
        }

        if (_muzzleFlashTimer > 0f)
        {
            _muzzleFlashTimer = Mathf.Max(0f, _muzzleFlashTimer - Time.deltaTime);
        }
        if (_gunRecoilKickTimer > 0f)
        {
            _gunRecoilKickTimer = Mathf.Max(0f, _gunRecoilKickTimer - Time.deltaTime);
            if (_gunRecoilKickTimer <= 0f && !_gunRecoilAimApplied)
            {
                ApplyGunRecoilToAim();
                _gunRecoilAimApplied = true;
                _gunRecoilPeak = Vector2.zero;
            }
        }
        if (_gunKickTimer > 0f)
        {
            _gunKickTimer = Mathf.Max(0f, _gunKickTimer - Time.deltaTime);
        }
        if (_hammerSwingTimer > 0f)
        {
            _hammerSwingTimer = Mathf.Max(0f, _hammerSwingTimer - Time.deltaTime);
        }

        float kickProgress = _gunKickTimer > 0f
            ? Mathf.Sin((1f - (_gunKickTimer / 0.08f)) * Mathf.PI)
            : 0f;

        Vector3 pistolPosition = new Vector3(0.34f, -0.26f, 0.62f - (0.08f * kickProgress));
        Quaternion pistolRotation = Quaternion.Euler(0f, -5f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.Pistol, ref pistolPosition);
        ApplyReloadDipToGun(CardHotbarTool.Pistol, ref pistolPosition, ref pistolRotation);
        _pistolRoot.transform.localPosition = pistolPosition;
        _pistolRoot.transform.localRotation = pistolRotation;

        Vector3 arPosition = new Vector3(0.3f, -0.24f, 0.58f - (0.06f * kickProgress));
        Quaternion arRotation = Quaternion.Euler(0f, -4f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.AssaultRifle, ref arPosition);
        ApplyReloadDipToGun(CardHotbarTool.AssaultRifle, ref arPosition, ref arRotation);
        _assaultRifleRoot.transform.localPosition = arPosition;
        _assaultRifleRoot.transform.localRotation = arRotation;

        Vector3 sniperPosition = new Vector3(0.28f, -0.22f, 0.56f - (0.05f * kickProgress));
        Quaternion sniperRotation = Quaternion.Euler(0f, -3f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.SniperRifle, ref sniperPosition);
        ApplyReloadDipToGun(CardHotbarTool.SniperRifle, ref sniperPosition, ref sniperRotation);
        _sniperRifleRoot.transform.localPosition = sniperPosition;
        _sniperRifleRoot.transform.localRotation = sniperRotation;

        bool showPistolFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.Pistol;
        bool showArFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.AssaultRifle;
        bool showSniperFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.SniperRifle;
        float flashPulse = _muzzleFlashTimer > 0f ? UnityEngine.Random.Range(0.85f, 1.25f) : 1f;

        if (_pistolMuzzleFlashRoot != null)
        {
            _pistolMuzzleFlashRoot.SetActive(showPistolFlash);
            _pistolMuzzleFlashRoot.transform.localScale = new Vector3(0.18f, 0.18f, 0.08f) * flashPulse;
        }

        if (_assaultRifleMuzzleFlashRoot != null)
        {
            _assaultRifleMuzzleFlashRoot.SetActive(showArFlash);
            _assaultRifleMuzzleFlashRoot.transform.localScale = new Vector3(0.16f, 0.16f, 0.08f) * flashPulse;
        }

        if (_sniperMuzzleFlashRoot != null)
        {
            _sniperMuzzleFlashRoot.SetActive(showSniperFlash);
            _sniperMuzzleFlashRoot.transform.localScale = new Vector3(0.14f, 0.14f, 0.08f) * flashPulse;
        }

        float swingProgress = _hammerSwingTimer > 0f
            ? Mathf.Sin((1f - (_hammerSwingTimer / 0.18f)) * Mathf.PI)
            : 0f;
        _hammerRoot.transform.localRotation = Quaternion.Euler(-55f * swingProgress, 0f, -10f);
    }

    void UpdatePreviewRoot(int index, VoxelLightingWorld.BuildPieceCandidate candidate, bool valid)
    {
        GameObject root = EnsurePreviewRoot(index);
        root.SetActive(true);
        root.transform.position = candidate.Position;
        root.transform.rotation = candidate.Rotation;

        SetPreviewColor(root, valid ? validPreviewColor : invalidPreviewColor);
        UpdatePreviewBars(root, candidate.Scale);
    }

    GameObject EnsurePreviewRoot(int index)
    {
        while (_previewRoots.Count <= index)
        {
            _previewRoots.Add(CreatePreviewRoot(_previewRoots.Count));
        }

        return _previewRoots[index];
    }

    GameObject CreatePreviewRoot(int index)
    {
        var root = new GameObject($"Build Preview Outline {index + 1}");

        for (int i = 0; i < 4; i++)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Preview Edge {i + 1}";
            bar.transform.SetParent(root.transform, false);
            bar.GetComponent<MeshRenderer>().sharedMaterial = CreateTransparentMaterial("Build Preview Material", validPreviewColor);
            Destroy(bar.GetComponent<Collider>());
        }

        return root;
    }

    void HidePreviewRoots()
    {
        HidePreviewRootsFrom(0);
    }

    void HidePreviewRootsFrom(int startIndex)
    {
        for (int i = startIndex; i < _previewRoots.Count; i++)
        {
            _previewRoots[i].SetActive(false);
        }
    }

    void DestroyPreviewRootsFrom(int startIndex)
    {
        for (int i = _previewRoots.Count - 1; i >= startIndex; i--)
        {
            Destroy(_previewRoots[i]);
            _previewRoots.RemoveAt(i);
        }
    }

    void SetPreviewColor(GameObject root, Color color)
    {
        for (int i = 0; i < root.transform.childCount; i++)
        {
            var renderer = root.transform.GetChild(i).GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }
        }
    }

    void UpdatePreviewBars(GameObject root, Vector3 scale)
    {
        float thinAxis = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
        float barThickness = Mathf.Max(thinAxis * 1.2f, 0.03f);

        if (scale.y <= scale.x && scale.y <= scale.z)
        {
            SetPreviewBar(root, 0, new Vector3(0f, 0f, scale.z * 0.5f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 1, new Vector3(0f, 0f, -scale.z * 0.5f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 2, new Vector3(scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 3, new Vector3(-scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, barThickness, scale.z));
        }
        else if (scale.x <= scale.z)
        {
            SetPreviewBar(root, 0, new Vector3(0f, scale.y * 0.5f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 1, new Vector3(0f, -scale.y * 0.5f, 0f), new Vector3(barThickness, barThickness, scale.z));
            SetPreviewBar(root, 2, new Vector3(0f, 0f, scale.z * 0.5f), new Vector3(barThickness, scale.y, barThickness));
            SetPreviewBar(root, 3, new Vector3(0f, 0f, -scale.z * 0.5f), new Vector3(barThickness, scale.y, barThickness));
        }
        else
        {
            SetPreviewBar(root, 0, new Vector3(0f, scale.y * 0.5f, 0f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 1, new Vector3(0f, -scale.y * 0.5f, 0f), new Vector3(scale.x, barThickness, barThickness));
            SetPreviewBar(root, 2, new Vector3(scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, scale.y, barThickness));
            SetPreviewBar(root, 3, new Vector3(-scale.x * 0.5f, 0f, 0f), new Vector3(barThickness, scale.y, barThickness));
        }
    }

    void SetPreviewBar(GameObject root, int index, Vector3 localPosition, Vector3 localScale)
    {
        Transform bar = root.transform.GetChild(index);
        bar.localPosition = localPosition;
        bar.localRotation = Quaternion.identity;
        bar.localScale = localScale;
    }

    void OnGUI()
    {
        if (!GameSession.IsMatchActive || !SceneFlow.IsGameActive)
        {
            return;
        }

        if (!IsUiOverlayBlocking())
        {
            if (GameSession.IsInPrepPhase && !GameSession.IsPrepReady)
            {
                // Card pick only — no gameplay HUD yet.
            }
            else if (GameSession.IsInPrepPhase && GameSession.IsPrepReady)
            {
                DrawHotbar();
            }
            else
            {
                DrawHotbar();

                if (!IsGameplayBlocked())
                {
                    DrawCrosshair();
                }

                DrawBuildSelector();
            }
        }

        PlayerBulletHitFlash.DrawOverlay();
    }

    void DrawCrosshair()
    {
        if (_isReloading)
        {
            if (SelectedTool == CardHotbarTool.SniperRifle &&
                _sniperAimingHeld &&
                _sniperScopeSwapPhase != 1 &&
                IsMagnifiedSniperScope(_sniperScopeIndex))
            {
                DrawRedDot();
                DrawSniperScopeLabel();
            }

            return;
        }

        if (SelectedTool == CardHotbarTool.SniperRifle)
        {
            bool showHipCrosshair = !_sniperAimingHeld || _sniperScopeSwapPhase == 1;
            if (showHipCrosshair)
            {
                DrawStandardCrosshair(sniperHipFireCrosshairGap, sniperHipFireCrosshairLength);
            }
            else if (_sniperAimingHeld)
            {
                if (_sniperScopeIndex == 0)
                {
                    DrawStandardCrosshair(weaponCrosshairGap, weaponCrosshairLength);
                }
                else if (_sniperScopeOverlayBlend > 0.05f)
                {
                    DrawRedDot();
                    DrawSniperScopeLabel();
                }
            }

            return;
        }

        if (SelectedTool == CardHotbarTool.AssaultRifle)
        {
            DrawStandardCrosshair(weaponCrosshairGap, weaponCrosshairLength);
            return;
        }

        DrawStandardCrosshair(crosshairGap, crosshairLength);
    }

    static float SniperScopeRadiusFraction =>
        SniperScopePostEffect.Instance != null ? SniperScopePostEffect.Instance.scopeRadius : (1f / 3f);

    void DrawSniperScopeLabel()
    {
        if (_sniperScopeSwapPhase != 0 || !IsMagnifiedSniperScope(_sniperScopeIndex))
        {
            return;
        }

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float scopeRadiusPx = Screen.height * SniperScopeRadiusFraction;
        const float panelWidth = 72f;
        const float panelHeight = 24f;
        float panelX = centerX - (panelWidth * 0.5f);
        float panelY = centerY - scopeRadiusPx - panelHeight - 10f;

        Color previousColor = GUI.color;
        GUI.color = scopeLabelPanelColor;
        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), Texture2D.whiteTexture);
        GUI.color = previousColor;

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = scopeLabelColor }
        };
        GUI.Label(new Rect(panelX, panelY, panelWidth, panelHeight), SniperScopeLabelText(_sniperScopeIndex), labelStyle);
    }

    static string SniperScopeLabelText(int scopeIndex)
    {
        switch (scopeIndex)
        {
            case 1:
                return "4x";
            case 2:
                return "10x";
            default:
                return "IRON";
        }
    }

    void DrawStandardCrosshair(float gap, float length)
    {
        Color previousColor = GUI.color;
        GUI.color = crosshairColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float halfThickness = crosshairThickness * 0.5f;

        GUI.DrawTexture(
            new Rect(centerX - gap - length, centerY - halfThickness, length, crosshairThickness),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX + gap, centerY - halfThickness, length, crosshairThickness),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX - halfThickness, centerY - gap - length, crosshairThickness, length),
            Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(centerX - halfThickness, centerY + gap, crosshairThickness, length),
            Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    void DrawRedDot()
    {
        Color previousColor = GUI.color;
        GUI.color = redDotColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float half = redDotSize * 0.5f;
        GUI.DrawTexture(new Rect(centerX - half, centerY - half, redDotSize, redDotSize), Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    void DrawHotbar()
    {
        const float slotSize = 36f;
        const float slotGap = 6f;
        const float groupGap = 14f;
        const float margin = 16f;

        float x = margin;
        float y = Screen.height - slotSize - margin;

        var keyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 8,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.2f, 0.2f, 0.2f, 0.9f) }
        };

        Color previousColor = GUI.color;
        var kit = _activeKit ?? CardKitDefinition.DefaultInfantryPlaceholder();
        int equippableCount = HotbarSlotCount;
        float reloadOverlayFill = ReloadOverlayFill();
        float hotbarWidth = slotSize + groupGap +
            (equippableCount * slotSize) + ((equippableCount - 1) * slotGap) +
            (equippableCount > 2 ? groupGap : 0f);

        DrawAmmoPanel(new Rect(x, y - 22f, hotbarWidth, 18f));

        DrawAbilityHotbarSlot(new Rect(x, y, slotSize, slotSize), keyStyle, reloadOverlayFill);
        x += slotSize + groupGap;

        for (int i = 0; i < equippableCount; i++)
        {
            if (i == 2)
            {
                x += groupGap;
            }

            var tool = kit.GetToolAt(i);
            bool selected = i == _selectedHotbarIndex;
            DrawEquippableHotbarSlot(
                new Rect(x, y, slotSize, slotSize),
                CardKitDefinition.HotbarKeyLabel(i),
                tool,
                selected,
                keyStyle,
                reloadOverlayFill);

            x += slotSize + slotGap;
        }

        GUI.color = previousColor;
    }

    void DrawAmmoPanel(Rect rect)
    {
        if (!IsFirearmTool(SelectedTool))
        {
            return;
        }

        var pool = GetAmmoPoolForSelectedTool();
        Color previousColor = GUI.color;

        GUI.color = Color.white;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);

        var ammoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };
        GUI.color = Color.black;
        GUI.Label(rect, $"{pool.reserve} / {pool.mag}", ammoStyle);

        GUI.color = previousColor;
    }

    void DrawAbilityHotbarSlot(Rect rect, GUIStyle keyStyle, float reloadOverlayFill)
    {
        bool ready = IsAbilityReady();
        Color previousColor = GUI.color;

        GUI.color = ready
            ? new Color(0.96f, 0.96f, 0.96f, 0.92f)
            : new Color(0.34f, 0.34f, 0.36f, 0.88f);
        GUI.Box(rect, string.Empty);

        float overlayFill = Mathf.Max(AbilityCooldownOverlayFill(), reloadOverlayFill);
        if (overlayFill > 0.001f)
        {
            float overlayHeight = rect.height * overlayFill;
            GUI.color = new Color(0.04f, 0.04f, 0.04f, ready ? 0.35f : 0.62f);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y + (rect.height - overlayHeight), rect.width, overlayHeight),
                Texture2D.whiteTexture);
        }

        GUI.color = ready ? Color.white : new Color(0.82f, 0.82f, 0.82f, 0.85f);
        GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 18f, 12f), "E", keyStyle);
        DrawAbilityHotbarIcon(rect, !ready);

        GUI.color = previousColor;
    }

    void DrawAbilityHotbarIcon(Rect rect, bool dimmed)
    {
        switch (ActiveCardSpecialty())
        {
            case "sniper":
                HotbarIconDrawer.DrawSniperScopeAbilityIcon(rect, (_sniperScopeIndex + 1) % 3, dimmed);
                break;
            case "infantry":
                HotbarIconDrawer.DrawInfantryAbilityIcon(rect, dimmed);
                break;
        }
    }

    void DrawEquippableHotbarSlot(
        Rect rect,
        string keyLabel,
        CardHotbarTool tool,
        bool selected,
        GUIStyle keyStyle,
        float reloadOverlayFill)
    {
        Color previousColor = GUI.color;
        GUI.color = selected
            ? new Color(0.16f, 0.68f, 0.24f, 0.9f)
            : new Color(0.96f, 0.96f, 0.96f, 0.72f);

        GUI.Box(rect, string.Empty);

        if (reloadOverlayFill > 0.001f)
        {
            float overlayHeight = rect.height * reloadOverlayFill;
            GUI.color = new Color(0.04f, 0.04f, 0.04f, 0.62f);
            GUI.DrawTexture(
                new Rect(rect.x, rect.y + (rect.height - overlayHeight), rect.width, overlayHeight),
                Texture2D.whiteTexture);
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 18f, 12f), keyLabel, keyStyle);
        HotbarIconDrawer.DrawToolIcon(rect, tool, dimmed: false);

        GUI.color = previousColor;
    }

    void DrawBuildSelector()
    {
        if (!_selectorOpen)
        {
            return;
        }

        EnsureRadialTexture();
        float size = selectorRadius * 2f;
        var rect = new Rect(Screen.width * 0.5f - selectorRadius, Screen.height * 0.5f - selectorRadius, size, size);
        GUI.DrawTexture(rect, _radialTexture);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };

        for (int i = 0; i < BuildPieceOptions.Length; i++)
        {
            float angle = i * (360f / BuildPieceOptions.Length) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (selectorRadius * 0.72f);
            var labelRect = new Rect(rect.center.x + offset.x - 46f, rect.center.y - offset.y - 12f, 92f, 24f);
            GUI.Label(labelRect, DisplayName(BuildPieceOptions[i]).ToUpperInvariant(), labelStyle);
        }
    }

    void EnsureRadialTexture()
    {
        if (_radialTexture != null && _radialTexturePiece == _selectedPiece)
        {
            return;
        }

        const int size = 192;
        if (_radialTexture == null)
        {
            _radialTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        _radialTexturePiece = _selectedPiece;
        float center = (size - 1) * 0.5f;
        float radius = size * 0.48f;
        float innerRadius = size * 0.16f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x - center, y - center);
                float distance = offset.magnitude;
                if (distance > radius || distance < innerRadius)
                {
                    _radialTexture.SetPixel(x, y, Color.clear);
                    continue;
                }

                VoxelLightingWorld.BuildPieceType sector = PieceFromRadialAngle(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                Color color = sector == _selectedPiece
                    ? new Color(0.08f, 0.62f, 0.16f, 0.45f)
                    : new Color(0.98f, 0.98f, 0.98f, 0.36f);
                _radialTexture.SetPixel(x, y, color);
            }
        }

        _radialTexture.Apply();
    }

    VoxelLightingWorld.BuildPieceType PieceFromRadialAngle(float angle)
    {
        float sectorSize = 360f / BuildPieceOptions.Length;
        float normalized = Mathf.Repeat(angle + (sectorSize * 0.5f), 360f);
        int index = Mathf.FloorToInt(normalized / sectorSize);
        return BuildPieceOptions[Mathf.Clamp(index, 0, BuildPieceOptions.Length - 1)];
    }

    static string DisplayName(VoxelLightingWorld.BuildPieceType pieceType)
    {
        switch (pieceType)
        {
            case VoxelLightingWorld.BuildPieceType.TrapDoor:
                return "Trap Door";
            default:
                return pieceType.ToString();
        }
    }

    static bool HasBuildOrientation(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType == VoxelLightingWorld.BuildPieceType.Wall ||
            pieceType == VoxelLightingWorld.BuildPieceType.Window ||
            pieceType == VoxelLightingWorld.BuildPieceType.Door;
    }

    static bool SupportsRectangleDrag(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType == VoxelLightingWorld.BuildPieceType.Wall ||
            pieceType == VoxelLightingWorld.BuildPieceType.Window ||
            pieceType == VoxelLightingWorld.BuildPieceType.Ceiling;
    }

    static bool CanUseVisibleSideSuggestion(VoxelLightingWorld.BuildPieceType pieceType)
    {
        return pieceType != VoxelLightingWorld.BuildPieceType.Ladder;
    }

    bool HasLineOfSightToBuildCandidate(VoxelLightingWorld.BuildPieceCandidate candidate)
    {
        if (viewCamera == null)
        {
            return true;
        }

        // The center belongs to all four halves. Reject it once up front before
        // spending additional raycasts on their surrounding sample grids.
        if (!HasLineOfSightToBuildPoint(candidate, candidate.Position))
        {
            return false;
        }

        GetBuildCandidateFaceAxes(candidate, out var horizontalAxis, out float horizontalExtent,
            out var verticalAxis, out float verticalExtent);

        // Keep samples just inside the physical edges so touching neighboring tiles do not
        // incorrectly occlude a half-face at a shared seam.
        horizontalExtent *= 0.92f;
        verticalExtent *= 0.92f;

        return IsBuildHalfFaceVisible(candidate, horizontalAxis, horizontalExtent, verticalAxis, verticalExtent,
                   -1f, 0f, -1f, 1f) ||
               IsBuildHalfFaceVisible(candidate, horizontalAxis, horizontalExtent, verticalAxis, verticalExtent,
                   0f, 1f, -1f, 1f) ||
               IsBuildHalfFaceVisible(candidate, horizontalAxis, horizontalExtent, verticalAxis, verticalExtent,
                   -1f, 1f, 0f, 1f) ||
               IsBuildHalfFaceVisible(candidate, horizontalAxis, horizontalExtent, verticalAxis, verticalExtent,
                   -1f, 1f, -1f, 0f);
    }

    static void GetBuildCandidateFaceAxes(
        VoxelLightingWorld.BuildPieceCandidate candidate,
        out Vector3 horizontalAxis,
        out float horizontalExtent,
        out Vector3 verticalAxis,
        out float verticalExtent)
    {
        Vector3 scale = candidate.Scale;
        float x = Mathf.Abs(scale.x);
        float y = Mathf.Abs(scale.y);
        float z = Mathf.Abs(scale.z);

        if (x <= y && x <= z)
        {
            horizontalAxis = candidate.Rotation * Vector3.forward;
            horizontalExtent = z * 0.5f;
            verticalAxis = candidate.Rotation * Vector3.up;
            verticalExtent = y * 0.5f;
            return;
        }

        if (z <= x && z <= y)
        {
            horizontalAxis = candidate.Rotation * Vector3.right;
            horizontalExtent = x * 0.5f;
            verticalAxis = candidate.Rotation * Vector3.up;
            verticalExtent = y * 0.5f;
            return;
        }

        horizontalAxis = candidate.Rotation * Vector3.right;
        horizontalExtent = x * 0.5f;
        verticalAxis = candidate.Rotation * Vector3.forward;
        verticalExtent = z * 0.5f;
    }

    bool IsBuildHalfFaceVisible(
        VoxelLightingWorld.BuildPieceCandidate candidate,
        Vector3 horizontalAxis,
        float horizontalExtent,
        Vector3 verticalAxis,
        float verticalExtent,
        float horizontalMin,
        float horizontalMax,
        float verticalMin,
        float verticalMax)
    {
        for (int verticalSample = 0; verticalSample < 3; verticalSample++)
        {
            float vertical = Mathf.Lerp(verticalMin, verticalMax, verticalSample * 0.5f) * verticalExtent;
            for (int horizontalSample = 0; horizontalSample < 3; horizontalSample++)
            {
                float horizontal = Mathf.Lerp(horizontalMin, horizontalMax, horizontalSample * 0.5f) *
                    horizontalExtent;
                Vector3 target = candidate.Position +
                    (horizontalAxis * horizontal) +
                    (verticalAxis * vertical);

                if (!HasLineOfSightToBuildPoint(candidate, target))
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool HasLineOfSightToBuildPoint(
        VoxelLightingWorld.BuildPieceCandidate candidate,
        Vector3 target)
    {
        Vector3 origin = viewCamera.transform.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return true;
        }

        direction /= distance;
        if (!Physics.Raycast(origin, direction, out var hit, distance - 0.03f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
        return marker != null &&
            marker.IsPanelPiece &&
            marker.Cell == candidate.Cell &&
            marker.FaceNormal == candidate.FaceNormal;
    }

    Vector3Int CameraForwardHorizontalAxis()
    {
        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        forward.y = 0f;
        return Mathf.Abs(forward.x) >= Mathf.Abs(forward.z)
            ? Vector3Int.right
            : new Vector3Int(0, 0, 1);
    }

    Vector3Int CameraPreferredWidthAxis(Vector3Int faceNormal)
    {
        Vector3Int cameraAxis = CameraForwardHorizontalAxis();

        int dot = (cameraAxis.x * faceNormal.x) + (cameraAxis.z * faceNormal.z);
        if (dot == 0)
        {
            return cameraAxis;
        }

        // Width must stay inside the wall plane, so if camera-forward points
        // through the wall, use the only horizontal axis that lies on it.
        return faceNormal.x != 0 ? new Vector3Int(0, 0, 1) : Vector3Int.right;
    }

    string OrientationLabel()
    {
        Vector3Int normal = BuildFaceNormals[_buildOrientationIndex];
        if (normal == Vector3Int.right)
        {
            return "+X";
        }
        if (normal == Vector3Int.left)
        {
            return "-X";
        }
        if (normal.z > 0)
        {
            return "+Z";
        }
        return "-Z";
    }

    string OrientationHelpText()
    {
        if (_selectedPiece == VoxelLightingWorld.BuildPieceType.Ceiling)
        {
            return "Ceiling drag: horizontal rectangle";
        }

        if (HasBuildOrientation(_selectedPiece))
        {
            return $"X: rotate {OrientationLabel()}";
        }

        return string.Empty;
    }

    string BuildModeHelpText()
    {
        string dragHelp = SupportsRectangleDrag(_selectedPiece)
            ? "   Ctrl+Left Drag: line/rectangle"
            : string.Empty;
        string lockHelp = HasBuildOrientation(_selectedPiece)
            ? $"   Z: orientation lock {(_orientationLocked ? "ON" : "OFF")}"
            : string.Empty;
        string orientationHelp = OrientationHelpText();
        if (!string.IsNullOrEmpty(orientationHelp))
        {
            orientationHelp = $"   {orientationHelp}";
        }

        return $"BLUEPRINT ({_selectedPiece})   Wheel/1-2/F/H: hotbar   Left Click: place{dragHelp}{orientationHelp}{lockHelp}   Right Click + Mouse Direction: select   Esc: menu";
    }

    static Vector3Int PerpendicularHorizontalAxis(Vector3Int axis)
    {
        if (axis.x != 0)
        {
            return new Vector3Int(0, 0, 1);
        }

        return Vector3Int.right;
    }

    static Vector3Int QuantizeNormal(Vector3 normal)
    {
        float ax = Mathf.Abs(normal.x);
        float ay = Mathf.Abs(normal.y);
        float az = Mathf.Abs(normal.z);

        if (ay >= ax && ay >= az)
        {
            return normal.y >= 0f ? Vector3Int.up : Vector3Int.down;
        }

        if (ax >= az)
        {
            return normal.x >= 0f ? Vector3Int.right : Vector3Int.left;
        }

        return normal.z >= 0f ? new Vector3Int(0, 0, 1) : new Vector3Int(0, 0, -1);
    }

    static Material CreateHeldToolMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        return new Material(shader)
        {
            name = materialName,
            color = color
        };
    }

    static Material CreateTransparentMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}
