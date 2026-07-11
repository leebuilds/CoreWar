using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// First-person Rigidbody controller with camera-relative movement and grid building.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    static readonly GrenadeType[] GrenadeOptions =
    {
        GrenadeType.Frag,
        GrenadeType.Flashbang
    };

    static int GrenadeWheelSegmentCount => GrenadeOptions.Length;

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
    public bool deferStartUntilNetworkSpawn;
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
    public float smgBulletSpeed = 400f;
    public float smgRpm = 540f;
    public float lmgBulletSpeed = 935f;
    public float lmgRpm = 320f;
    public float machineGunBulletSpeed = 2000f;
    public float machineGunRpm = 1500;
    public float machineGunDrawSeconds = 2.16f;
    public float machineGunCrosshairRadiusPixels = 24f;
    public float machineGunSpreadCenterBiasExponent = 4.5f;
    public float machineGunSuppressionDurationSeconds = 1.5f;
    public float gunnerSuppressionBoostDurationSeconds = 7f;
    public float gunnerSuppressionBoostCooldownSeconds = 30f;
    public float gunnerSuppressionBoostRpm = 3000f;
    public float gunnerSuppressionBoostCrosshairRadiusMultiplier = 1.2f;
    public float gunnerSuppressionBoostSpreadCenterBiasExponent = 0.42f;
    public float gunnerSuppressionBoostFlickIntensityMultiplier = 2.5f;
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
    public float infantrySpeedBoostMultiplier = 1.15f;
    public float infantrySpeedBoostDurationSeconds = 10f;
    public float infantrySpeedBoostCooldownSeconds = 30f;
    public float heavyShieldHealth = 120f;
    public float heavyShieldDecayPerSecond = 12f;
    public float heavyShieldCooldownSeconds = 30f;
    public float skirmisherDashDistanceMeters = 8f;
    public float skirmisherDashDurationSeconds = 0.2f;
    public float skirmisherDashCooldownSeconds = 8f;
    public float scopedArAdsMagnification = 1.8f;
    public float scopedArAdsTransitionSeconds = 0.22f;
    public float rangerHoldBreathMaxSeconds = 4f;
    public float rangerHoldBreathCooldownSeconds = 14f;
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
    public float smgDrawSeconds = 0.69f;
    public float assaultRifleDrawSeconds = 1.1f;
    public float lmgDrawSeconds = 1.8f;
    public float sniperDrawSeconds = 2f;
    public float huntingRifleAdsMagnification = 6.5f;
    public float huntingRifleHipFireCrosshairGap = 56f;
    public float huntingRifleHipFireCrosshairLength = 44f;
    public float hunterMarkDurationSeconds = 4f;
    public float hunterMarkCooldownSeconds = 40f;
    public float cyborgLaserSpeed = 600f;
    public float cyborgLaserRpm = 500f;
    public float cyborgLaserOverheatSeconds = 4f;
    public float cyborgLaserOverheatCooldownSeconds = 3f;
    public float cyborgLaserCoolSeconds = 5f;
    public float cyborgLaserDrawSeconds = .8f;
    public float laserSwordRangeMeters = 4.5f;
    public float laserSwordArcDegrees = 135f;
    public float laserSwordSwingSeconds = 0.5f;
    public float laserSwordCooldownSeconds = .8f;
    public float laserSwordDamage = 40f;
    public float cyborgRegenBoostDurationSeconds = 6f;
    public float cyborgRegenBoostCooldownSeconds = 35f;
    public float cyborgRegenBoostFractionPerSecond = 0.2f;
    public float cyborgMaxHealthBoostFraction = PlayerHealth.CyborgMaxHealthBoostFraction;
    public float antiMaterialDrawSeconds = 2f;
    public float antiMaterialAdsTransitionSeconds = 1.05f;
    public float antiMaterialAdsMagnification = 12f;
    public float antiMaterialChargeSeconds = 1f;
    public float antiMaterialBulletSpeed = 1300f;
    public float antiMaterialBraceCooldownSeconds = 45f;
    public float antiMaterialBraceAnchorDistanceMeters = 0.5f;
    public float antiMaterialBraceOrbitDegreesPerSecond = 72f;
    public float antiMaterialBraceGunTiltDegrees = 18f;
    public float antiMaterialBraceMaxVerticalAim = 0.35f;
    public float c4DrawSeconds = 0.8f;
    public float c4RemoteDrawSeconds = 0.5f;
    public float c4ThrowSpeed = 10f;
    public float c4FallAccelerationMetersPerSecond = 8f;
    public float c4ThrowLockSeconds = 1f;
    public float c4RemoteDetonationDelaySeconds = 1f;
    public float explosiveVestAttachSeconds = 5f;
    public float explosiveVestMaxAttachDistanceMeters = 1f;
    public float explosiveVestTargetSearchRadiusMeters = 2.5f;
    public float explosiveVestCooldownSeconds = 120f;
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
    public float fragGrenadeFuseSeconds = 5f;
    public float grenadeDrawSeconds = 0.5f;
    const float GrenadeWheelHoldSeconds = 0.18f;
    const float GrenadePostThrowSlotSwitchSeconds = 0.5f;
    const float GrenadeHandCooldownSeconds = 1.8f;
    const int FragGrenadesPerLife = 2;
    const int FlashbangGrenadesPerLife = 1;

    Rigidbody _rb;
    CapsuleCollider _capsule;
    float _yaw;
    float _pitch;
    float _baseLookSensitivity;
    bool _grounded;
    bool _selectorOpen;
    Vector2 _selectorDirection;
    bool _grenadeSelectorOpen;
    Vector2 _grenadeSelectorDirection;
    bool _grenadeSlotSelected;
    bool _grenadeKeyHeld;
    float _grenadeKeyHoldTimer;
    bool _grenadeWheelOpenedFromHold;
    GrenadeType _selectedGrenade = GrenadeType.Frag;
    bool _grenadePrimed;
    float _grenadeFuseTimer;
    float _grenadePostThrowSlotSwitchTimer;
    float _grenadeHandCooldownTimer;
    int _fragGrenadesRemaining;
    int _flashbangGrenadesRemaining;
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
    GameObject _scopedAssaultRifleRoot;
    GameObject _sniperRifleRoot;
    GameObject _huntingRifleRoot;
    GameObject _smgRoot;
    GameObject _machinePistolRoot;
    GameObject _lmgRoot;
    GameObject _machineGunRoot;
    GameObject _antiMaterialRifleRoot;
    GameObject _cyborgLaserRoot;
    GameObject _laserSwordRoot;
    GameObject _c4ChargeRoot;
    GameObject _c4RemoteRoot;
    GameObject _heldFragGrenadeRoot;
    MeshRenderer _heldGrenadeBodyRenderer;
    GameObject _hammerRoot;
    GameObject _blueprintRoot;
    GameObject _pistolMuzzleFlashRoot;
    GameObject _assaultRifleMuzzleFlashRoot;
    GameObject _scopedAssaultRifleMuzzleFlashRoot;
    GameObject _sniperMuzzleFlashRoot;
    GameObject _huntingRifleMuzzleFlashRoot;
    GameObject _smgMuzzleFlashRoot;
    GameObject _machinePistolMuzzleFlashRoot;
    GameObject _lmgMuzzleFlashRoot;
    GameObject _machineGunMuzzleFlashRoot;
    GameObject _antiMaterialMuzzleFlashRoot;
    GameObject _cyborgLaserMuzzleFlashRoot;
    float _weaponFireCooldown;
    float _weaponFireSlowTimer;
    bool _sniperAimingHeld;
    bool _sniperAdsActive;
    bool _scopedArAdsHeld;
    float _scopedArDisplayedFov;
    float _scopedArFovTransitionStart;
    float _scopedArFovTransitionTarget;
    float _scopedArFovTransitionElapsed;
    float _scopedArFovTransitionDuration;
    float _scopedArScopeOverlayBlend;
    bool _holdBreathActive;
    float _holdBreathRemaining;
    float _hunterMarkRemaining;
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
    float _scopeSwayNoiseSeedX;
    float _scopeSwayNoiseSeedY;
    float _scopeSwayIntensity = 1f;
    float _scopeSwayIntensityVelocity;
    float _scopeSwayTargetIntensity = 1f;
    float _scopeSwaySpeedFactor = 1f;
    float _scopeSwaySpeedFactorVelocity;
    float _scopeSwayTargetSpeedFactor = 1f;
    float _scopeSwayPhaseTimer;
    float _scopeSwayOscillationPhase;
    int _scopeSwayLastPhaseKind = -1;
    int _scopeSwayFastStreak;
    float _baseCardMoveSpeed = 8f;
    float _abilityCooldownRemaining;
    float _speedBoostRemaining;
    bool _shieldAbilityActive;
    bool _dashActive;
    float _dashTimer;
    Vector3 _dashDirection;
    PlayerHealth _playerHealth;
    bool _initialized;
    bool _localAuthority = true;
    GameSession.Team _playerTeam = GameSession.Team.Red;
    float _gunKickTimer;
    float _muzzleFlashTimer;
    float _hammerSwingTimer;
    float _laserSwordSwingTimer;
    float _laserSwordCooldownTimer;
    float _cyborgLaserHeat;
    float _cyborgLaserOverheatLockoutTimer;
    bool _cyborgLaserFiringHeld;
    float _cyborgRegenBoostRemaining;
    float _antiMaterialChargeTimer;
    bool _antiMaterialCharging;
    bool _antiMaterialBraceActive;
    Vector3 _antiMaterialBraceAnchor;
    Vector3 _antiMaterialBraceGroundNormal = Vector3.up;
    float _antiMaterialBraceOrbitAngle;
    float _antiMaterialBraceOrbitRadius;
    float _antiMaterialBraceTransformHeightAboveAnchor;
    Vector2 _antiMaterialBraceGunTilt;
    float _antiMaterialBraceBaseYaw;
    C4ChargeProjectile _activeC4Charge;
    float _c4ActionLockTimer;
    float _c4RemoteDrawTimer;
    bool _c4RemoteReady;
    Vector2 _gunRecoilPeak;
    Vector2 _gunRecoilResidual;
    float _gunRecoilKickTimer;
    bool _gunRecoilAimApplied;
    float _sessionHeartbeat;
    bool _wasInPrepPhase;
    bool _wasPrepReady;
    WeaponAmmoPool _pistolAmmo;
    WeaponAmmoPool _smgAmmo;
    WeaponAmmoPool _machinePistolAmmo;
    WeaponAmmoPool _assaultRifleAmmo;
    WeaponAmmoPool _lmgAmmo;
    WeaponAmmoPool _machineGunAmmo;
    WeaponAmmoPool _sniperAmmo;
    WeaponAmmoPool _huntingRifleAmmo;
    WeaponAmmoPool _antiMaterialAmmo;
    WeaponAmmoPool _c4Ammo;
    int _pistolFamilyReserve;
    int _rifleFamilyReserve;
    float _pistolFamilyRechargeTimer;
    float _rifleFamilyRechargeTimer;
    float _machineGunRechargeTimer;
    float _machineGunSuppressionRemaining;
    float _machineGunSuppressionSpeedMultiplier = 1f;
    float _gunnerSuppressionBoostRemaining;
    float _sniperRechargeTimer;
    float _huntingRifleRechargeTimer;
    float _antiMaterialRechargeTimer;
    float _c4RechargeTimer;
    bool _explosiveVestAttaching;
    float _explosiveVestAttachTimer;
    GameObject _explosiveVestAttachTarget;
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
    float _postPrepWeaponLockTimer;
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
    Texture2D _grenadeRadialTexture;
    int _grenadeRadialTextureHighlight = -1;
    PhysicsMaterial _slipperyMaterial;

    const int DefaultSniperMagnificationIndex = 1;
    const float AssaultRifleRecoilScale = 0.675f;
    const float ScopedArRecoilMultiplier = 1.5f;
    const float ScopedArAdsRecoilScale = 0.5f;
    const float ScopedArHoldBreathRecoilMultiplierValue = 0.13f;
    const float PistolRecoilScale = 0.425f;
    const int ScopedArScopePresentationIndex = 3;
    const float WeaponFireSlowWindowSeconds = 0.35f;
    const float AntiMaterialRecoilScale = 5.67f;
    const float AntiMaterialBraceRecoilMultiplier = 0.2f;
    const float AntiMaterialBraceAdsMultiplier = 0.5f;
    const float AntiMaterialReloadSeconds = 8f;
    const float AntiMaterialBraceReloadSeconds = 6f;
    const float AntiMaterialBraceVerticalTiltFraction = 0.6f;
    const float AntiMaterialBraceMaxGroundSlopeDegrees = 40f;
    const float HuntingRifleScopeSwayMultiplier = 0.8f;
    const float AntiMaterialScopeSwayMultiplier = 6f;
    const float AntiMaterialBraceScopeSwayMultiplier = 0.88f;
    const float ScopedArScopeSwayMultiplier = 0.7f;
    const float ScopeSwayRandomBlend = 0.3f;
    const float ScopeSwayRangeMultiplier = 1.3f;
    const float ScopeSwayGlobalSpeedScale = 1f / 3f;
    const int ScopeSwayPhaseHeavy = 0;
    const int ScopeSwayPhaseWideLong = 1;
    const int ScopeSwayPhaseStill = 2;
    const int ScopeSwayPhaseLight = 3;
    const int ScopeSwayPhaseSlowDrift = 4;
    const float SniperScopeSwaySpeedMultiplier = 0.4f;
    const float HuntingRifleScopeSwaySpeedMultiplier = 0.4f;
    const float AntiMaterialScopeSwaySpeedMultiplier = 0.7f;
    const float AntiMaterialBraceScopeSwaySpeedMultiplier = 0.4f;
    const float ScopedArScopeSwaySpeedMultiplier = 0.7f;
    const float ScopedArHoldBreathScopeSwaySpeedMultiplier = 0.2f;
    const float InfantrySpeedBoostReloadDurationMultiplier = 0.8f;
    const float InfantrySpeedBoostDrawDurationMultiplier = 0.8f;
    const float InfantrySpeedBoostRecoilMultiplier = 0.85f;
    const int AntiMaterialScopePresentationIndex = 4;
    public bool AntiMaterialBraceActive => _antiMaterialBraceActive;

    float AntiMaterialEffectiveAdsTransitionSeconds =>
        antiMaterialAdsTransitionSeconds *
        (IsAntiMaterialBraceActive() ? AntiMaterialBraceAdsMultiplier : 1f);

    float AntiMaterialEffectiveReloadSeconds =>
        IsAntiMaterialBraceActive() ? AntiMaterialBraceReloadSeconds : AntiMaterialReloadSeconds;

    float AntiMaterialEffectiveRecoilScale =>
        AntiMaterialRecoilScale * (IsAntiMaterialBraceActive() ? AntiMaterialBraceRecoilMultiplier : 1f);

    bool IsAntiMaterialBraceActive()
    {
        return _antiMaterialBraceActive && SelectedTool == CardHotbarTool.AntiMaterialRifle;
    }

    bool IsInfantrySpeedBoostActive()
    {
        return _speedBoostRemaining > 0f && ActiveCardId() == "infantry_1";
    }

    float InfantrySpeedBoostReloadDuration(float baseSeconds)
    {
        return IsInfantrySpeedBoostActive()
            ? baseSeconds * InfantrySpeedBoostReloadDurationMultiplier
            : baseSeconds;
    }

    float InfantrySpeedBoostDrawDuration(float baseSeconds)
    {
        return IsInfantrySpeedBoostActive()
            ? baseSeconds * InfantrySpeedBoostDrawDurationMultiplier
            : baseSeconds;
    }

    float InfantrySpeedBoostRecoilScale(float baseRecoilScale)
    {
        return IsInfantrySpeedBoostActive()
            ? baseRecoilScale * InfantrySpeedBoostRecoilMultiplier
            : baseRecoilScale;
    }

    float ReloadDuration(CardHotbarTool weapon)
    {
        switch (weapon)
        {
            case CardHotbarTool.Pistol:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.PistolReloadSeconds);
            case CardHotbarTool.Smg:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.SmgReloadSeconds);
            case CardHotbarTool.MachinePistol:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.MachinePistolReloadSeconds);
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.AssaultRifleReloadSeconds);
            case CardHotbarTool.LightMachineGun:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.LmgReloadSeconds);
            case CardHotbarTool.MachineGun:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.MachineGunReloadSeconds);
            case CardHotbarTool.HuntingRifle:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.HuntingRifleReloadSeconds);
            case CardHotbarTool.AntiMaterialRifle:
                return InfantrySpeedBoostReloadDuration(AntiMaterialEffectiveReloadSeconds);
            case CardHotbarTool.SniperRifle:
                return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.SniperReloadStartSeconds);
            default:
                return 0f;
        }
    }

    float SniperRoundReloadDuration()
    {
        return InfantrySpeedBoostReloadDuration(WeaponAmmoDefaults.SniperRoundReloadSeconds);
    }

    bool BuildModeActive => SelectedTool == CardHotbarTool.Blueprint;

    CardHotbarTool SelectedTool
    {
        get
        {
            if (_grenadeSlotSelected)
            {
                return CardHotbarTool.Grenade;
            }

            return _activeKit == null ? CardHotbarTool.AssaultRifle : _activeKit.GetToolAt(_selectedHotbarIndex);
        }
    }

    int HotbarSlotCount => _activeKit == null ? 4 : _activeKit.SlotCount;

    public static ThirdPersonController Local { get; private set; }

    public GameSession.Team PlayerTeam => _playerTeam;
    public float NetworkAimYaw => _yaw;
    public float NetworkAimPitch => _pitch;
    public bool HasLocalAuthority => _localAuthority;
    public bool IsHudOverlayBlocking => IsUiOverlayBlocking();
    public bool IsHudGameplayBlocked => IsGameplayBlocked();
    public CardKitDefinition ActiveKit => _activeKit ?? CardKitDefinition.DefaultInfantryPlaceholder();
    public int SelectedHotbarIndex => _selectedHotbarIndex;
    public int EquippableHotbarCount => HotbarSlotCount;
    public float HotbarReloadOverlayFill => ReloadOverlayFill();
    public float HotbarSwitchLockOverlayFill => SwitchLockOverlayFill();
    public float HotbarAbilityOverlayFill => AbilityCooldownOverlayFill();
    public float HotbarWeaponOverlayFill(CardHotbarTool tool) => WeaponOverlayFill(tool);
    public bool IsAbilityReadyForHud => IsAbilityReady();
    public bool IsFirearmSelected => IsFirearmTool(SelectedTool);
    public bool ShowsAmmoHud =>
        (IsFirearmTool(SelectedTool) && !UsesOverheatHud) || SelectedTool == CardHotbarTool.C4Charge;
    public bool UsesSingleChargeAmmoHud => SelectedTool == CardHotbarTool.C4Charge;
    public bool IsC4RemoteSelectedForHud =>
        SelectedTool == CardHotbarTool.C4Charge &&
        _activeC4Charge != null &&
        (_c4RemoteDrawTimer > 0f || _c4RemoteReady);
    public bool UsesOverheatHud => CardKitDefinition.UsesOverheatMeter(SelectedTool);
    public float LaserHeatFraction => Mathf.Clamp01(_cyborgLaserHeat);
    public float LaserOverheatLockoutFraction =>
        cyborgLaserOverheatCooldownSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(_cyborgLaserOverheatLockoutTimer / cyborgLaserOverheatCooldownSeconds);
    public bool IsLaserOverheated => _cyborgLaserOverheatLockoutTimer > 0f;
    public WeaponAmmoPool CurrentAmmo => GetAmmoPoolForSelectedTool();
    public bool GunnerSuppressionBoostActive => _gunnerSuppressionBoostRemaining > 0f;
    public bool IsBuildSelectorOpen => _selectorOpen;
    public bool IsGrenadeSelectorOpen => _grenadeSelectorOpen;
    public bool IsGrenadeHotbarSelected => _grenadeSlotSelected;
    public GrenadeType SelectedGrenadeType => _selectedGrenade;
    public int FragGrenadesRemaining => _fragGrenadesRemaining;
    public int FlashbangGrenadesRemaining => _flashbangGrenadesRemaining;
    public bool HasAnyGrenadesRemaining => _fragGrenadesRemaining > 0 || _flashbangGrenadesRemaining > 0;
    public bool IsRadialSelectorOpen => _selectorOpen || _grenadeSelectorOpen;
    public float BuildSelectorRadius => selectorRadius;
    public float GrenadeSelectorRadius => selectorRadius;
    public int BuildPieceOptionCount => BuildPieceOptions.Length;
    public int GrenadeOptionCount => GrenadeOptions.Length;
    public int SniperScopeIndex => _sniperScopeIndex;
    public string ActiveCardSpecialtyForHud => ActiveCardSpecialty();
    public string ActiveCardIdForHud => GameSession.ActiveCardId ?? string.Empty;
    public float HeavyShieldMaxForHud => heavyShieldHealth;

    public Texture2D GetBuildSelectorTexture()
    {
        EnsureRadialTexture();
        return _radialTexture;
    }

    public string GetBuildPieceDisplayName(int index)
    {
        return DisplayName(BuildPieceOptions[Mathf.Clamp(index, 0, BuildPieceOptions.Length - 1)]);
    }

    public Texture2D GetGrenadeSelectorTexture()
    {
        EnsureGrenadeRadialTexture();
        return _grenadeRadialTexture;
    }

    public string GetGrenadeDisplayName(int index)
    {
        if (index < 0 || index >= GrenadeOptions.Length)
        {
            return string.Empty;
        }

        return GrenadeDisplayNameWithCount(GrenadeOptions[index]);
    }

    string GrenadeDisplayNameWithCount(GrenadeType grenadeType)
    {
        int count = GetGrenadeCount(grenadeType);
        if (count <= 0)
        {
            return string.Empty;
        }

        return $"{GrenadeDisplayName(grenadeType)} {count}";
    }

    public void GetCrosshairPresentation(
        out bool showStandard,
        out float gap,
        out float length,
        out float thickness,
        out Color color,
        out bool showRedDot,
        out bool showScopeLabel,
        out int scopeIndex,
        out float scopeRadiusFraction,
        out bool showCircle,
        out float circleRadius,
        out float circleThickness)
    {
        showStandard = false;
        gap = crosshairGap;
        length = crosshairLength;
        thickness = crosshairThickness;
        color = crosshairColor;
        showRedDot = false;
        showScopeLabel = false;
        scopeIndex = _sniperScopeIndex;
        scopeRadiusFraction = SniperScopePostEffect.Instance != null
            ? SniperScopePostEffect.Instance.scopeRadius
            : (1f / 3f);
        showCircle = false;
        circleRadius = 0f;
        circleThickness = crosshairThickness;

        if (_isReloading)
        {
            if (SelectedTool == CardHotbarTool.SniperRifle &&
                _sniperAimingHeld &&
                _sniperScopeSwapPhase != 1 &&
                IsMagnifiedSniperScope(_sniperScopeIndex))
            {
                showRedDot = true;
                showScopeLabel = true;
            }
            else if ((SelectedTool == CardHotbarTool.SniperRifle || SelectedTool == CardHotbarTool.HuntingRifle) &&
                _sniperAimingHeld &&
                _sniperScopeSwapPhase != 1 &&
                !IsMagnifiedSniperScope(ActiveMarksmanAdsScopeIndex()))
            {
                showStandard = true;
                gap = weaponCrosshairGap;
                length = weaponCrosshairLength;
            }
            else if (SelectedTool == CardHotbarTool.ScopedAssaultRifle &&
                _scopedArAdsHeld &&
                _scopedArScopeOverlayBlend > 0.05f)
            {
                showRedDot = true;
                showScopeLabel = true;
                scopeIndex = ScopedArScopePresentationIndex;
            }
            else if (SelectedTool == CardHotbarTool.MachineGun)
            {
                showCircle = true;
                circleRadius = EffectiveMachineGunCrosshairRadiusPixels;
            }

            FinalizeScopeLabel(ref showScopeLabel);
            return;
        }

        if (SelectedTool == CardHotbarTool.AntiMaterialRifle)
        {
            if (IsAntiMaterialAdsReady() && _sniperScopeOverlayBlend > 0.05f)
            {
                showRedDot = true;
                showScopeLabel = true;
                scopeIndex = AntiMaterialScopePresentationIndex;
            }

            return;
        }

        if (SelectedTool == CardHotbarTool.SniperRifle || SelectedTool == CardHotbarTool.HuntingRifle)
        {
            bool showHipCrosshair = !_sniperAimingHeld || _sniperScopeSwapPhase == 1;
            if (showHipCrosshair)
            {
                showStandard = true;
                gap = MarksmanHipFireCrosshairGap();
                length = MarksmanHipFireCrosshairLength();
            }
            else if (_sniperAimingHeld)
            {
                if (ActiveMarksmanAdsScopeIndex() == 0)
                {
                    showStandard = true;
                    gap = weaponCrosshairGap;
                    length = weaponCrosshairLength;
                }
                else if (_sniperScopeOverlayBlend > 0.05f)
                {
                    showRedDot = true;
                    showScopeLabel = true;
                }
            }

            FinalizeScopeLabel(ref showScopeLabel);
            return;
        }

        if (SelectedTool == CardHotbarTool.ScopedAssaultRifle)
        {
            if (!_scopedArAdsHeld || _scopedArScopeOverlayBlend <= 0.05f)
            {
                showStandard = true;
                gap = weaponCrosshairGap;
                length = weaponCrosshairLength;
            }
            else
            {
                showRedDot = true;
                showScopeLabel = true;
                scopeIndex = ScopedArScopePresentationIndex;
            }

            return;
        }

        if (SelectedTool == CardHotbarTool.AssaultRifle ||
            SelectedTool == CardHotbarTool.Smg ||
            SelectedTool == CardHotbarTool.MachinePistol ||
            SelectedTool == CardHotbarTool.LightMachineGun ||
            SelectedTool == CardHotbarTool.CyborgLaser)
        {
            showStandard = true;
            gap = weaponCrosshairGap;
            length = weaponCrosshairLength;
            return;
        }

        if (SelectedTool == CardHotbarTool.MachineGun)
        {
            showCircle = true;
            circleRadius = EffectiveMachineGunCrosshairRadiusPixels;
            circleThickness = crosshairThickness;
            return;
        }

        showStandard = true;
    }

    void FinalizeScopeLabel(ref bool showScopeLabel)
    {
        if (showScopeLabel)
        {
            showScopeLabel = _sniperScopeSwapPhase == 0 &&
                (SelectedTool == CardHotbarTool.AntiMaterialRifle ||
                    (SelectedTool != CardHotbarTool.HuntingRifle && IsMagnifiedSniperScope(_sniperScopeIndex)));
        }
    }

    float AssaultRifleFireInterval => 60f / Mathf.Max(1f, assaultRifleRpm);
    float SmgFireInterval => 60f / Mathf.Max(1f, smgRpm);
    float LmgFireInterval => 60f / Mathf.Max(1f, lmgRpm);
    float MachineGunFireInterval => 60f / Mathf.Max(1f, EffectiveMachineGunRpm);
    float EffectiveMachineGunRpm =>
        GunnerSuppressionBoostActive ? gunnerSuppressionBoostRpm : machineGunRpm;
    float EffectiveMachineGunCrosshairRadiusPixels =>
        GunnerSuppressionBoostActive
            ? machineGunCrosshairRadiusPixels * gunnerSuppressionBoostCrosshairRadiusMultiplier
            : machineGunCrosshairRadiusPixels;
    float CyborgLaserFireInterval => 60f / Mathf.Max(1f, cyborgLaserRpm);
    float SmgRecoilScale => AssaultRifleRecoilScale * 1.75f;
    float MachineGunRecoilScale => SmgRecoilScale * 0.6f;
    float MachinePistolRecoilScale => SmgRecoilScale * 1.5f;
    float LmgRecoilScale => AssaultRifleRecoilScale * 2f;
    float ScopedArRecoilScale =>
        AssaultRifleRecoilScale *
        ScopedArRecoilMultiplier *
        ScopedArAdsRecoilMultiplier() *
        ScopedArHoldBreathRecoilMultiplier();

    float ScopedArAdsRecoilMultiplier()
    {
        if (SelectedTool != CardHotbarTool.ScopedAssaultRifle || !_scopedArAdsHeld)
        {
            return 1f;
        }

        return ScopedArAdsRecoilScale;
    }

    float ScopedArHoldBreathRecoilMultiplier()
    {
        if (!_holdBreathActive || ActiveCardId() != "infantry_2")
        {
            return 1f;
        }

        return ScopedArHoldBreathRecoilMultiplierValue;
    }

    float ScopedArAdsFov => fieldOfView / Mathf.Max(1f, scopedArAdsMagnification);

    float HoldBreathRecoilMultiplier(bool ads)
    {
        if (!_holdBreathActive || ActiveCardId() != "infantry_2")
        {
            return 1f;
        }

        return ads ? 0.25f : 0.5f;
    }

    bool IsMarksmanRifleTool(CardHotbarTool tool)
    {
        return tool == CardHotbarTool.SniperRifle || tool == CardHotbarTool.HuntingRifle;
    }

    float HuntingRifleAdsFov => fieldOfView / Mathf.Max(1f, huntingRifleAdsMagnification);

    float AntiMaterialAdsFov => fieldOfView / Mathf.Max(1f, antiMaterialAdsMagnification);

    float HuntingRifleDrawSeconds => sniperDrawSeconds * 0.8f;

    int ActiveMarksmanAdsScopeIndex()
    {
        if (SelectedTool == CardHotbarTool.HuntingRifle)
        {
            return 0;
        }

        if (SelectedTool == CardHotbarTool.AntiMaterialRifle)
        {
            return AntiMaterialScopePresentationIndex;
        }

        return _sniperScopeIndex;
    }

    float MarksmanAdsFov()
    {
        return SelectedTool == CardHotbarTool.HuntingRifle
            ? HuntingRifleAdsFov
            : SniperScopeFieldOfView(_sniperScopeIndex);
    }

    float MarksmanHipFireCrosshairGap()
    {
        return SelectedTool == CardHotbarTool.HuntingRifle
            ? huntingRifleHipFireCrosshairGap
            : sniperHipFireCrosshairGap;
    }

    float MarksmanHipFireCrosshairLength()
    {
        return SelectedTool == CardHotbarTool.HuntingRifle
            ? huntingRifleHipFireCrosshairLength
            : sniperHipFireCrosshairLength;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _playerHealth = GetComponent<PlayerHealth>();
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
        _scopeSwayNoiseSeedX = Random.Range(0f, 100f);
        _scopeSwayNoiseSeedY = Random.Range(0f, 100f);
        PickNextScopeSwayPhase();

        MenuSettings.EnsureLoaded();
        ApplyMenuSettings();
        MenuSettings.Changed += ApplyMenuSettings;
    }

    PlayerHealth EnsurePlayerHealth()
    {
        if (_playerHealth == null)
        {
            _playerHealth = GetComponent<PlayerHealth>();
        }

        return _playerHealth;
    }

    public void InitializeNetworkController(bool localAuthority)
    {
        InitializeController(localAuthority);
    }

    public void SetRemoteAim(float yaw, float pitch)
    {
        if (_localAuthority)
        {
            return;
        }

        _yaw = yaw;
        _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        UpdateCharacterAimFromYaw();
    }

    public void SetNetworkTeam(GameSession.Team team)
    {
        _playerTeam = team;
    }

    void OnDestroy()
    {
        if (Local == this)
        {
            Local = null;
        }

        MenuSettings.Changed -= ApplyMenuSettings;
        if (_localAuthority)
        {
            ProfileSession.TouchActivity();
        }
    }

    void ApplyMenuSettings()
    {
        lookSensitivity = _baseLookSensitivity * MenuSettings.LookSensitivity;
    }

    float CurrentLookSensitivity()
    {
        float sensitivity = lookSensitivity;
        if (SelectedTool == CardHotbarTool.ScopedAssaultRifle && _scopedArAdsHeld)
        {
            float zoomFactor = Mathf.Clamp(_scopedArDisplayedFov / Mathf.Max(1f, fieldOfView), 0.08f, 1f);
            sensitivity *= MenuSettings.AdsSensitivity * zoomFactor;
        }
        else if (SelectedTool == CardHotbarTool.SniperRifle && _sniperAimingHeld)
        {
            float zoomFactor = Mathf.Clamp(_sniperDisplayedFov / Mathf.Max(1f, fieldOfView), 0.08f, 1f);
            sensitivity *= MenuSettings.AdsSensitivity * zoomFactor;
        }
        else if (SelectedTool == CardHotbarTool.HuntingRifle && _sniperAimingHeld)
        {
            float zoomFactor = Mathf.Clamp(_sniperDisplayedFov / Mathf.Max(1f, fieldOfView), 0.08f, 1f);
            sensitivity *= MenuSettings.AdsSensitivity * zoomFactor;
        }
        else if (SelectedTool == CardHotbarTool.AntiMaterialRifle && _sniperAimingHeld)
        {
            float zoomFactor = Mathf.Clamp(_sniperDisplayedFov / Mathf.Max(1f, fieldOfView), 0.04f, 1f);
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

        if (!_initialized)
        {
            return;
        }

        if (!_localAuthority)
        {
            UpdateCharacterAimFromYaw();
            return;
        }

        if (IsUiOverlayBlocking())
        {
            _selectorOpen = false;
            HidePreviewRoots();
            UpdateCameraTransform();
            UpdateHeldToolVisuals();
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
        if (deferStartUntilNetworkSpawn)
        {
            return;
        }

        InitializeController(localAuthority: true);
    }

    void InitializeController(bool localAuthority)
    {
        EnsurePlayerHealth();
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _localAuthority = localAuthority;
        _initialSpawnPosition = transform.position;

        if (viewCamera != null)
        {
            viewCamera.fieldOfView = fieldOfView;
            _sniperDisplayedFov = fieldOfView;
            _sniperFovTransitionTarget = fieldOfView;
            _sniperFovTransitionStart = fieldOfView;
            _scopedArDisplayedFov = fieldOfView;
            _scopedArFovTransitionTarget = fieldOfView;
            _scopedArFovTransitionStart = fieldOfView;
        }

        if (_localAuthority && hideLocalCharacterVisual && characterVisual != null)
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
        if (_localAuthority)
        {
            ProfileSession.EnsureInitialized();
            ProfileSession.TouchActivity();
        }
        _wasInPrepPhase = GameSession.IsInPrepPhase;
        _wasPrepReady = GameSession.IsPrepReady;

        if (_localAuthority)
        {
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

            Local = this;
            _playerTeam = GameSession.SelectedTeam;
            CreateHeldToolVisuals();
            RefreshHeldToolVisibility();
            BeginWeaponDraw(SelectedTool);
        }
    }

    void ApplyKitFromSession()
    {
        ClearPerCharacterGameplayEffects();
        _activeKit = GameSession.ActiveKit ?? CardKitDefinition.DefaultInfantryPlaceholder();
        _selectedHotbarIndex = Mathf.Clamp(_selectedHotbarIndex, 0, Mathf.Max(0, HotbarSlotCount - 1));
        _grenadeSlotSelected = false;
        ExitSniperAds();
        ExitScopedArAds();
        RefreshCardMoveSpeed();
        ResetAbilityState();
        ResetCyborgLaserHeat();
        ResetC4State(destroyCharge: true);
        CancelGrenadePrime();
        _grenadePostThrowSlotSwitchTimer = 0f;
        EnsurePlayerHealth()?.RefillHealth();
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

    public void HandlePlayerDeath()
    {
        ResetToSpawn();
    }

    void ApplySpawnReset(Vector3 respawnPosition)
    {
        ClearPerCharacterGameplayEffects();
        _selectorOpen = false;
        _grenadeSelectorOpen = false;
        _grenadeSlotSelected = false;
        _grenadeKeyHeld = false;
        _grenadeKeyHoldTimer = 0f;
        _grenadeWheelOpenedFromHold = false;
        CancelGrenadePrime();
        _grenadePostThrowSlotSwitchTimer = 0f;
        _grenadeHandCooldownTimer = 0f;
        _rectangleDragActive = false;
        _scrollTargetLocked = false;
        _hasBuildCandidate = false;
        HidePreviewRoots();

        _gunKickTimer = 0f;
        _muzzleFlashTimer = 0f;
        _hammerSwingTimer = 0f;
        _laserSwordSwingTimer = 0f;
        _laserSwordCooldownTimer = 0f;
        CancelAntiMaterialCharge();
        ResetC4State(destroyCharge: true);
        CancelGrenadePrime();
        ThrownGrenadeProjectile.DestroyAll();
        FlashbangBurstEffect.DestroyAll();
        ResetCyborgLaserHeat();
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

        if (!_initialized || !_localAuthority)
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
                if (_characterPicker.TryGoBack())
                {
                    return;
                }

                _characterPicker.Hide();
                return;
            }

            if (_respawnPicker != null && _respawnPicker.IsOpen)
            {
                if (_respawnPicker.TryGoBack())
                {
                    return;
                }

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

        bool uiOverlayBlocking = IsUiOverlayBlocking();
        UpdateContinuousGameplayState(allowAbilityInput: !uiOverlayBlocking);

        if (uiOverlayBlocking)
        {
            return;
        }

        UpdateSessionHeartbeat();

        if (GameSession.IsInPrepPhase && !GameSession.IsPrepReady)
        {
            return;
        }

        UpdateRadialSelectorInput();
        HandleLook();

        if (GameSession.IsInPrepPhase && GameSession.IsPrepReady)
        {
            HandleHotbarInput();
            HandleReloadInput();
            HandleAbilityInput();
            return;
        }

        HandleHotbarInput();
        HandleReloadInput();
        HandleAbilityInput();
        UpdateWeaponFireInputGate();
        HandleSelectedToolInput();

        if (Input.GetButtonDown("Jump") && CanJump() && !IsAntiMaterialBraceActive() && !IsRadialSelectorOpen)
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

        if (!_initialized || !_localAuthority)
        {
            return;
        }

        if (IsMovementBlocked())
        {
            StopHorizontalMovement();
            return;
        }

        if (_dashActive)
        {
            UpdateDashMovement();
            return;
        }

        if (IsAntiMaterialBraceActive())
        {
            UpdateGrounded();
            HandleAntiMaterialBraceMovement();
            return;
        }

        UpdateGrounded();
        HandleMovement();
    }

    void HandleAntiMaterialBraceMovement()
    {
        float orbitInput = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(orbitInput) > 0.01f)
        {
            _antiMaterialBraceOrbitAngle += orbitInput *
                antiMaterialBraceOrbitDegreesPerSecond *
                Mathf.Deg2Rad *
                Time.fixedDeltaTime;
        }

        Vector3 offset = new Vector3(
            Mathf.Sin(_antiMaterialBraceOrbitAngle),
            0f,
            Mathf.Cos(_antiMaterialBraceOrbitAngle)) * _antiMaterialBraceOrbitRadius;
        Vector3 targetPosition = _antiMaterialBraceAnchor + offset;
        targetPosition.y = _antiMaterialBraceTransformHeightAboveAnchor;

        var velocity = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
        _rb.MovePosition(targetPosition);
        AimAtAntiMaterialBraceAnchor();
    }

    Vector3 GetBraceOrbitForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, _antiMaterialBraceGroundNormal);
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, _antiMaterialBraceGroundNormal);
        }

        return forward.normalized;
    }

    Vector3 GetBraceOrbitRight()
    {
        return Vector3.Cross(_antiMaterialBraceGroundNormal, GetBraceOrbitForward()).normalized;
    }

    void AimAtAntiMaterialBraceAnchor()
    {
        Vector3 flatToAnchor = _antiMaterialBraceAnchor - transform.position;
        flatToAnchor.y = 0f;
        if (flatToAnchor.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        flatToAnchor.Normalize();
        _antiMaterialBraceBaseYaw = Mathf.Atan2(flatToAnchor.x, flatToAnchor.z) * Mathf.Rad2Deg;
        _yaw = _antiMaterialBraceBaseYaw + _antiMaterialBraceGunTilt.x;
        _pitch = Mathf.Clamp(_antiMaterialBraceGunTilt.y, minPitch, maxPitch);
    }

    bool TrySampleBraceGround(Vector3 probePoint, out Vector3 groundPoint, out Vector3 groundNormal)
    {
        groundPoint = probePoint;
        groundNormal = Vector3.up;
        Vector3 rayStart = probePoint + (_antiMaterialBraceGroundNormal * 2f);
        if (!Physics.Raycast(
                rayStart,
                -_antiMaterialBraceGroundNormal,
                out RaycastHit hit,
                4f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            rayStart = probePoint + (Vector3.up * 2f);
            if (!Physics.Raycast(
                    rayStart,
                    Vector3.down,
                    out hit,
                    4f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }

        if (Vector3.Angle(hit.normal, Vector3.up) > AntiMaterialBraceMaxGroundSlopeDegrees)
        {
            return false;
        }

        groundPoint = hit.point;
        groundNormal = hit.normal;
        return true;
    }

    bool TryFindBraceAnchorPoint(Vector3 desiredWorldPoint, out Vector3 anchorPoint, out Vector3 groundNormal)
    {
        Vector3 probe = desiredWorldPoint + (Vector3.up * 1.5f);
        if (TrySampleBraceGround(probe, out anchorPoint, out groundNormal))
        {
            return true;
        }

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude <= 0.0001f)
        {
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();
        for (int i = 1; i <= 3; i++)
        {
            probe = desiredWorldPoint - (flatForward * (0.15f * i)) + (Vector3.up * 1.5f);
            if (TrySampleBraceGround(probe, out anchorPoint, out groundNormal))
            {
                return true;
            }
        }

        anchorPoint = desiredWorldPoint;
        groundNormal = Vector3.up;
        return false;
    }

    void UpdateDashMovement()
    {
        float dashSpeed = skirmisherDashDistanceMeters / Mathf.Max(0.01f, skirmisherDashDurationSeconds);
        var velocity = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(_dashDirection.x * dashSpeed, velocity.y, _dashDirection.z * dashSpeed);
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

        if (IsRadialSelectorOpen)
        {
            return;
        }

        if (IsAntiMaterialBraceActive())
        {
            float sensitivity = CurrentLookSensitivity();
            float verticalTiltRange = antiMaterialBraceGunTiltDegrees * 0.65f * AntiMaterialBraceVerticalTiltFraction;
            _antiMaterialBraceGunTilt.x = Mathf.Clamp(
                _antiMaterialBraceGunTilt.x + (lookDelta.x * sensitivity),
                -antiMaterialBraceGunTiltDegrees,
                antiMaterialBraceGunTiltDegrees);
            _antiMaterialBraceGunTilt.y = Mathf.Clamp(
                _antiMaterialBraceGunTilt.y - (lookDelta.y * sensitivity),
                -verticalTiltRange,
                verticalTiltRange);

            AimAtAntiMaterialBraceAnchor();
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
        if (viewCamera == null || !TryGetScopeSwayMultiplier(out float swayMultiplier))
        {
            ResetScopeSwayEnvelope();
            return;
        }

        float adsBlend = ScopeSwayAdsBlend();
        if (adsBlend <= 0.001f)
        {
            ResetScopeSwayEnvelope();
            return;
        }

        TickScopeSwayEnvelope();

        float sway = sniperScopeSwayDegrees *
            swayMultiplier *
            adsBlend *
            ScopeSwayRangeMultiplier *
            _scopeSwayIntensity;
        float motionSpeed = ScopeSwaySpeedMultiplier() * _scopeSwaySpeedFactor * ScopeSwayGlobalSpeedScale;
        _scopeSwayOscillationPhase += Time.deltaTime * motionSpeed;
        float phase = _scopeSwayOscillationPhase;
        float smoothX = Mathf.Sin((phase + _scopeSwayNoiseSeedX * 0.01f) * 7.6f);
        float smoothY = Mathf.Cos((phase + _scopeSwayNoiseSeedY * 0.01f) * 5.9f);
        float randomX = (Mathf.PerlinNoise(_scopeSwayNoiseSeedX, phase * 0.31f) - 0.5f) * 2f;
        float randomY = (Mathf.PerlinNoise(_scopeSwayNoiseSeedY, phase * 0.27f) - 0.5f) * 2f;
        float fastMotion = Mathf.InverseLerp(0.75f, 1.25f, _scopeSwaySpeedFactor);
        float blend = Mathf.Lerp(ScopeSwayRandomBlend, ScopeSwayRandomBlend * 0.1f, fastMotion);
        _sniperScopeSway = new Vector2(
            (smoothX * (1f - blend) + randomX * blend) * sway,
            (smoothY * (1f - blend) + randomY * blend) * sway * 0.75f);
    }

    void TickScopeSwayEnvelope()
    {
        if (_scopeSwayPhaseTimer <= 0f)
        {
            PickNextScopeSwayPhase();
        }

        _scopeSwayPhaseTimer -= Time.deltaTime;
        float fastMotion = Mathf.InverseLerp(
            0.7f,
            1.25f,
            Mathf.Max(_scopeSwaySpeedFactor, _scopeSwayTargetSpeedFactor));
        float intensitySmoothTime = Mathf.Lerp(0.9f, 1.85f, fastMotion);
        float speedSmoothTime = Mathf.Lerp(0.7f, 1.55f, fastMotion);
        _scopeSwayIntensity = Mathf.SmoothDamp(
            _scopeSwayIntensity,
            _scopeSwayTargetIntensity,
            ref _scopeSwayIntensityVelocity,
            intensitySmoothTime);
        _scopeSwaySpeedFactor = Mathf.SmoothDamp(
            _scopeSwaySpeedFactor,
            _scopeSwayTargetSpeedFactor,
            ref _scopeSwaySpeedFactorVelocity,
            speedSmoothTime);
    }

    void PickNextScopeSwayPhase()
    {
        int phaseKind = RollScopeSwayPhaseKind();
        ApplyScopeSwayPhaseKind(phaseKind);
        if (IsFastScopeSwayPhase(phaseKind))
        {
            _scopeSwayFastStreak++;
        }
        else
        {
            _scopeSwayFastStreak = 0;
        }

        _scopeSwayLastPhaseKind = phaseKind;
    }

    int RollScopeSwayPhaseKind()
    {
        float wideWeight = 0.38f;
        float stillWeight = 0.16f;
        float lightWeight = 0.2f;
        float slowDriftWeight = 0.2f;
        float heavyWeight = 0.06f;

        if (_scopeSwayFastStreak >= 1 || _scopeSwayLastPhaseKind == ScopeSwayPhaseHeavy)
        {
            heavyWeight = 0f;
            wideWeight += 0.04f;
            slowDriftWeight += 0.02f;
        }

        if (_scopeSwayLastPhaseKind == ScopeSwayPhaseHeavy)
        {
            stillWeight += 0.08f;
            wideWeight += 0.04f;
            lightWeight -= 0.04f;
        }

        float totalWeight = wideWeight + stillWeight + lightWeight + slowDriftWeight + heavyWeight;
        float roll = Random.value * totalWeight;
        if ((roll -= wideWeight) <= 0f)
        {
            return ScopeSwayPhaseWideLong;
        }

        if ((roll -= stillWeight) <= 0f)
        {
            return ScopeSwayPhaseStill;
        }

        if ((roll -= lightWeight) <= 0f)
        {
            return ScopeSwayPhaseLight;
        }

        if ((roll -= slowDriftWeight) <= 0f)
        {
            return ScopeSwayPhaseSlowDrift;
        }

        return ScopeSwayPhaseHeavy;
    }

    void ApplyScopeSwayPhaseKind(int phaseKind)
    {
        switch (phaseKind)
        {
            case ScopeSwayPhaseStill:
                _scopeSwayTargetIntensity = Random.Range(0.04f, 0.14f);
                _scopeSwayTargetSpeedFactor = Random.Range(0.25f, 0.48f);
                _scopeSwayPhaseTimer = Random.Range(2.8f, 5.5f);
                break;
            case ScopeSwayPhaseLight:
                _scopeSwayTargetIntensity = Random.Range(0.48f, 0.74f);
                _scopeSwayTargetSpeedFactor = Random.Range(0.55f, 0.82f);
                _scopeSwayPhaseTimer = Random.Range(2.2f, 3.8f);
                break;
            case ScopeSwayPhaseWideLong:
                _scopeSwayTargetIntensity = Random.Range(1.55f, 2.35f);
                _scopeSwayTargetSpeedFactor = Random.Range(0.28f, 0.48f);
                _scopeSwayPhaseTimer = Random.Range(5f, 8.5f);
                break;
            case ScopeSwayPhaseSlowDrift:
                _scopeSwayTargetIntensity = Random.Range(0.75f, 1.15f);
                _scopeSwayTargetSpeedFactor = Random.Range(0.16f, 0.34f);
                _scopeSwayPhaseTimer = Random.Range(3.8f, 6.5f);
                break;
            default:
                _scopeSwayTargetIntensity = Random.Range(1.15f, 1.65f);
                _scopeSwayTargetSpeedFactor = Random.Range(0.95f, 1.22f);
                _scopeSwayPhaseTimer = Random.Range(2f, 3.4f);
                if (_scopeSwaySpeedFactor > 0.85f)
                {
                    _scopeSwayTargetSpeedFactor = Mathf.Max(
                        _scopeSwayTargetSpeedFactor,
                        _scopeSwaySpeedFactor * 0.82f);
                }

                break;
        }
    }

    static bool IsFastScopeSwayPhase(int phaseKind)
    {
        return phaseKind == ScopeSwayPhaseHeavy;
    }

    void ResetScopeSwayEnvelope()
    {
        _scopeSwayPhaseTimer = 0f;
        _scopeSwayLastPhaseKind = -1;
        _scopeSwayFastStreak = 0;
    }

    bool TryGetScopeSwayMultiplier(out float swayMultiplier)
    {
        swayMultiplier = 0f;
        switch (SelectedTool)
        {
            case CardHotbarTool.SniperRifle:
                if (!_sniperAimingHeld)
                {
                    return false;
                }

                swayMultiplier = 1f;
                return true;
            case CardHotbarTool.HuntingRifle:
                if (!_sniperAimingHeld)
                {
                    return false;
                }

                swayMultiplier = HuntingRifleScopeSwayMultiplier;
                return true;
            case CardHotbarTool.AntiMaterialRifle:
                if (!_sniperAimingHeld)
                {
                    return false;
                }

                swayMultiplier = IsAntiMaterialBraceActive()
                    ? AntiMaterialBraceScopeSwayMultiplier
                    : AntiMaterialScopeSwayMultiplier;
                return true;
            case CardHotbarTool.ScopedAssaultRifle:
                if (!_scopedArAdsHeld)
                {
                    return false;
                }

                swayMultiplier = ScopedArScopeSwayMultiplier;
                return true;
            default:
                return false;
        }
    }

    float ScopeSwayAdsBlend()
    {
        if (SelectedTool == CardHotbarTool.ScopedAssaultRifle)
        {
            return _scopedArScopeOverlayBlend;
        }

        if (SelectedTool == CardHotbarTool.SniperRifle ||
            SelectedTool == CardHotbarTool.HuntingRifle ||
            SelectedTool == CardHotbarTool.AntiMaterialRifle)
        {
            return SniperAdsAccuracyFactor();
        }

        return 0f;
    }

    float ScopeSwaySpeedMultiplier()
    {
        switch (SelectedTool)
        {
            case CardHotbarTool.SniperRifle:
                return SniperScopeSwaySpeedMultiplier;
            case CardHotbarTool.HuntingRifle:
                return HuntingRifleScopeSwaySpeedMultiplier;
            case CardHotbarTool.AntiMaterialRifle:
                return IsAntiMaterialBraceActive()
                    ? AntiMaterialBraceScopeSwaySpeedMultiplier
                    : AntiMaterialScopeSwaySpeedMultiplier;
            case CardHotbarTool.ScopedAssaultRifle:
                if (_holdBreathActive && ActiveCardId() == "infantry_2")
                {
                    return ScopedArHoldBreathScopeSwaySpeedMultiplier;
                }

                return ScopedArScopeSwaySpeedMultiplier;
            default:
                return 1f;
        }
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
        return BuildCrosshairAimRay();
    }

    Ray BuildCrosshairAimRay(Vector2 screenPixelOffset = default)
    {
        SyncViewCameraForAim();
        if (viewCamera != null)
        {
            if (screenPixelOffset == Vector2.zero)
            {
                return viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            }

            return viewCamera.ScreenPointToRay(new Vector3(
                (Screen.width * 0.5f) + screenPixelOffset.x,
                (Screen.height * 0.5f) + screenPixelOffset.y,
                0f));
        }

        return BuildFallbackAimRay();
    }

    Ray BuildFallbackAimRay()
    {
        Vector3 origin = cameraYawPivot != null
            ? cameraYawPivot.position
            : transform.position + Vector3.up * eyeHeight;
        Vector3 forward = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(_pitch, 0f, 0f) * Vector3.forward;
        return new Ray(origin, forward);
    }

    void SyncViewCameraForAim()
    {
        if (cameraYawPivot == null || cameraPitchPivot == null || viewCamera == null)
        {
            return;
        }

        UpdateCameraTransform();
    }

    void ApplyGunRecoilToAim()
    {
        _yaw += _gunRecoilResidual.x;
        _pitch = Mathf.Clamp(_pitch - _gunRecoilResidual.y, minPitch, maxPitch);
    }

    void UpdateCharacterAim()
    {
        if (characterVisual == null)
        {
            return;
        }

        if (viewCamera == null)
        {
            UpdateCharacterAimFromYaw();
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

    void UpdateCharacterAimFromYaw()
    {
        if (characterVisual == null)
        {
            return;
        }

        Vector3 aimDirection = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(aimDirection.normalized, Vector3.up);
        characterVisual.rotation = Quaternion.Slerp(characterVisual.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void HandleMovement()
    {
        if (IsAntiMaterialBraceActive())
        {
            return;
        }

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
        var targetHorizontal = wishDirection * (moveSpeed * WeaponHandlingSpeedFactor() * SuppressionSpeedFactor());

        var velocity = _rb.linearVelocity;
        var horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float accel = _grounded ? acceleration : airAcceleration;
        horizontal = Vector3.MoveTowards(horizontal, targetHorizontal, accel * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        // Character facing follows the crosshair in LateUpdate.
    }

    // Heavier weapons slow movement while held, firing, or aiming down sights.
    float WeaponHandlingSpeedFactor()
    {
        bool firing = _weaponFireSlowTimer > 0f;
        bool ads = (_sniperAimingHeld &&
            (SelectedTool == CardHotbarTool.SniperRifle ||
                SelectedTool == CardHotbarTool.HuntingRifle ||
                SelectedTool == CardHotbarTool.AntiMaterialRifle)) ||
            (SelectedTool == CardHotbarTool.ScopedAssaultRifle && _scopedArAdsHeld);

        switch (SelectedTool)
        {
            case CardHotbarTool.SniperRifle:
                return firing ? 0.3f : 0.7f;
            case CardHotbarTool.AntiMaterialRifle:
                if (firing)
                {
                    return 0.2f;
                }

                return ads ? 0.2f : 0.5f;
            case CardHotbarTool.HuntingRifle:
                return firing || ads ? 0.45f : 0.85f;
            case CardHotbarTool.LightMachineGun:
                return firing ? 0.45f : 0.7f;
            case CardHotbarTool.MachineGun:
                return firing ? 0.45f : 0.7f;
            case CardHotbarTool.CyborgLaser:
                return firing ? 0.75f : 0.95f;
            case CardHotbarTool.AssaultRifle:
                return firing ? 0.6f : 1f;
            case CardHotbarTool.ScopedAssaultRifle:
                return firing || ads ? 0.6f : 1f;
            case CardHotbarTool.Smg:
                return firing ? 0.9f : 1f;
            default:
                return 1f;
        }
    }

    float SuppressionSpeedFactor()
    {
        return MachineGunSuppressionUtility.SpeedFactor(
            _machineGunSuppressionRemaining,
            _machineGunSuppressionSpeedMultiplier);
    }

    public void ApplyMachineGunSuppression(bool enhancedSuppression)
    {
        bool wasSuppressed = _machineGunSuppressionRemaining > 0f;
        float speedMultiplier = enhancedSuppression
            ? MachineGunSuppressionUtility.BoostedSpeedMultiplier
            : MachineGunSuppressionUtility.DefaultSpeedMultiplier;
        float flickScale = enhancedSuppression
            ? gunnerSuppressionBoostFlickIntensityMultiplier
            : 1f;

        MachineGunSuppressionUtility.Apply(
            ref _machineGunSuppressionRemaining,
            machineGunSuppressionDurationSeconds);
        MachineGunSuppressionUtility.ApplySpeedMultiplier(
            ref _machineGunSuppressionSpeedMultiplier,
            speedMultiplier,
            wasSuppressed);

        if (this == Local)
        {
            PlayerBulletHitFlash.Instance?.FlickFromGunshot(flickScale);
        }
    }

    void UpdateMachineGunSuppression()
    {
        MachineGunSuppressionUtility.Tick(ref _machineGunSuppressionRemaining, Time.deltaTime);
        if (_machineGunSuppressionRemaining <= 0f)
        {
            _machineGunSuppressionSpeedMultiplier = 1f;
        }
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
        _pistolFamilyReserve = WeaponAmmoDefaults.PistolStartReserve;
        _pistolAmmo = new WeaponAmmoPool(
            _pistolFamilyReserve,
            WeaponAmmoDefaults.PistolMagSize,
            WeaponAmmoDefaults.PistolMagSize,
            WeaponAmmoDefaults.PistolMaxTotal);
        _smgAmmo = new WeaponAmmoPool(
            _pistolFamilyReserve,
            WeaponAmmoDefaults.SmgMagSize,
            WeaponAmmoDefaults.SmgMagSize,
            WeaponAmmoDefaults.SmgMaxTotal);
        _machinePistolAmmo = new WeaponAmmoPool(
            _pistolFamilyReserve,
            WeaponAmmoDefaults.MachinePistolMagSize,
            WeaponAmmoDefaults.MachinePistolMagSize,
            WeaponAmmoDefaults.MachinePistolMaxTotal);

        _rifleFamilyReserve = WeaponAmmoDefaults.AssaultRifleStartReserve;
        _assaultRifleAmmo = new WeaponAmmoPool(
            _rifleFamilyReserve,
            WeaponAmmoDefaults.AssaultRifleMagSize,
            WeaponAmmoDefaults.AssaultRifleMagSize,
            WeaponAmmoDefaults.AssaultRifleMaxTotal);
        _lmgAmmo = new WeaponAmmoPool(
            _rifleFamilyReserve,
            WeaponAmmoDefaults.LmgMagSize,
            WeaponAmmoDefaults.LmgMagSize,
            WeaponAmmoDefaults.LmgMaxTotal);
        _machineGunAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.MachineGunStartReserve,
            WeaponAmmoDefaults.MachineGunMagSize,
            WeaponAmmoDefaults.MachineGunMagSize,
            WeaponAmmoDefaults.MachineGunMaxTotal);

        _sniperAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.SniperStartReserve,
            WeaponAmmoDefaults.SniperMagSize,
            WeaponAmmoDefaults.SniperMagSize,
            WeaponAmmoDefaults.SniperMaxTotal);
        _huntingRifleAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.HuntingRifleStartReserve,
            WeaponAmmoDefaults.HuntingRifleMagSize,
            WeaponAmmoDefaults.HuntingRifleMagSize,
            WeaponAmmoDefaults.HuntingRifleMaxTotal);
        _antiMaterialAmmo = new WeaponAmmoPool(
            WeaponAmmoDefaults.AntiMaterialStartReserve,
            WeaponAmmoDefaults.AntiMaterialMagSize,
            WeaponAmmoDefaults.AntiMaterialMagSize,
            WeaponAmmoDefaults.AntiMaterialMaxTotal);
        _c4Ammo = new WeaponAmmoPool(
            0,
            WeaponAmmoDefaults.C4MagSize,
            WeaponAmmoDefaults.C4MagSize,
            WeaponAmmoDefaults.C4MaxTotal);
        SyncPistolFamilyReserve();
        SyncRifleFamilyReserve();
        ResetAmmoRechargeTimers();
        ResetGrenadeInventory();
    }

    void ResetGrenadeInventory()
    {
        _fragGrenadesRemaining = FragGrenadesPerLife;
        _flashbangGrenadesRemaining = FlashbangGrenadesPerLife;
        EnsureSelectedGrenadeAvailable();
    }

    int GetGrenadeCount(GrenadeType grenadeType)
    {
        return grenadeType == GrenadeType.Flashbang
            ? _flashbangGrenadesRemaining
            : _fragGrenadesRemaining;
    }

    void EnsureSelectedGrenadeAvailable()
    {
        if (GetGrenadeCount(_selectedGrenade) > 0)
        {
            return;
        }

        if (_fragGrenadesRemaining > 0)
        {
            _selectedGrenade = GrenadeType.Frag;
            return;
        }

        if (_flashbangGrenadesRemaining > 0)
        {
            _selectedGrenade = GrenadeType.Flashbang;
        }
    }

    void ConsumeThrownGrenade(GrenadeType grenadeType)
    {
        switch (grenadeType)
        {
            case GrenadeType.Flashbang:
                if (_flashbangGrenadesRemaining > 0)
                {
                    _flashbangGrenadesRemaining--;
                }

                break;
            default:
                if (_fragGrenadesRemaining > 0)
                {
                    _fragGrenadesRemaining--;
                }

                break;
        }

        EnsureSelectedGrenadeAvailable();
        if (!HasAnyGrenadesRemaining && _grenadeSlotSelected)
        {
            _grenadePostThrowSlotSwitchTimer = GrenadePostThrowSlotSwitchSeconds;
        }
    }

    void ResetAmmoRechargeTimers()
    {
        _pistolFamilyRechargeTimer = 0f;
        _rifleFamilyRechargeTimer = 0f;
        _machineGunRechargeTimer = 0f;
        _sniperRechargeTimer = 0f;
        _huntingRifleRechargeTimer = 0f;
        _antiMaterialRechargeTimer = 0f;
        _c4RechargeTimer = 0f;
    }

    void SyncPistolFamilyReserve()
    {
        _pistolAmmo.SyncReserveFromShared(_pistolFamilyReserve);
        _smgAmmo.SyncReserveFromShared(_pistolFamilyReserve);
        _machinePistolAmmo.SyncReserveFromShared(_pistolFamilyReserve);
    }

    void SyncRifleFamilyReserve()
    {
        _assaultRifleAmmo.SyncReserveFromShared(_rifleFamilyReserve);
        _lmgAmmo.SyncReserveFromShared(_rifleFamilyReserve);
    }

    static bool IsPistolFamilyWeapon(CardHotbarTool weapon)
    {
        return weapon == CardHotbarTool.Pistol ||
            weapon == CardHotbarTool.Smg ||
            weapon == CardHotbarTool.MachinePistol;
    }

    static bool IsRifleFamilyWeapon(CardHotbarTool weapon)
    {
        return weapon == CardHotbarTool.AssaultRifle ||
            weapon == CardHotbarTool.ScopedAssaultRifle ||
            weapon == CardHotbarTool.LightMachineGun;
    }

    void RefillMagFromFamilyReserve(CardHotbarTool weapon)
    {
        ref WeaponAmmoPool pool = ref GetAmmoPoolRef(weapon);
        if (IsPistolFamilyWeapon(weapon))
        {
            pool.FillMagFromSharedReserve(ref _pistolFamilyReserve);
            SyncPistolFamilyReserve();
            return;
        }

        if (IsRifleFamilyWeapon(weapon))
        {
            pool.FillMagFromSharedReserve(ref _rifleFamilyReserve);
            SyncRifleFamilyReserve();
        }
    }

    ref WeaponAmmoPool GetAmmoPoolRef(CardHotbarTool weapon)
    {
        switch (weapon)
        {
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
                return ref _assaultRifleAmmo;
            case CardHotbarTool.LightMachineGun:
                return ref _lmgAmmo;
            case CardHotbarTool.MachineGun:
                return ref _machineGunAmmo;
            case CardHotbarTool.SniperRifle:
                return ref _sniperAmmo;
            case CardHotbarTool.HuntingRifle:
                return ref _huntingRifleAmmo;
            case CardHotbarTool.AntiMaterialRifle:
                return ref _antiMaterialAmmo;
            case CardHotbarTool.Smg:
                return ref _smgAmmo;
            case CardHotbarTool.MachinePistol:
                return ref _machinePistolAmmo;
            default:
                return ref _pistolAmmo;
        }
    }

    WeaponAmmoPool GetAmmoPoolForSelectedTool()
    {
        switch (SelectedTool)
        {
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
                SyncRifleFamilyReserve();
                return _assaultRifleAmmo;
            case CardHotbarTool.LightMachineGun:
                SyncRifleFamilyReserve();
                return _lmgAmmo;
            case CardHotbarTool.MachineGun:
                return _machineGunAmmo;
            case CardHotbarTool.SniperRifle:
                return _sniperAmmo;
            case CardHotbarTool.HuntingRifle:
                return _huntingRifleAmmo;
            case CardHotbarTool.AntiMaterialRifle:
                return _antiMaterialAmmo;
            case CardHotbarTool.Smg:
                SyncPistolFamilyReserve();
                return _smgAmmo;
            case CardHotbarTool.MachinePistol:
                SyncPistolFamilyReserve();
                return _machinePistolAmmo;
            case CardHotbarTool.Pistol:
                SyncPistolFamilyReserve();
                return _pistolAmmo;
            case CardHotbarTool.C4Charge:
                return _c4Ammo;
            default:
                return default;
        }
    }

    static bool IsFirearmTool(CardHotbarTool tool)
    {
        return CardKitDefinition.IsFirearm(tool);
    }

    static bool IsDrawBlockingTool(CardHotbarTool tool)
    {
        return IsFirearmTool(tool) || tool == CardHotbarTool.C4Charge || tool == CardHotbarTool.Grenade;
    }

    bool IsGrenadeInHand()
    {
        return _grenadePrimed || _grenadePostThrowSlotSwitchTimer > 0f;
    }

    bool IsGrenadeHandCooldownActive()
    {
        return _grenadeHandCooldownTimer > 0f;
    }

    void BeginGrenadeHandCooldown()
    {
        _grenadeHandCooldownTimer = GrenadeHandCooldownSeconds;
    }

    bool IsReloadFullyLocked()
    {
        if (!_isReloading)
        {
            return false;
        }

        switch (_reloadWeapon)
        {
            case CardHotbarTool.SniperRifle:
                return _sniperReloadLocked;
            case CardHotbarTool.HuntingRifle:
            case CardHotbarTool.AntiMaterialRifle:
                return true;
            default:
                return true;
        }
    }

    float WeaponOverlayFill(CardHotbarTool tool)
    {
        float fill = 0f;

        if (_drawingWeapon == tool && _weaponDrawDuration > 0f && _weaponDrawTimer > 0f)
        {
            fill = Mathf.Max(fill, Mathf.Clamp01(_weaponDrawTimer / _weaponDrawDuration));
        }

        if (tool == CardHotbarTool.LaserSword && _laserSwordCooldownTimer > 0f)
        {
            fill = Mathf.Max(fill, laserSwordCooldownSeconds <= 0f
                ? 0f
                : Mathf.Clamp01(_laserSwordCooldownTimer / laserSwordCooldownSeconds));
        }

        if (tool == CardHotbarTool.C4Charge && _c4ActionLockTimer > 0f)
        {
            fill = Mathf.Max(fill, c4ThrowLockSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_c4ActionLockTimer / c4ThrowLockSeconds));
        }

        if (tool == CardHotbarTool.Grenade)
        {
            if (_grenadeHandCooldownTimer > 0f)
            {
                fill = Mathf.Max(fill, GrenadeHandCooldownSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(_grenadeHandCooldownTimer / GrenadeHandCooldownSeconds));
            }

            if (_grenadePostThrowSlotSwitchTimer > 0f)
            {
                fill = Mathf.Max(fill, GrenadePostThrowSlotSwitchSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(_grenadePostThrowSlotSwitchTimer / GrenadePostThrowSlotSwitchSeconds));
            }
        }

        return fill;
    }

    float SwitchLockOverlayFill()
    {
        if (IsRadialSelectorOpen || IsBlindnessBlockingInput() || _antiMaterialBraceActive)
        {
            return 1f;
        }

        if (IsC4ActionLocked())
        {
            return c4ThrowLockSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_c4ActionLockTimer / c4ThrowLockSeconds);
        }

        if (IsReloadFullyLocked())
        {
            return Mathf.Max(0.001f, ReloadOverlayFill());
        }

        if (IsLaserSwordHotbarLocked())
        {
            return laserSwordCooldownSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_laserSwordCooldownTimer / laserSwordCooldownSeconds);
        }

        if (IsGrenadeInHand())
        {
            return _grenadePostThrowSlotSwitchTimer > 0f && GrenadePostThrowSlotSwitchSeconds > 0f
                ? Mathf.Clamp01(_grenadePostThrowSlotSwitchTimer / GrenadePostThrowSlotSwitchSeconds)
                : 1f;
        }

        return 0f;
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
                return ReloadDuration(CardHotbarTool.Pistol) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.Pistol));
            case CardHotbarTool.Smg:
                return ReloadDuration(CardHotbarTool.Smg) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.Smg));
            case CardHotbarTool.MachinePistol:
                return ReloadDuration(CardHotbarTool.MachinePistol) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.MachinePistol));
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
                return ReloadDuration(CardHotbarTool.AssaultRifle) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.AssaultRifle));
            case CardHotbarTool.LightMachineGun:
                return ReloadDuration(CardHotbarTool.LightMachineGun) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.LightMachineGun));
            case CardHotbarTool.MachineGun:
                return ReloadDuration(CardHotbarTool.MachineGun) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.MachineGun));
            case CardHotbarTool.HuntingRifle:
                return ReloadDuration(CardHotbarTool.HuntingRifle) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.HuntingRifle));
            case CardHotbarTool.AntiMaterialRifle:
                return ReloadDuration(CardHotbarTool.AntiMaterialRifle) <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / ReloadDuration(CardHotbarTool.AntiMaterialRifle));
            case CardHotbarTool.SniperRifle:
                if (_sniperReloadPhase == 0)
                {
                    float lockedTotal = ReloadDuration(CardHotbarTool.SniperRifle) + SniperRoundReloadDuration();
                    float remaining = _reloadTimer + SniperRoundReloadDuration();
                    return lockedTotal <= 0f ? 0f : Mathf.Clamp01(remaining / lockedTotal);
                }

                return SniperRoundReloadDuration() <= 0f
                    ? 0f
                    : Mathf.Clamp01(_reloadTimer / SniperRoundReloadDuration());
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

        if (IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsC4ActionLocked() ||
            _isReloading ||
            _weaponFireCooldown > 0f ||
            !IsFirearmTool(SelectedTool) ||
            IsWeaponDrawInProgress())
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

        if (weapon == CardHotbarTool.SniperRifle ||
            weapon == CardHotbarTool.HuntingRifle ||
            weapon == CardHotbarTool.AntiMaterialRifle)
        {
            ExitSniperAds();
            _sniperReloadPhase = 0;
            _sniperReloadLocked = weapon == CardHotbarTool.SniperRifle;
            _reloadTimer = weapon switch
            {
                CardHotbarTool.HuntingRifle => ReloadDuration(CardHotbarTool.HuntingRifle),
                CardHotbarTool.AntiMaterialRifle => ReloadDuration(CardHotbarTool.AntiMaterialRifle),
                _ => ReloadDuration(CardHotbarTool.SniperRifle)
            };
            return;
        }

        if (weapon == CardHotbarTool.ScopedAssaultRifle)
        {
            ExitScopedArAds();
        }

        _sniperReloadPhase = 0;
        _sniperReloadLocked = false;
        _reloadTimer = ReloadDuration(_reloadWeapon);
    }

    void CancelReload()
    {
        _isReloading = false;
        _reloadWeapon = default;
        _reloadTimer = 0f;
        _sniperReloadPhase = 0;
        _sniperReloadLocked = false;
        _sniperRoundPulseTimer = 0f;
        CancelAntiMaterialCharge();
    }

    void CompleteReload()
    {
        CancelReload();
    }

    bool ShouldShowReloadGunDip()
    {
        if (_reloadWeapon == CardHotbarTool.AntiMaterialRifle && IsAntiMaterialBraceActive())
        {
            return false;
        }

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
        if (_isReloading && IsBlindnessBlockingInput())
        {
            CancelReload();
            return;
        }

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
            case CardHotbarTool.Smg:
            case CardHotbarTool.MachinePistol:
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
            case CardHotbarTool.LightMachineGun:
                RefillMagFromFamilyReserve(_reloadWeapon);
                CompleteReload();
                break;
            case CardHotbarTool.MachineGun:
                _machineGunAmmo.FillMagFromReserve();
                CompleteReload();
                break;
            case CardHotbarTool.HuntingRifle:
                _huntingRifleAmmo.LoadSingleRound();
                CompleteReload();
                break;
            case CardHotbarTool.AntiMaterialRifle:
                _antiMaterialAmmo.LoadSingleRound();
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
            _reloadTimer = SniperRoundReloadDuration();
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
            _reloadTimer = SniperRoundReloadDuration();
            return;
        }

        CompleteReload();
    }

    void HandleHotbarInput()
    {
        if (IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsC4ActionLocked() ||
            _antiMaterialBraceActive ||
            IsReloadFullyLocked() ||
            IsLaserSwordHotbarLocked() ||
            IsGrenadeInHand())
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f)
        {
            if (_grenadeSlotSelected)
            {
                SelectHotbarIndex(2);
            }
            else if (_selectedHotbarIndex == 1)
            {
                SelectGrenadeHotbarSlot();
            }
            else
            {
                SelectHotbarIndex(NextHotbarIndex(1));
            }
        }
        else if (scroll < -0.01f)
        {
            if (_grenadeSlotSelected)
            {
                SelectHotbarIndex(1);
            }
            else if (_selectedHotbarIndex == 2)
            {
                SelectGrenadeHotbarSlot();
            }
            else
            {
                SelectHotbarIndex(NextHotbarIndex(-1));
            }
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
        if (IsLaserSwordHotbarLocked() && index != GetHotbarIndexForTool(CardHotbarTool.LaserSword))
        {
            return;
        }

        if (IsGrenadeInHand())
        {
            return;
        }

        if (_grenadeSlotSelected)
        {
            _grenadeSlotSelected = false;
        }
        else if (_selectedHotbarIndex == index)
        {
            return;
        }

        bool wasBuilding = BuildModeActive;
        if (_antiMaterialBraceActive)
        {
            EndAntiMaterialBrace(applyCooldown: true);
        }

        if (_isReloading && _reloadWeapon != CardHotbarTool.HuntingRifle &&
            _reloadWeapon == CardHotbarTool.SniperRifle && !_sniperReloadLocked)
        {
            CancelReload();
        }

        _selectedHotbarIndex = index;
        _weaponFireCooldown = 0f;
        if (SelectedTool != CardHotbarTool.C4Charge)
        {
            _c4RemoteDrawTimer = 0f;
            _c4RemoteReady = false;
        }

        ExitSniperAds();
        ExitScopedArAds();
        if (wasBuilding || BuildModeActive)
        {
            ClearBuildInteractionState();
        }

        RefreshHeldToolVisibility();
        BeginWeaponDraw(SelectedTool);
    }

    void SelectGrenadeHotbarSlot()
    {
        if (IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsC4ActionLocked() ||
            _antiMaterialBraceActive ||
            IsReloadFullyLocked() ||
            IsLaserSwordHotbarLocked() ||
            IsGrenadeInHand() ||
            IsGrenadeHandCooldownActive() ||
            !HasAnyGrenadesRemaining)
        {
            return;
        }

        if (_grenadeSlotSelected)
        {
            RefreshHeldToolVisibility();
            return;
        }

        bool wasBuilding = BuildModeActive;
        if (_antiMaterialBraceActive)
        {
            EndAntiMaterialBrace(applyCooldown: true);
        }

        if (_isReloading && _reloadWeapon != CardHotbarTool.HuntingRifle &&
            _reloadWeapon == CardHotbarTool.SniperRifle && !_sniperReloadLocked)
        {
            CancelReload();
        }

        _grenadeSlotSelected = true;
        _weaponFireCooldown = 0f;
        _c4RemoteDrawTimer = 0f;
        _c4RemoteReady = false;

        ExitSniperAds();
        ExitScopedArAds();
        if (wasBuilding)
        {
            ClearBuildInteractionState();
        }

        RefreshHeldToolVisibility();
        BeginWeaponDraw(CardHotbarTool.Grenade);
    }

    bool IsLaserSwordHotbarLocked()
    {
        return _laserSwordCooldownTimer > 0f;
    }

    int GetHotbarIndexForTool(CardHotbarTool tool)
    {
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (_activeKit.GetToolAt(i) == tool)
            {
                return i;
            }
        }

        return 0;
    }

    void UpdateContinuousGameplayState(bool allowAbilityInput)
    {
        UpdateReloadState();
        UpdateWeaponDrawTimer();
        UpdateWeaponFireCooldown();
        UpdateCyborgLaserHeat();
        UpdateLaserSwordTimers();
        UpdateAntiMaterialCharge();
        UpdateC4State();
        UpdateGrenadeFuseState();
        UpdateGrenadePostThrowSlotSwitch();
        UpdateGrenadeHandCooldown();
        UpdateMachineGunSuppression();
        UpdateAmmoRecharge();
        UpdateAbilityTimers(allowAbilityInput);
    }

    void UpdateWeaponFireCooldown()
    {
        if (_weaponFireCooldown > 0f)
        {
            _weaponFireCooldown = Mathf.Max(0f, _weaponFireCooldown - Time.deltaTime);
        }

        if (_weaponFireSlowTimer > 0f)
        {
            _weaponFireSlowTimer = Mathf.Max(0f, _weaponFireSlowTimer - Time.deltaTime);
        }
    }

    void UpdateGrenadeHandCooldown()
    {
        if (_grenadeHandCooldownTimer > 0f)
        {
            _grenadeHandCooldownTimer = Mathf.Max(0f, _grenadeHandCooldownTimer - Time.deltaTime);
        }
    }

    void UpdateGrenadePostThrowSlotSwitch()
    {
        if (_grenadePostThrowSlotSwitchTimer <= 0f)
        {
            return;
        }

        _grenadePostThrowSlotSwitchTimer = Mathf.Max(0f, _grenadePostThrowSlotSwitchTimer - Time.deltaTime);
        if (_grenadePostThrowSlotSwitchTimer > 0f || !_grenadeSlotSelected || HasAnyGrenadesRemaining)
        {
            return;
        }

        _grenadeSlotSelected = false;
        RefreshHeldToolVisibility();
        BeginWeaponDraw(SelectedTool);
    }

    bool IsC4ActionLocked()
    {
        return _c4ActionLockTimer > 0f;
    }

    void UpdateC4State()
    {
        if (_c4ActionLockTimer > 0f)
        {
            _c4ActionLockTimer = Mathf.Max(0f, _c4ActionLockTimer - Time.deltaTime);
            if (_c4ActionLockTimer > 0f)
            {
                return;
            }

            RefreshHeldToolVisibility();
        }

        if (_c4RemoteDrawTimer > 0f)
        {
            _c4RemoteDrawTimer = Mathf.Max(0f, _c4RemoteDrawTimer - Time.deltaTime);
            if (_c4RemoteDrawTimer <= 0f)
            {
                _c4RemoteReady = true;
            }

            return;
        }

        if (SelectedTool == CardHotbarTool.C4Charge &&
            _activeC4Charge != null &&
            _activeC4Charge.CanRemoteDetonate &&
            !_c4RemoteReady &&
            _c4RemoteDrawTimer <= 0f &&
            !IsWeaponDrawInProgress())
        {
            BeginC4RemoteDraw();
        }
    }

    void BeginC4RemoteDraw()
    {
        _c4RemoteDrawTimer = Mathf.Max(0f, c4RemoteDrawSeconds);
        _c4RemoteReady = _c4RemoteDrawTimer <= 0f;
        RefreshHeldToolVisibility();
    }

    void ResetC4State(bool destroyCharge)
    {
        if (destroyCharge && _activeC4Charge != null)
        {
            Destroy(_activeC4Charge.gameObject);
        }

        _activeC4Charge = null;
        _c4ActionLockTimer = 0f;
        _c4RemoteDrawTimer = 0f;
        _c4RemoteReady = false;
    }

    void UpdateAmmoRecharge()
    {
        TickAmmoRecharge(ref _pistolFamilyRechargeTimer, RefillPistolFamilyReserve);
        TickAmmoRecharge(ref _rifleFamilyRechargeTimer, RefillRifleFamilyReserve);
        TickAmmoRecharge(ref _machineGunRechargeTimer, RefillMachineGunReserve);
        TickAmmoRecharge(ref _sniperRechargeTimer, RefillSniperReserve);
        TickAmmoRecharge(ref _huntingRifleRechargeTimer, RefillHuntingRifleReserve);
        TickAmmoRecharge(ref _antiMaterialRechargeTimer, RefillAntiMaterialReserve);
        TickAmmoRecharge(ref _c4RechargeTimer, RefillC4Charge);
    }

    static void TickAmmoRecharge(ref float timer, System.Action refill)
    {
        if (timer <= 0f)
        {
            return;
        }

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        if (timer <= 0f)
        {
            refill();
        }
    }

    void RefillPistolFamilyReserve()
    {
        _pistolFamilyReserve = WeaponAmmoDefaults.PistolStartReserve;
        SyncPistolFamilyReserve();
    }

    void RefillRifleFamilyReserve()
    {
        _rifleFamilyReserve = WeaponAmmoDefaults.AssaultRifleStartReserve;
        SyncRifleFamilyReserve();
    }

    void RefillMachineGunReserve()
    {
        _machineGunAmmo.reserve = WeaponAmmoDefaults.MachineGunStartReserve;
    }

    void RefillSniperReserve()
    {
        _sniperAmmo.reserve = WeaponAmmoDefaults.SniperStartReserve;
    }

    void RefillHuntingRifleReserve()
    {
        _huntingRifleAmmo.reserve = WeaponAmmoDefaults.HuntingRifleStartReserve;
    }

    void RefillAntiMaterialReserve()
    {
        _antiMaterialAmmo.reserve = WeaponAmmoDefaults.AntiMaterialStartReserve;
    }

    void RefillC4Charge()
    {
        _c4Ammo.mag = WeaponAmmoDefaults.C4MagSize;
        RefreshHeldToolVisibility();
    }

    void MaybeStartAmmoRecharge(CardHotbarTool weapon)
    {
        if (IsPistolFamilyWeapon(weapon))
        {
            SyncPistolFamilyReserve();
            if (IsAmmoPoolDepleted(GetAmmoPoolRef(weapon)))
            {
                StartAmmoRechargeTimer(ref _pistolFamilyRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
            }

            return;
        }

        if (IsRifleFamilyWeapon(weapon))
        {
            SyncRifleFamilyReserve();
            if (IsAmmoPoolDepleted(GetAmmoPoolRef(weapon)))
            {
                StartAmmoRechargeTimer(ref _rifleFamilyRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
            }

            return;
        }

        switch (weapon)
        {
            case CardHotbarTool.SniperRifle:
                if (IsAmmoPoolDepleted(_sniperAmmo))
                {
                    StartAmmoRechargeTimer(ref _sniperRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
                }

                break;
            case CardHotbarTool.HuntingRifle:
                if (IsAmmoPoolDepleted(_huntingRifleAmmo))
                {
                    StartAmmoRechargeTimer(ref _huntingRifleRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
                }

                break;
            case CardHotbarTool.AntiMaterialRifle:
                if (IsAmmoPoolDepleted(_antiMaterialAmmo))
                {
                    StartAmmoRechargeTimer(ref _antiMaterialRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
                }

                break;
            case CardHotbarTool.MachineGun:
                if (IsAmmoPoolDepleted(_machineGunAmmo))
                {
                    StartAmmoRechargeTimer(ref _machineGunRechargeTimer, WeaponAmmoDefaults.AmmoRechargeSeconds);
                }

                break;
            case CardHotbarTool.C4Charge:
                if (IsAmmoPoolDepleted(_c4Ammo))
                {
                    StartAmmoRechargeTimer(ref _c4RechargeTimer, WeaponAmmoDefaults.C4RechargeSeconds);
                }

                break;
        }
    }

    static void StartAmmoRechargeTimer(ref float timer, float durationSeconds)
    {
        if (timer <= 0f)
        {
            timer = durationSeconds;
        }
    }

    static bool IsAmmoPoolDepleted(WeaponAmmoPool pool)
    {
        return pool.mag <= 0 && pool.reserve <= 0;
    }

    void HandleAbilityInput()
    {
        if (IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsReloadFullyLocked() ||
            _dashActive ||
            IsC4ActionLocked())
        {
            return;
        }

        string cardId = ActiveCardId();
        if (cardId == "infantry_2")
        {
            return;
        }

        if (cardId == "sniper_3")
        {
            HandleAntiMaterialBraceInput();
            return;
        }

        if (cardId == "demolition_1")
        {
            HandleExplosiveVestInput();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        switch (cardId)
        {
            case "sniper_1":
                TrySniperScopeAbility();
                break;
            case "sniper_2":
                TryHunterMark();
                break;
            case "infantry_1":
                TryInfantrySpeedBoost();
                break;
            case "infantry_3":
                TrySkirmisherDash();
                break;
            case "heavy_1":
                TryHeavyShield();
                break;
            case "heavy_2":
                TryCyborgRegenBoost();
                break;
            case "gunner_1":
                TryGunnerSuppressionBoost();
                break;
        }
    }

    void UpdateAbilityTimers(bool allowAbilityInput)
    {
        if (_dashActive)
        {
            _dashTimer -= Time.deltaTime;
            SetDashBlur(Mathf.Clamp01(_dashTimer / Mathf.Max(0.01f, skirmisherDashDurationSeconds)));
            if (_dashTimer <= 0f)
            {
                EndSkirmisherDash();
            }
        }

        if (_shieldAbilityActive)
        {
            var health = EnsurePlayerHealth();
            health?.TickShield(heavyShieldDecayPerSecond);
            if (health == null || !health.HasShield)
            {
                EndHeavyShield();
            }
        }

        UpdateHoldBreathState(allowAbilityInput);
        UpdateAntiMaterialBraceState(allowAbilityInput);
        UpdateHunterMarkState();
        UpdateCyborgRegenBoostState();
        UpdateGunnerSuppressionBoostState();
        UpdateExplosiveVestAttachState(allowAbilityInput);

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

    void UpdateHoldBreathState(bool allowAbilityInput)
    {
        if (ActiveCardId() != "infantry_2")
        {
            return;
        }

        if (!_holdBreathActive)
        {
            if (!allowAbilityInput ||
                _abilityCooldownRemaining > 0f ||
                IsRadialSelectorOpen ||
                IsBlindnessBlockingInput() ||
                IsReloadFullyLocked() ||
                _dashActive ||
                IsC4ActionLocked())
            {
                return;
            }

            if (Input.GetKey(KeyCode.E))
            {
                _holdBreathActive = true;
                _holdBreathRemaining = rangerHoldBreathMaxSeconds;
            }

            return;
        }

        if (allowAbilityInput && !Input.GetKey(KeyCode.E))
        {
            EndHoldBreath();
            return;
        }

        _holdBreathRemaining -= Time.deltaTime;
        if (_holdBreathRemaining <= 0f)
        {
            EndHoldBreath();
        }
    }

    void EndHoldBreath()
    {
        if (!_holdBreathActive)
        {
            return;
        }

        _holdBreathActive = false;
        _holdBreathRemaining = 0f;
        _abilityCooldownRemaining = rangerHoldBreathCooldownSeconds;
    }

    void HandleExplosiveVestInput()
    {
        if (_abilityCooldownRemaining > 0f)
        {
            return;
        }

        if (Input.GetKey(KeyCode.E))
        {
            if (!_explosiveVestAttaching)
            {
                BeginExplosiveVestAttach();
            }

            return;
        }

        if (_explosiveVestAttaching)
        {
            CancelExplosiveVestAttach();
        }
    }

    void BeginExplosiveVestAttach()
    {
        GameObject target = ResolveExplosiveVestTarget();
        if (target == null || ExplosiveVestState.TryGetEquipped(target, out _))
        {
            return;
        }

        if (target != gameObject &&
            HorizontalDistanceTo(target.transform.position) > explosiveVestMaxAttachDistanceMeters)
        {
            return;
        }

        _explosiveVestAttachTarget = target;
        _explosiveVestAttaching = true;
        _explosiveVestAttachTimer = explosiveVestAttachSeconds;
    }

    void CancelExplosiveVestAttach()
    {
        _explosiveVestAttaching = false;
        _explosiveVestAttachTimer = 0f;
        _explosiveVestAttachTarget = null;
    }

    void CompleteExplosiveVestAttach()
    {
        GameObject target = _explosiveVestAttachTarget;
        CancelExplosiveVestAttach();
        if (target == null || ExplosiveVestState.TryGetEquipped(target, out _))
        {
            return;
        }

        ExplosiveVestState.Ensure(target)?.Equip();
        _abilityCooldownRemaining = explosiveVestCooldownSeconds;
    }

    void UpdateExplosiveVestAttachState(bool allowAbilityInput)
    {
        if (!_explosiveVestAttaching)
        {
            return;
        }

        if (!allowAbilityInput || !Input.GetKey(KeyCode.E))
        {
            CancelExplosiveVestAttach();
            return;
        }

        if (_explosiveVestAttachTarget == null ||
            ExplosiveVestState.TryGetEquipped(_explosiveVestAttachTarget, out _))
        {
            CancelExplosiveVestAttach();
            return;
        }

        if (HorizontalDistanceTo(_explosiveVestAttachTarget.transform.position) >
            explosiveVestMaxAttachDistanceMeters)
        {
            CancelExplosiveVestAttach();
            return;
        }

        _explosiveVestAttachTimer -= Time.deltaTime;
        if (_explosiveVestAttachTimer <= 0f)
        {
            CompleteExplosiveVestAttach();
        }
    }

    GameObject ResolveExplosiveVestTarget()
    {
        Vector3 origin = transform.position;
        float searchRadius = Mathf.Max(0.1f, explosiveVestTargetSearchRadiusMeters);

        ThirdPersonController closestTeammate = null;
        float closestTeammateDistance = float.MaxValue;
        ThirdPersonController closestEnemy = null;
        float closestEnemyDistance = float.MaxValue;
        ShootingRangeDummy closestDummy = null;
        float closestDummyDistance = float.MaxValue;

        var controllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            ThirdPersonController controller = controllers[i];
            if (controller == null || controller == this)
            {
                continue;
            }

            float distance = HorizontalDistanceTo(controller.transform.position, origin);
            if (distance > searchRadius)
            {
                continue;
            }

            if (IsSameTeam(controller))
            {
                if (distance < closestTeammateDistance)
                {
                    closestTeammateDistance = distance;
                    closestTeammate = controller;
                }
            }
            else if (distance < closestEnemyDistance)
            {
                closestEnemyDistance = distance;
                closestEnemy = controller;
            }
        }

        IReadOnlyList<ShootingRangeDummy> dummies = ShootingRangeSession.Dummies;
        for (int i = 0; i < dummies.Count; i++)
        {
            ShootingRangeDummy dummy = dummies[i];
            if (dummy == null || dummy.IsDown)
            {
                continue;
            }

            float distance = HorizontalDistanceTo(dummy.transform.position, origin);
            if (distance <= searchRadius && distance < closestDummyDistance)
            {
                closestDummyDistance = distance;
                closestDummy = dummy;
            }
        }

        if (closestTeammate != null)
        {
            return closestTeammate.gameObject;
        }

        if (closestDummy != null)
        {
            return closestDummy.gameObject;
        }

        if (closestEnemy != null)
        {
            return closestEnemy.gameObject;
        }

        return gameObject;
    }

    bool IsSameTeam(ThirdPersonController other)
    {
        return other != null && other != this && other._playerTeam == _playerTeam;
    }

    float HorizontalDistanceTo(Vector3 worldPosition, Vector3? origin = null)
    {
        Vector3 from = origin ?? transform.position;
        Vector3 flatFrom = new Vector3(from.x, 0f, from.z);
        Vector3 flatTo = new Vector3(worldPosition.x, 0f, worldPosition.z);
        return Vector3.Distance(flatFrom, flatTo);
    }

    void HandleAntiMaterialBraceInput()
    {
        if (!Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (_antiMaterialBraceActive)
        {
            EndAntiMaterialBrace(applyCooldown: true);
            return;
        }

        if (_abilityCooldownRemaining > 0f ||
            SelectedTool != CardHotbarTool.AntiMaterialRifle ||
            IsWeaponDrawInProgress() ||
            IsBlindnessBlockingInput())
        {
            return;
        }

        BeginAntiMaterialBrace();
    }

    void BeginAntiMaterialBrace()
    {
        Vector3 aimForward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
        if (!IsAntiMaterialBraceAimMostlyHorizontal(aimForward))
        {
            return;
        }

        Vector3 flatForward = aimForward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        flatForward.Normalize();
        _antiMaterialBraceGroundNormal = Vector3.up;
        _antiMaterialBraceTransformHeightAboveAnchor = transform.position.y;
        _antiMaterialBraceAnchor = transform.position + (flatForward * antiMaterialBraceAnchorDistanceMeters);
        _antiMaterialBraceAnchor.y = _antiMaterialBraceTransformHeightAboveAnchor;

        Vector3 flatToPlayer = transform.position - _antiMaterialBraceAnchor;
        flatToPlayer.y = 0f;
        _antiMaterialBraceOrbitRadius = Mathf.Max(
            antiMaterialBraceAnchorDistanceMeters * 0.85f,
            flatToPlayer.magnitude);
        _antiMaterialBraceOrbitAngle = flatToPlayer.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(flatToPlayer.x, flatToPlayer.z)
            : Mathf.PI;
        _antiMaterialBraceGunTilt = Vector2.zero;
        _antiMaterialBraceActive = true;
        CancelAntiMaterialCharge();
        StopHorizontalMovement();
        AimAtAntiMaterialBraceAnchor();
    }

    bool IsAntiMaterialBraceAimMostlyHorizontal(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        direction.Normalize();
        return Mathf.Abs(direction.y) <= antiMaterialBraceMaxVerticalAim;
    }

    void UpdateAntiMaterialBraceState(bool allowAbilityInput)
    {
        if (!_antiMaterialBraceActive)
        {
            return;
        }

        if (!allowAbilityInput ||
            IsBlindnessBlockingInput() ||
            SelectedTool != CardHotbarTool.AntiMaterialRifle)
        {
            EndAntiMaterialBrace(applyCooldown: true);
        }
    }

    void EndAntiMaterialBrace(bool applyCooldown)
    {
        if (!_antiMaterialBraceActive)
        {
            return;
        }

        _antiMaterialBraceActive = false;
        _antiMaterialBraceGunTilt = Vector2.zero;
        _antiMaterialBraceGroundNormal = Vector3.up;
        if (applyCooldown)
        {
            _abilityCooldownRemaining = antiMaterialBraceCooldownSeconds;
        }
    }

    void TryHunterMark()
    {
        if (_abilityCooldownRemaining > 0f || _hunterMarkRemaining > 0f)
        {
            return;
        }

        _hunterMarkRemaining = hunterMarkDurationSeconds;
        _abilityCooldownRemaining = hunterMarkCooldownSeconds;
        HunterMarkSystem.ApplyMark(this, hunterMarkDurationSeconds);
    }

    void UpdateHunterMarkState()
    {
        if (_hunterMarkRemaining <= 0f)
        {
            return;
        }

        _hunterMarkRemaining = Mathf.Max(0f, _hunterMarkRemaining - Time.deltaTime);
        if (_hunterMarkRemaining <= 0f)
        {
            HunterMarkSystem.ClearAllMarks();
        }
    }

    void TrySniperScopeAbility()
    {
        if (_sniperScopeSwapPhase != 0 || SelectedTool != CardHotbarTool.SniperRifle)
        {
            return;
        }

        int nextScopeIndex = _sniperScopeIndex == 1 ? 2 : 1;
        if (SelectedTool == CardHotbarTool.SniperRifle && _sniperAimingHeld)
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

    void TrySkirmisherDash()
    {
        if (_dashActive || _abilityCooldownRemaining > 0f)
        {
            return;
        }

        Vector3 forward = cameraYawPivot != null ? cameraYawPivot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        _dashDirection = forward.normalized;
        _dashActive = true;
        _dashTimer = skirmisherDashDurationSeconds;
        _abilityCooldownRemaining = skirmisherDashCooldownSeconds;
        SetDashBlur(1f);
    }

    void EndSkirmisherDash()
    {
        _dashActive = false;
        _dashTimer = 0f;
        SetDashBlur(0f);
        StopHorizontalMovement();
    }

    void TryCyborgRegenBoost()
    {
        if (_cyborgRegenBoostRemaining > 0f || _abilityCooldownRemaining > 0f)
        {
            return;
        }

        var health = EnsurePlayerHealth();
        if (health == null)
        {
            return;
        }

        _cyborgRegenBoostRemaining = cyborgRegenBoostDurationSeconds;
        _abilityCooldownRemaining = cyborgRegenBoostCooldownSeconds;
        health.ActivateMaxHealthBoost(cyborgMaxHealthBoostFraction);
        health.SetAbilityRegeneration(cyborgRegenBoostFractionPerSecond);
    }

    void EndCyborgRegenBoost()
    {
        var health = EnsurePlayerHealth();
        health?.SetAbilityRegeneration(0f);
        health?.ClearMaxHealthBoost();
        _cyborgRegenBoostRemaining = 0f;
    }

    void UpdateCyborgRegenBoostState()
    {
        if (_cyborgRegenBoostRemaining <= 0f)
        {
            return;
        }

        _cyborgRegenBoostRemaining = Mathf.Max(0f, _cyborgRegenBoostRemaining - Time.deltaTime);
        if (_cyborgRegenBoostRemaining <= 0f)
        {
            EndCyborgRegenBoost();
        }
    }

    void TryGunnerSuppressionBoost()
    {
        if (_gunnerSuppressionBoostRemaining > 0f || _abilityCooldownRemaining > 0f)
        {
            return;
        }

        _gunnerSuppressionBoostRemaining = gunnerSuppressionBoostDurationSeconds;
        _abilityCooldownRemaining = gunnerSuppressionBoostCooldownSeconds;
    }

    void UpdateGunnerSuppressionBoostState()
    {
        if (_gunnerSuppressionBoostRemaining <= 0f)
        {
            return;
        }

        EndGunnerSuppressionBoostIfMagEmpty();
        if (_gunnerSuppressionBoostRemaining <= 0f)
        {
            return;
        }

        _gunnerSuppressionBoostRemaining = Mathf.Max(0f, _gunnerSuppressionBoostRemaining - Time.deltaTime);
    }

    void EndGunnerSuppressionBoostIfMagEmpty()
    {
        if (!GunnerSuppressionBoostActive || _machineGunAmmo.mag > 0)
        {
            return;
        }

        _gunnerSuppressionBoostRemaining = 0f;
    }

    void TryHeavyShield()
    {
        if (_shieldAbilityActive || _abilityCooldownRemaining > 0f)
        {
            return;
        }

        var health = EnsurePlayerHealth();
        if (health == null)
        {
            return;
        }

        _shieldAbilityActive = true;
        health.ActivateShield(heavyShieldHealth);
    }

    void EndHeavyShield()
    {
        if (!_shieldAbilityActive)
        {
            return;
        }

        _shieldAbilityActive = false;
        EnsurePlayerHealth()?.ClearShield();
        _abilityCooldownRemaining = heavyShieldCooldownSeconds;
    }

    void SetDashBlur(float blend)
    {
        if (!_localAuthority)
        {
            return;
        }

        if (SniperScopePostEffect.Instance != null)
        {
            SniperScopePostEffect.Instance.SetFullScreenBlur(blend);
        }
    }

    void RefreshCardMoveSpeed()
    {
        var card = CardCatalog.Get(GameSession.ActiveCardId);
        _baseCardMoveSpeed = card?.preview != null ? card.preview.moveSpeed : moveSpeed;
        ApplyCurrentMoveSpeed();
    }

    void ApplyCurrentMoveSpeed()
    {
        bool boostActive = IsInfantrySpeedBoostActive();
        moveSpeed = boostActive
            ? _baseCardMoveSpeed * infantrySpeedBoostMultiplier
            : _baseCardMoveSpeed;
    }

    void ClearPerCharacterGameplayEffects()
    {
        if (!_localAuthority)
        {
            return;
        }

        PlayerBulletHitFlash.Instance?.Clear();
        SetDashBlur(0f);
    }

    void ResetAbilityState()
    {
        _abilityCooldownRemaining = 0f;
        _speedBoostRemaining = 0f;
        _shieldAbilityActive = false;
        _dashActive = false;
        _dashTimer = 0f;
        SetDashBlur(0f);
        _holdBreathActive = false;
        _holdBreathRemaining = 0f;
        _antiMaterialBraceActive = false;
        _antiMaterialBraceGunTilt = Vector2.zero;
        _antiMaterialBraceGroundNormal = Vector3.up;
        _hunterMarkRemaining = 0f;
        _cyborgRegenBoostRemaining = 0f;
        _machineGunSuppressionRemaining = 0f;
        _machineGunSuppressionSpeedMultiplier = 1f;
        _gunnerSuppressionBoostRemaining = 0f;
        CancelExplosiveVestAttach();
        ExplosiveVestState.Ensure(gameObject)?.Clear();
        if (_localAuthority)
        {
            HunterMarkSystem.ClearAllMarks();
        }

        ExitScopedArAds();
        EnsurePlayerHealth()?.ClearShield();
        EndCyborgRegenBoost();
        ApplyCurrentMoveSpeed();
    }

    bool IsAbilityReady()
    {
        switch (ActiveCardId())
        {
            case "infantry_1":
                return _abilityCooldownRemaining <= 0f && _speedBoostRemaining <= 0f;
            case "infantry_2":
                return !_holdBreathActive && _abilityCooldownRemaining <= 0f;
            case "infantry_3":
                return _abilityCooldownRemaining <= 0f && !_dashActive;
            case "heavy_1":
                return _abilityCooldownRemaining <= 0f && !_shieldAbilityActive;
            case "heavy_2":
                return _abilityCooldownRemaining <= 0f && _cyborgRegenBoostRemaining <= 0f;
            case "sniper_2":
                return _abilityCooldownRemaining <= 0f && _hunterMarkRemaining <= 0f;
            case "sniper_1":
                return _sniperScopeSwapPhase == 0 && _abilityCooldownRemaining <= 0f;
            case "sniper_3":
                return !_antiMaterialBraceActive &&
                    _abilityCooldownRemaining <= 0f &&
                    SelectedTool == CardHotbarTool.AntiMaterialRifle;
            case "demolition_1":
                return !_explosiveVestAttaching && _abilityCooldownRemaining <= 0f;
            case "gunner_1":
                return _abilityCooldownRemaining <= 0f && _gunnerSuppressionBoostRemaining <= 0f;
            default:
                return false;
        }
    }

    float AbilityCooldownOverlayFill()
    {
        switch (ActiveCardId())
        {
            case "infantry_1":
                if (_speedBoostRemaining > 0f)
                {
                    return Mathf.Clamp01(_speedBoostRemaining / infantrySpeedBoostDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / infantrySpeedBoostCooldownSeconds);
                }

                return 0f;
            case "infantry_2":
                if (_holdBreathActive)
                {
                    return Mathf.Clamp01(_holdBreathRemaining / Mathf.Max(0.01f, rangerHoldBreathMaxSeconds));
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / rangerHoldBreathCooldownSeconds);
                }

                return 0f;
            case "infantry_3":
                if (_dashActive)
                {
                    return Mathf.Clamp01(_dashTimer / skirmisherDashDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / skirmisherDashCooldownSeconds);
                }

                return 0f;
            case "heavy_1":
                if (_shieldAbilityActive)
                {
                    var health = EnsurePlayerHealth();
                    if (health != null && heavyShieldHealth > 0f)
                    {
                        return 1f - Mathf.Clamp01(health.ShieldHealth / heavyShieldHealth);
                    }
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / heavyShieldCooldownSeconds);
                }

                return 0f;
            case "heavy_2":
                if (_cyborgRegenBoostRemaining > 0f)
                {
                    return 1f - Mathf.Clamp01(_cyborgRegenBoostRemaining / cyborgRegenBoostDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / cyborgRegenBoostCooldownSeconds);
                }

                return 0f;
            case "sniper_2":
                if (_hunterMarkRemaining > 0f)
                {
                    return 1f - Mathf.Clamp01(_hunterMarkRemaining / hunterMarkDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / hunterMarkCooldownSeconds);
                }

                return 0f;
            case "sniper_1":
                return _sniperScopeSwapPhase != 0 ? 1f : 0f;
            case "sniper_3":
                if (_antiMaterialBraceActive)
                {
                    return 0f;
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / antiMaterialBraceCooldownSeconds);
                }

                return 0f;
            case "demolition_1":
                if (_explosiveVestAttaching)
                {
                    return explosiveVestAttachSeconds <= 0f
                        ? 1f
                        : 1f - Mathf.Clamp01(_explosiveVestAttachTimer / explosiveVestAttachSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / explosiveVestCooldownSeconds);
                }

                return 0f;
            case "gunner_1":
                if (_gunnerSuppressionBoostRemaining > 0f)
                {
                    return 1f - Mathf.Clamp01(_gunnerSuppressionBoostRemaining / gunnerSuppressionBoostDurationSeconds);
                }

                if (_abilityCooldownRemaining > 0f)
                {
                    return Mathf.Clamp01(_abilityCooldownRemaining / gunnerSuppressionBoostCooldownSeconds);
                }

                return 0f;
            default:
                return 0f;
        }
    }

    static string ActiveCardId()
    {
        return GameSession.ActiveCardId ?? string.Empty;
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
        _blockWeaponFireUntilMouseRelease = _weaponMouseHeldDuringPrep || Input.GetMouseButton(0);
        _weaponMouseHeldDuringPrep = false;
        _postPrepWeaponLockTimer = 1f;
        _drawingWeapon = default;
        _weaponDrawTimer = 0f;
        _weaponDrawDuration = 0f;
    }

    void UpdateWeaponFireInputGate()
    {
        if (_postPrepWeaponLockTimer > 0f)
        {
            _postPrepWeaponLockTimer = Mathf.Max(0f, _postPrepWeaponLockTimer - Time.deltaTime);
        }

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
            IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsC4ActionLocked() ||
            _c4RemoteDrawTimer > 0f ||
            (_grenadePrimed && !_grenadeSlotSelected) ||
            _postPrepWeaponLockTimer > 0f ||
            _blockWeaponFireUntilMouseRelease ||
            IsWeaponDrawInProgress();
    }

    static bool IsBlindnessBlockingInput()
    {
        return PlayerBulletHitFlash.Instance != null && PlayerBulletHitFlash.Instance.BlocksGameplayInput;
    }

    float WeaponDrawDuration(CardHotbarTool weapon)
    {
        float baseDuration;
        switch (weapon)
        {
            case CardHotbarTool.Pistol:
                baseDuration = pistolDrawSeconds;
                break;
            case CardHotbarTool.Smg:
                baseDuration = smgDrawSeconds;
                break;
            case CardHotbarTool.MachinePistol:
                baseDuration = pistolDrawSeconds;
                break;
            case CardHotbarTool.AssaultRifle:
            case CardHotbarTool.ScopedAssaultRifle:
                baseDuration = assaultRifleDrawSeconds;
                break;
            case CardHotbarTool.LightMachineGun:
                baseDuration = lmgDrawSeconds;
                break;
            case CardHotbarTool.MachineGun:
                baseDuration = machineGunDrawSeconds;
                break;
            case CardHotbarTool.SniperRifle:
                baseDuration = sniperDrawSeconds;
                break;
            case CardHotbarTool.HuntingRifle:
                baseDuration = HuntingRifleDrawSeconds;
                break;
            case CardHotbarTool.AntiMaterialRifle:
                baseDuration = antiMaterialDrawSeconds;
                break;
            case CardHotbarTool.CyborgLaser:
                baseDuration = cyborgLaserDrawSeconds;
                break;
            case CardHotbarTool.C4Charge:
                baseDuration = c4DrawSeconds;
                break;
            case CardHotbarTool.Grenade:
                return grenadeDrawSeconds;
            default:
                return 0f;
        }

        return InfantrySpeedBoostDrawDuration(baseDuration);
    }

    void BeginWeaponDraw(CardHotbarTool weapon)
    {
        if (!IsDrawBlockingTool(weapon))
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
        return IsDrawBlockingTool(SelectedTool) &&
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
        if (IsRadialSelectorOpen)
        {
            return;
        }

        if (!IsMarksmanRifleTool(SelectedTool) && SelectedTool != CardHotbarTool.AntiMaterialRifle)
        {
            ExitSniperAds();
        }

        if (SelectedTool != CardHotbarTool.ScopedAssaultRifle)
        {
            ExitScopedArAds();
        }

        if (IsReloadFullyLocked())
        {
            if (_reloadWeapon == CardHotbarTool.SniperRifle &&
                IsMarksmanRifleTool(SelectedTool) &&
                !IsWeaponDrawInProgress())
            {
                UpdateSniperAdsState();
            }
            else if (_isReloading &&
                (_reloadWeapon == CardHotbarTool.HuntingRifle ||
                    _reloadWeapon == CardHotbarTool.AntiMaterialRifle))
            {
                ExitSniperAds();
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
            case CardHotbarTool.ScopedAssaultRifle:
                HandleScopedAssaultRifleInput();
                break;
            case CardHotbarTool.Smg:
                HandleSmgInput();
                break;
            case CardHotbarTool.MachinePistol:
                HandleMachinePistolInput();
                break;
            case CardHotbarTool.LightMachineGun:
                HandleLmgInput();
                break;
            case CardHotbarTool.MachineGun:
                HandleMachineGunInput();
                break;
            case CardHotbarTool.SniperRifle:
                HandleSniperRifleInput();
                break;
            case CardHotbarTool.HuntingRifle:
                HandleHuntingRifleInput();
                break;
            case CardHotbarTool.AntiMaterialRifle:
                HandleAntiMaterialRifleInput();
                break;
            case CardHotbarTool.C4Charge:
                HandleC4Input();
                break;
            case CardHotbarTool.Pistol:
                HandlePistolInput();
                break;
            case CardHotbarTool.CyborgLaser:
                HandleCyborgLaserInput();
                break;
            case CardHotbarTool.LaserSword:
                HandleLaserSwordInput();
                break;
            case CardHotbarTool.Hammer:
                HandleHammerInput();
                break;
            case CardHotbarTool.Blueprint:
                HandleBuildingInput();
                break;
            case CardHotbarTool.Grenade:
                HandleGrenadeToolInput();
                break;
        }
    }

    void HandleGrenadeToolInput()
    {
        if (!IsGameplayWeaponInputAllowed() ||
            IsRadialSelectorOpen ||
            IsBlindnessBlockingInput() ||
            IsC4ActionLocked() ||
            _dashActive ||
            IsWeaponDrawInProgress() ||
            IsGrenadeHandCooldownActive() ||
            GetGrenadeCount(_selectedGrenade) <= 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1) && !_grenadePrimed)
        {
            PrimeSelectedGrenade();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ThrowGrenade();
        }
    }

    void HandleC4Input()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (_activeC4Charge == null)
        {
            if (!_c4Ammo.CanFire)
            {
                return;
            }

            ThrowC4Charge();
            return;
        }

        if (_c4RemoteReady && _activeC4Charge.CanRemoteDetonate)
        {
            _activeC4Charge.QueueDetonation(c4RemoteDetonationDelaySeconds);
        }
    }

    void ThrowC4Charge()
    {
        if (viewCamera == null)
        {
            return;
        }

        Ray throwRay = BuildCenterAimRay();
        Vector3 spawnPosition = BulletSpawnPosition(throwRay);
        var charge = new GameObject("C4 Charge");
        charge.transform.position = spawnPosition;
        charge.transform.rotation = Quaternion.LookRotation(throwRay.direction, Vector3.up);
        _activeC4Charge = charge.AddComponent<C4ChargeProjectile>();
        _activeC4Charge.Destroyed += HandleActiveC4Destroyed;
        _activeC4Charge.Initialize(
            throwRay.direction * c4ThrowSpeed,
            c4FallAccelerationMetersPerSecond,
            gameObject,
            c4ThrowLockSeconds);

        _c4Ammo.ConsumeRound();
        MaybeStartAmmoRecharge(CardHotbarTool.C4Charge);
        _c4ActionLockTimer = c4ThrowLockSeconds;
        _c4RemoteReady = false;
        _c4RemoteDrawTimer = 0f;
        RefreshHeldToolVisibility();
    }

    void HandleActiveC4Destroyed(C4ChargeProjectile charge)
    {
        if (_activeC4Charge != charge)
        {
            return;
        }

        _activeC4Charge = null;
        _c4RemoteReady = false;
        _c4RemoteDrawTimer = 0f;
        RefreshHeldToolVisibility();
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

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(
                CardHotbarTool.AssaultRifle,
                assaultRifleBulletSpeed,
                AssaultRifleRecoilScale,
                ProjectileWeaponType.AssaultRifle))
        {
            return;
        }

        _weaponFireCooldown = AssaultRifleFireInterval;
    }

    void HandleScopedAssaultRifleInput()
    {
        UpdateScopedArAdsState();

        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(
                CardHotbarTool.ScopedAssaultRifle,
                assaultRifleBulletSpeed,
                ScopedArRecoilScale,
                ProjectileWeaponType.AssaultRifle))
        {
            return;
        }

        _weaponFireCooldown = AssaultRifleFireInterval;
    }

    void HandleSmgInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(
                CardHotbarTool.Smg,
                smgBulletSpeed,
                SmgRecoilScale,
                ProjectileWeaponType.Smg))
        {
            return;
        }

        _weaponFireCooldown = SmgFireInterval;
    }

    void HandleMachinePistolInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(
                CardHotbarTool.MachinePistol,
                smgBulletSpeed,
                MachinePistolRecoilScale,
                ProjectileWeaponType.MachinePistol))
        {
            return;
        }

        _weaponFireCooldown = SmgFireInterval;
    }

    void HandleLmgInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireWeapon(
                CardHotbarTool.LightMachineGun,
                lmgBulletSpeed,
                LmgRecoilScale,
                ProjectileWeaponType.LightMachineGun))
        {
            return;
        }

        _weaponFireCooldown = LmgFireInterval;
    }

    void HandleMachineGunInput()
    {
        if (IsWeaponFireInputBlocked())
        {
            return;
        }

        if (_isReloading)
        {
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        if (!TryFireMachineGun())
        {
            return;
        }

        _weaponFireCooldown = MachineGunFireInterval;
    }

    bool TryFireMachineGun()
    {
        ref WeaponAmmoPool pool = ref GetAmmoPoolRef(CardHotbarTool.MachineGun);
        if (!pool.CanFire)
        {
            return false;
        }

        FireWeapon(
            BuildMachineGunAimRay(),
            machineGunBulletSpeed,
            MachineGunRecoilScale,
            ProjectileWeaponType.MachineGun);
        pool.ConsumeRound();
        EndGunnerSuppressionBoostIfMagEmpty();
        MaybeStartAmmoRecharge(CardHotbarTool.MachineGun);
        MenuUiSounds.PlayWeaponGunshot(ProjectileWeaponType.MachineGun);
        _weaponFireSlowTimer = WeaponFireSlowWindowSeconds;
        return true;
    }

    Ray BuildMachineGunAimRay()
    {
        float spreadBias = GunnerSuppressionBoostActive
            ? gunnerSuppressionBoostSpreadCenterBiasExponent
            : machineGunSpreadCenterBiasExponent;
        return BuildCrosshairAimRay(SampleCenterBiasedCircleOffset(
            EffectiveMachineGunCrosshairRadiusPixels,
            spreadBias));
    }

    static Vector2 SampleCenterBiasedCircleOffset(float radiusPixels, float centerBiasExponent)
    {
        if (radiusPixels <= 0.01f)
        {
            return Vector2.zero;
        }

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        float normalizedRadius = Mathf.Pow(
            UnityEngine.Random.value,
            Mathf.Max(0.05f, centerBiasExponent));
        float distance = radiusPixels * normalizedRadius;
        return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
    }

    void HandleCyborgLaserInput()
    {
        _cyborgLaserFiringHeld = Input.GetMouseButton(0);

        if (IsWeaponFireInputBlocked() || _cyborgLaserOverheatLockoutTimer > 0f)
        {
            return;
        }

        if (!_cyborgLaserFiringHeld)
        {
            return;
        }

        if (_weaponFireCooldown > 0f)
        {
            return;
        }

        FireCyborgLaser();
        _weaponFireCooldown = CyborgLaserFireInterval;
        _weaponFireSlowTimer = WeaponFireSlowWindowSeconds;
    }

    void HandleLaserSwordInput()
    {
        if (IsWeaponFireInputBlocked() || _laserSwordCooldownTimer > 0f || _laserSwordSwingTimer > 0f)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        SwingLaserSword();
    }

    void FireCyborgLaser()
    {
        if (viewCamera == null)
        {
            return;
        }

        Ray shotRay = BuildCenterAimRay();
        Vector3 spawnPosition = BulletSpawnPosition(shotRay);
        var bullet = new GameObject("Projectile Laser");
        bullet.transform.position = spawnPosition;
        bullet.transform.rotation = Quaternion.LookRotation(shotRay.direction, Vector3.up);
        bullet.AddComponent<ProjectileBullet>().Initialize(
            shotRay.direction * cyborgLaserSpeed,
            ProjectileWeaponType.CyborgLaser,
            gameObject);

        _gunKickTimer = 0.08f;
        _muzzleFlashTimer = 0.045f;
        _gunRecoilPeak = new Vector2(
            UnityEngine.Random.Range(-gunRecoilHorizontalRandomness, gunRecoilHorizontalRandomness) * PistolRecoilScale,
            UnityEngine.Random.Range(gunRecoilVerticalRandomness * 0.55f, gunRecoilVerticalRandomness) * PistolRecoilScale);
        float verticalRetention = UnityEngine.Random.Range(0.35f, 0.58f);
        _gunRecoilResidual = new Vector2(_gunRecoilPeak.x, _gunRecoilPeak.y * verticalRetention);
        _gunRecoilKickTimer = gunRecoilKickDuration;
        _gunRecoilAimApplied = false;
        MenuUiSounds.PlayWeaponGunshot(ProjectileWeaponType.CyborgLaser);
    }

    void SwingLaserSword()
    {
        _laserSwordSwingTimer = laserSwordSwingSeconds;
        _laserSwordCooldownTimer = laserSwordCooldownSeconds;
        SelectHotbarIndex(GetHotbarIndexForTool(CardHotbarTool.LaserSword));
        ApplyLaserSwordDamage();
    }

    void ApplyLaserSwordDamage()
    {
        Vector3 forward = cameraYawPivot != null ? cameraYawPivot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        forward.Normalize();
        Vector3 origin = transform.position + (Vector3.up * Mathf.Max(0.5f, eyeHeight * 0.85f));
        float halfArc = laserSwordArcDegrees * 0.5f;
        float range = Mathf.Max(0.1f, laserSwordRangeMeters);
        var damagedRoots = new HashSet<GameObject>();

        Collider[] hits = Physics.OverlapSphere(origin, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            var dummy = hit.GetComponentInParent<ShootingRangeDummy>();
            if (dummy != null)
            {
                if (!damagedRoots.Add(dummy.gameObject))
                {
                    continue;
                }

                if (!IsWithinLaserSwordArc(origin, forward, dummy.transform.position, range, halfArc))
                {
                    continue;
                }

                dummy.ApplyDirectDamage(laserSwordDamage, false);
                continue;
            }

            var controller = hit.GetComponentInParent<ThirdPersonController>();
            if (controller == null || controller == this)
            {
                continue;
            }

            if (!damagedRoots.Add(controller.gameObject))
            {
                continue;
            }

            if (!IsWithinLaserSwordArc(origin, forward, controller.transform.position, range, halfArc))
            {
                continue;
            }

            controller.GetComponent<PlayerHealth>()?.ApplyDamage(laserSwordDamage, false);
        }

        C4ChargeProjectile.ApplyChargesInRange(
            origin,
            range,
            laserSwordDamage,
            targetPosition => IsWithinLaserSwordArc(origin, forward, targetPosition, range, halfArc));
    }

    static bool IsWithinLaserSwordArc(
        Vector3 origin,
        Vector3 forward,
        Vector3 targetPosition,
        float range,
        float halfArcDegrees)
    {
        Vector3 toTarget = targetPosition - origin;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance <= 0.0001f || distance > range)
        {
            return false;
        }

        return Vector3.Angle(forward, toTarget) <= halfArcDegrees;
    }

    void UpdateCyborgLaserHeat()
    {
        if (_cyborgLaserOverheatLockoutTimer > 0f)
        {
            _cyborgLaserOverheatLockoutTimer = Mathf.Max(0f, _cyborgLaserOverheatLockoutTimer - Time.deltaTime);
            _cyborgLaserHeat = 1f;
            return;
        }

        if (SelectedTool != CardHotbarTool.CyborgLaser)
        {
            _cyborgLaserFiringHeld = false;
        }

        if (_cyborgLaserFiringHeld &&
            SelectedTool == CardHotbarTool.CyborgLaser &&
            !IsWeaponFireInputBlocked())
        {
            float heatRate = cyborgLaserOverheatSeconds <= 0f
                ? 1f
                : 1f / cyborgLaserOverheatSeconds;
            _cyborgLaserHeat = Mathf.Min(1f, _cyborgLaserHeat + (heatRate * Time.deltaTime));
            if (_cyborgLaserHeat >= 0.999f)
            {
                _cyborgLaserHeat = 1f;
                _cyborgLaserOverheatLockoutTimer = cyborgLaserOverheatCooldownSeconds;
                _cyborgLaserFiringHeld = false;
            }

            return;
        }

        if (_cyborgLaserHeat > 0f)
        {
            float coolRate = cyborgLaserCoolSeconds <= 0f
                ? 1f
                : 1f / cyborgLaserCoolSeconds;
            _cyborgLaserHeat = Mathf.Max(0f, _cyborgLaserHeat - (coolRate * Time.deltaTime));
        }
    }

    void UpdateLaserSwordTimers()
    {
        if (_laserSwordCooldownTimer > 0f)
        {
            _laserSwordCooldownTimer = Mathf.Max(0f, _laserSwordCooldownTimer - Time.deltaTime);
        }
    }

    void ResetCyborgLaserHeat()
    {
        _cyborgLaserHeat = 0f;
        _cyborgLaserOverheatLockoutTimer = 0f;
        _cyborgLaserFiringHeld = false;
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

        TryFireWeapon(
            CardHotbarTool.Pistol,
            pistolBulletSpeed,
            PistolRecoilScale * HoldBreathRecoilMultiplier(false),
            ProjectileWeaponType.Pistol);
    }

    void HandleSniperRifleInput()
    {
        UpdateSniperAdsState();

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

        float recoilScale = _sniperAimingHeld ? 2.835f : 5.04f;
        if (!TryFireSniperWeapon(sniperBulletSpeed, recoilScale))
        {
            return;
        }

        _weaponFireCooldown = sniperFireCooldownSeconds;
    }

    void HandleHuntingRifleInput()
    {
        UpdateSniperAdsState();

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
            return;
        }

        float recoilScale = _sniperAimingHeld ? 2.4f : 4.2f;
        if (!TryFireHuntingRifleWeapon(sniperBulletSpeed, recoilScale))
        {
            return;
        }

        _weaponFireCooldown = sniperFireCooldownSeconds;
        _weaponFireSlowTimer = WeaponFireSlowWindowSeconds;
    }

    void HandleAntiMaterialRifleInput()
    {
        UpdateAntiMaterialAdsState();

        if (IsWeaponFireInputBlocked() || _isReloading)
        {
            CancelAntiMaterialCharge();
            return;
        }

        if (!IsAntiMaterialAdsReady())
        {
            CancelAntiMaterialCharge();
            return;
        }

        if (!Input.GetMouseButton(0))
        {
            CancelAntiMaterialCharge();
            return;
        }

        if (!_antiMaterialAmmo.CanFire)
        {
            CancelAntiMaterialCharge();
            return;
        }

        if (!_antiMaterialCharging)
        {
            _antiMaterialCharging = true;
            _antiMaterialChargeTimer = 0f;
            MenuUiSounds.StartAntiMaterialCharge();
        }
    }

    void UpdateAntiMaterialAdsState()
    {
        if (SelectedTool != CardHotbarTool.AntiMaterialRifle || viewCamera == null)
        {
            return;
        }

        if (_isReloading)
        {
            ExitSniperAds();
            return;
        }

        if (IsBlindnessBlockingInput())
        {
            ExitSniperAds();
            CancelAntiMaterialCharge();
            return;
        }

        bool wantAds = Input.GetMouseButton(1);
        bool wasAlreadyAiming = _sniperAimingHeld;
        if (wantAds)
        {
            if (!wasAlreadyAiming)
            {
                _sniperAimingHeld = true;
                _sniperAdsActive = true;
                BeginSniperFovTransition(AntiMaterialAdsFov);
            }
        }
        else if (_sniperAimingHeld)
        {
            _sniperAimingHeld = false;
            _sniperAdsActive = false;
            BeginSniperFovTransition(fieldOfView);
            CancelAntiMaterialCharge();
        }

        TickSniperFovTransition();
        UpdateAntiMaterialScopeOverlay();
        RefreshHeldToolVisibility();
    }

    void UpdateAntiMaterialScopeOverlay()
    {
        if (SelectedTool != CardHotbarTool.AntiMaterialRifle)
        {
            return;
        }

        float fadeDuration = _sniperAimingHeld
            ? AntiMaterialEffectiveAdsTransitionSeconds
            : adsExitTransitionSeconds;
        _sniperScopeOverlayBlend = Mathf.MoveTowards(
            _sniperScopeOverlayBlend,
            _sniperAimingHeld ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, fadeDuration));
        PushSniperScopePostEffect();
    }

    bool IsAntiMaterialAdsReady()
    {
        return SelectedTool == CardHotbarTool.AntiMaterialRifle &&
            _sniperAimingHeld &&
            _sniperFovTransitionDuration > 0f &&
            _sniperFovTransitionElapsed >= _sniperFovTransitionDuration &&
            _sniperScopeOverlayBlend >= 0.98f;
    }

    void UpdateAntiMaterialCharge()
    {
        if (!_antiMaterialCharging)
        {
            return;
        }

        if (SelectedTool != CardHotbarTool.AntiMaterialRifle ||
            IsWeaponFireInputBlocked() ||
            _isReloading ||
            !_antiMaterialAmmo.CanFire ||
            !IsAntiMaterialAdsReady() ||
            !Input.GetMouseButton(0))
        {
            CancelAntiMaterialCharge();
            return;
        }

        _antiMaterialChargeTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_antiMaterialChargeTimer / Mathf.Max(0.01f, antiMaterialChargeSeconds));
        MenuUiSounds.UpdateAntiMaterialCharge(progress);
        if (_antiMaterialChargeTimer < antiMaterialChargeSeconds)
        {
            return;
        }

        _antiMaterialCharging = false;
        _antiMaterialChargeTimer = 0f;
        MenuUiSounds.StopAntiMaterialCharge();
        FireAntiMaterialRound();
    }

    void CancelAntiMaterialCharge()
    {
        if (!_antiMaterialCharging && _antiMaterialChargeTimer <= 0f)
        {
            return;
        }

        _antiMaterialCharging = false;
        _antiMaterialChargeTimer = 0f;
        MenuUiSounds.StopAntiMaterialCharge();
    }

    void FireAntiMaterialRound()
    {
        if (!_antiMaterialAmmo.CanFire || viewCamera == null)
        {
            return;
        }

        Ray shotRay = BuildSniperAimRay();
        Vector3 spawnPosition = BulletSpawnPosition(shotRay);
        var projectile = new GameObject("Anti-Material Projectile");
        projectile.transform.position = spawnPosition;
        projectile.transform.rotation = Quaternion.LookRotation(shotRay.direction, Vector3.up);
        projectile.AddComponent<AntiMaterialProjectile>().Initialize(
            shotRay.direction * antiMaterialBulletSpeed,
            antiMaterialBulletSpeed,
            gameObject);

        _antiMaterialAmmo.ConsumeRound();
        MaybeStartAmmoRecharge(CardHotbarTool.AntiMaterialRifle);
        _gunKickTimer = 0.12f;
        _muzzleFlashTimer = 0.06f;
        _gunRecoilPeak = new Vector2(
            UnityEngine.Random.Range(-gunRecoilHorizontalRandomness, gunRecoilHorizontalRandomness) *
                AntiMaterialEffectiveRecoilScale,
            UnityEngine.Random.Range(gunRecoilVerticalRandomness * 0.55f, gunRecoilVerticalRandomness) *
                AntiMaterialEffectiveRecoilScale);
        float verticalRetention = UnityEngine.Random.Range(0.35f, 0.58f);
        _gunRecoilResidual = new Vector2(_gunRecoilPeak.x, _gunRecoilPeak.y * verticalRetention);
        _gunRecoilKickTimer = gunRecoilKickDuration;
        _gunRecoilAimApplied = false;
        _weaponFireSlowTimer = WeaponFireSlowWindowSeconds;
        MenuUiSounds.PlayWeaponGunshot(ProjectileWeaponType.AntiMaterialRifle);
    }

    bool TryFireHuntingRifleWeapon(float muzzleSpeed, float recoilScale)
    {
        if (!_huntingRifleAmmo.CanFire)
        {
            return false;
        }

        FireWeapon(BuildSniperAimRay(), muzzleSpeed, recoilScale, ProjectileWeaponType.HuntingRifle);
        _huntingRifleAmmo.ConsumeRound();
        MaybeStartAmmoRecharge(CardHotbarTool.HuntingRifle);
        MenuUiSounds.PlayWeaponGunshot(ProjectileWeaponType.HuntingRifle);
        return true;
    }

    bool TryFireSniperWeapon(float muzzleSpeed, float recoilScale)
    {
        if (!_sniperAmmo.CanFire)
        {
            return false;
        }

        FireWeapon(BuildSniperAimRay(), muzzleSpeed, recoilScale, ProjectileWeaponType.SniperRifle);
        _sniperAmmo.ConsumeRound();
        MaybeStartAmmoRecharge(CardHotbarTool.SniperRifle);
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
        MaybeStartAmmoRecharge(weapon);
        MenuUiSounds.PlayWeaponGunshot(weaponType);
        _weaponFireSlowTimer = WeaponFireSlowWindowSeconds;
        return true;
    }

    Ray BuildSniperAimRay()
    {
        float spreadHalf = SniperCurrentSpreadHalfPixels();
        if (spreadHalf <= 0.01f)
        {
            return BuildCrosshairAimRay();
        }

        return BuildCrosshairAimRay(SampleSniperSpreadOffset(spreadHalf));
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
        float hipSpread = MarksmanHipFireCrosshairGap();
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
        if (!IsMarksmanRifleTool(SelectedTool) || viewCamera == null)
        {
            ExitSniperAds();
            return;
        }

        if (_isReloading && SelectedTool == CardHotbarTool.HuntingRifle)
        {
            ExitSniperAds();
            return;
        }

        if (IsBlindnessBlockingInput())
        {
            ExitSniperAds();
            CancelAntiMaterialCharge();
            return;
        }

        bool wantAds = Input.GetMouseButton(1);
        bool wasAlreadyAiming = _sniperAimingHeld;
        if (_sniperScopeSwapPhase != 0 && SelectedTool == CardHotbarTool.SniperRifle)
        {
            TickSniperScopeSwap(wantAds);
        }
        else if (wantAds)
        {
            if (!wasAlreadyAiming)
            {
                _sniperAimingHeld = true;
                _sniperAdsActive = true;
                BeginSniperFovTransition(MarksmanAdsFov());
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
        targetScopeIndex = Mathf.Clamp(targetScopeIndex, 1, 2);
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
            _sniperFovTransitionDuration = SelectedTool switch
            {
                CardHotbarTool.HuntingRifle => ads4xTransitionSeconds,
                CardHotbarTool.AntiMaterialRifle => AntiMaterialEffectiveAdsTransitionSeconds,
                _ => SniperAdsTransitionDuration(_sniperScopeIndex)
            };
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
            ? (SelectedTool == CardHotbarTool.HuntingRifle
                ? ads4xTransitionSeconds
                : SniperAdsTransitionDuration(_sniperScopeIndex))
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
            ActiveMarksmanAdsScopeIndex());
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
        CancelAntiMaterialCharge();
        _sniperFovTransitionElapsed = 0f;
        _sniperFovTransitionDuration = 0f;
        _sniperFovTransitionTarget = fieldOfView;
        _sniperFovTransitionStart = fieldOfView;
        _sniperDisplayedFov = fieldOfView;
        if (viewCamera != null && !_scopedArAdsHeld)
        {
            viewCamera.fieldOfView = fieldOfView;
        }

        if (SniperScopePostEffect.Instance != null && !_scopedArAdsHeld)
        {
            SniperScopePostEffect.Instance.SetActive(false, 0f, 0);
            SniperScopePostEffect.Instance.SetFullScreenBlur(0f);
        }
    }

    void ExitScopedArAds()
    {
        _scopedArAdsHeld = false;
        _scopedArScopeOverlayBlend = 0f;
        _scopedArFovTransitionElapsed = 0f;
        _scopedArFovTransitionDuration = 0f;
        _scopedArFovTransitionTarget = fieldOfView;
        _scopedArFovTransitionStart = fieldOfView;
        _scopedArDisplayedFov = fieldOfView;
        if (viewCamera != null && !_sniperAimingHeld)
        {
            viewCamera.fieldOfView = fieldOfView;
        }

        if (SniperScopePostEffect.Instance != null && !_sniperAimingHeld)
        {
            SniperScopePostEffect.Instance.SetActive(false, 0f, 0);
            SniperScopePostEffect.Instance.SetFullScreenBlur(0f);
        }

        RefreshHeldToolVisibility();
    }

    void UpdateScopedArAdsState()
    {
        if (SelectedTool != CardHotbarTool.ScopedAssaultRifle || viewCamera == null)
        {
            ExitScopedArAds();
            return;
        }

        if (IsBlindnessBlockingInput())
        {
            ExitScopedArAds();
            return;
        }

        bool wantAds = Input.GetMouseButton(1);
        if (wantAds)
        {
            if (!_scopedArAdsHeld)
            {
                _scopedArAdsHeld = true;
                BeginScopedArFovTransition(ScopedArAdsFov);
            }
        }
        else if (_scopedArAdsHeld)
        {
            _scopedArAdsHeld = false;
            BeginScopedArFovTransition(fieldOfView);
        }

        TickScopedArFovTransition();
        UpdateScopedArScopeOverlay();
        RefreshHeldToolVisibility();
    }

    void BeginScopedArFovTransition(float targetFov)
    {
        _scopedArFovTransitionStart = _scopedArDisplayedFov > 0f ? _scopedArDisplayedFov : fieldOfView;
        _scopedArFovTransitionTarget = targetFov;
        _scopedArFovTransitionElapsed = 0f;
        _scopedArFovTransitionDuration = _scopedArAdsHeld && targetFov < fieldOfView - 0.01f
            ? scopedArAdsTransitionSeconds
            : adsExitTransitionSeconds;
    }

    void TickScopedArFovTransition()
    {
        if (viewCamera == null)
        {
            return;
        }

        if (_scopedArFovTransitionDuration <= 0f)
        {
            _scopedArDisplayedFov = _scopedArFovTransitionTarget;
            viewCamera.fieldOfView = _scopedArDisplayedFov;
            return;
        }

        _scopedArFovTransitionElapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(_scopedArFovTransitionElapsed / _scopedArFovTransitionDuration);
        normalized = normalized * normalized * (3f - (2f * normalized));
        _scopedArDisplayedFov = Mathf.Lerp(_scopedArFovTransitionStart, _scopedArFovTransitionTarget, normalized);
        viewCamera.fieldOfView = _scopedArDisplayedFov;
    }

    void UpdateScopedArScopeOverlay()
    {
        if (SniperScopePostEffect.Instance == null)
        {
            return;
        }

        float fadeDuration = _scopedArAdsHeld ? scopedArAdsTransitionSeconds : adsExitTransitionSeconds;
        _scopedArScopeOverlayBlend = Mathf.MoveTowards(
            _scopedArScopeOverlayBlend,
            _scopedArAdsHeld ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, fadeDuration));

        bool active = _scopedArScopeOverlayBlend > 0.001f && _scopedArAdsHeld;
        SniperScopePostEffect.Instance.SetFullScreenBlur(0f);
        SniperScopePostEffect.Instance.SetActive(active, _scopedArScopeOverlayBlend, 1);
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

        recoilScale = InfantrySpeedBoostRecoilScale(recoilScale);

        Vector3 spawnPosition = BulletSpawnPosition(shotRay);
        if (TryGetComponent<NetworkPlayerAvatar>(out var avatar) && avatar.IsSpawned)
        {
            avatar.RequestProjectileFire(spawnPosition, shotRay.direction, muzzleSpeed, weaponType);
        }
        else
        {
            var bullet = new GameObject("Projectile Bullet");
            bullet.transform.position = spawnPosition;
            bullet.transform.rotation = Quaternion.LookRotation(shotRay.direction, Vector3.up);
            bullet.AddComponent<ProjectileBullet>().Initialize(
                shotRay.direction * muzzleSpeed, weaponType, gameObject);
        }

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
        if (IsBlindnessBlockingInput())
        {
            ClearBuildInteractionState();
            return;
        }

        if (viewCamera == null || voxelWorld == null)
        {
            return;
        }

        if (!BuildModeActive)
        {
            ClearBuildInteractionState();
            return;
        }

        if (_selectorOpen)
        {
            return;
        }

        if (_mouseMovedThisFrame)
        {
            _scrollTargetLocked = false;
        }

        HandleBuildOrientationInput();
        UpdateBuildCandidate();

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

    void UpdateRadialSelectorInput()
    {
        UpdateGrenadeSelectorInput();
        if (BuildModeActive)
        {
            UpdateBuildSelectorInput();
        }
    }

    void UpdateGrenadeSelectorInput()
    {
        if (_selectorOpen)
        {
            if (_grenadeSelectorOpen)
            {
                _grenadeSelectorOpen = false;
                _grenadeSelectorDirection = Vector2.zero;
            }

            if (_grenadeKeyHeld)
            {
                _grenadeKeyHeld = false;
                _grenadeKeyHoldTimer = 0f;
                _grenadeWheelOpenedFromHold = false;
            }

            return;
        }

        if (IsGrenadeInHand() || IsGrenadeHandCooldownActive())
        {
            if (_grenadeSelectorOpen)
            {
                _grenadeSelectorOpen = false;
                _grenadeSelectorDirection = Vector2.zero;
            }

            if (_grenadeKeyHeld)
            {
                _grenadeKeyHeld = false;
                _grenadeKeyHoldTimer = 0f;
                _grenadeWheelOpenedFromHold = false;
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!HasAnyGrenadesRemaining)
            {
                return;
            }

            _grenadeKeyHeld = true;
            _grenadeKeyHoldTimer = 0f;
            _grenadeWheelOpenedFromHold = false;
        }

        if (_grenadeKeyHeld && Input.GetKey(KeyCode.Q))
        {
            _grenadeKeyHoldTimer += Time.deltaTime;

            if (!_grenadeWheelOpenedFromHold && _grenadeKeyHoldTimer >= GrenadeWheelHoldSeconds)
            {
                _grenadeWheelOpenedFromHold = true;
                _selectorOpen = false;
                _selectorDirection = Vector2.zero;
                _grenadeSelectorOpen = true;
                _grenadeSelectorDirection = Vector2.zero;
            }

            if (_grenadeSelectorOpen)
            {
                _grenadeSelectorDirection += new Vector2(
                    Input.GetAxisRaw("Mouse X"),
                    Input.GetAxisRaw("Mouse Y")) * selectorMouseScale;

                if (_grenadeSelectorDirection.magnitude >= selectorActivationDistance)
                {
                    SelectGrenadeFromDirection(_grenadeSelectorDirection);
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            if (_grenadeSelectorOpen)
            {
                _grenadeSelectorOpen = false;
                _grenadeSelectorDirection = Vector2.zero;
                SelectGrenadeHotbarSlot();
            }
            else if (_grenadeKeyHeld && !_grenadeWheelOpenedFromHold)
            {
                SelectGrenadeHotbarSlot();
            }

            _grenadeKeyHeld = false;
            _grenadeKeyHoldTimer = 0f;
            _grenadeWheelOpenedFromHold = false;
        }
    }

    void UpdateBuildSelectorInput()
    {
        if (_grenadeSelectorOpen)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            _grenadeKeyHeld = false;
            _grenadeKeyHoldTimer = 0f;
            _grenadeWheelOpenedFromHold = false;
            _grenadeSelectorOpen = false;
            _grenadeSelectorDirection = Vector2.zero;
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
            _selectorDirection = Vector2.zero;
        }
    }

    void PrimeSelectedGrenade()
    {
        if (GetGrenadeCount(_selectedGrenade) <= 0)
        {
            return;
        }

        _grenadePrimed = true;
        _grenadeFuseTimer = fragGrenadeFuseSeconds;
        RefreshHeldToolVisibility();
    }

    void CancelGrenadePrime()
    {
        _grenadePrimed = false;
        _grenadeFuseTimer = 0f;
        RefreshHeldToolVisibility();
    }

    void ThrowGrenade()
    {
        if (viewCamera == null || GetGrenadeCount(_selectedGrenade) <= 0)
        {
            return;
        }

        Ray throwRay = BuildCenterAimRay();
        Vector3 spawnPosition = BulletSpawnPosition(throwRay);
        float remainingFuse = _grenadePrimed ? _grenadeFuseTimer : fragGrenadeFuseSeconds;
        var thrownType = _selectedGrenade;
        CancelGrenadePrime();
        ConsumeThrownGrenade(thrownType);
        BeginGrenadeHandCooldown();
        ThrownGrenadeProjectile.Spawn(thrownType, spawnPosition, throwRay.direction, remainingFuse);
    }

    void UpdateGrenadeFuseState()
    {
        if (!_grenadePrimed)
        {
            return;
        }

        _grenadeFuseTimer = Mathf.Max(0f, _grenadeFuseTimer - Time.deltaTime);
        if (_grenadeFuseTimer <= 0f)
        {
            DetonateHeldGrenade();
        }
    }

    void DetonateHeldGrenade()
    {
        if (GetGrenadeCount(_selectedGrenade) <= 0)
        {
            CancelGrenadePrime();
            return;
        }

        Vector3 center = transform.position + Vector3.up;
        var detonatedType = _selectedGrenade;
        CancelGrenadePrime();
        ConsumeThrownGrenade(detonatedType);
        BeginGrenadeHandCooldown();
        switch (detonatedType)
        {
            case GrenadeType.Flashbang:
                FlashbangBlindUtility.DetonateFlashbang(center);
                break;
            default:
                GrenadeBlastUtility.DetonateFrag(center);
                break;
        }
    }

    void EnsureGrenadeRadialTexture()
    {
        int highlightIndex = GetGrenadeWheelHighlightIndex();
        if (_grenadeRadialTexture != null && _grenadeRadialTextureHighlight == highlightIndex)
        {
            return;
        }

        const int size = 192;
        if (_grenadeRadialTexture == null)
        {
            _grenadeRadialTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        _grenadeRadialTextureHighlight = highlightIndex;
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
                    _grenadeRadialTexture.SetPixel(x, y, Color.clear);
                    continue;
                }

                int segmentIndex = GrenadeSegmentIndexFromAngle(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                bool selected = segmentIndex == highlightIndex;
                Color color = selected
                    ? SegmentHighlightColor(segmentIndex)
                    : new Color(0.98f, 0.98f, 0.98f, 0.36f);
                _grenadeRadialTexture.SetPixel(x, y, color);
            }
        }

        _grenadeRadialTexture.Apply();
    }

    static Color SegmentHighlightColor(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= GrenadeOptions.Length)
        {
            return new Color(0.42f, 0.44f, 0.46f, 0.5f);
        }

        return GrenadeOptions[segmentIndex] == GrenadeType.Flashbang
            ? new Color(0.36f, 0.37f, 0.38f, 0.55f)
            : new Color(0.42f, 0.44f, 0.46f, 0.5f);
    }

    int GetGrenadeWheelHighlightIndex()
    {
        if (_grenadeSelectorOpen &&
            _grenadeSelectorDirection.magnitude >= selectorActivationDistance)
        {
            return GrenadeSegmentIndexFromDirection(_grenadeSelectorDirection);
        }

        return GrenadeSegmentIndexForType(_selectedGrenade);
    }

    static int GrenadeSegmentIndexForType(GrenadeType grenadeType)
    {
        for (int i = 0; i < GrenadeOptions.Length; i++)
        {
            if (GrenadeOptions[i] == grenadeType)
            {
                return i;
            }
        }

        return 0;
    }

    static int GrenadeSegmentIndexFromDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return GrenadeSegmentIndexFromAngle(angle);
    }

    static int GrenadeSegmentIndexFromAngle(float angle)
    {
        float sectorSize = 360f / GrenadeWheelSegmentCount;
        float normalized = Mathf.Repeat(angle + (sectorSize * 0.5f), 360f);
        return Mathf.FloorToInt(normalized / sectorSize);
    }

    void SelectGrenadeFromDirection(Vector2 direction)
    {
        if (IsGrenadeInHand())
        {
            return;
        }

        int segmentIndex = GrenadeSegmentIndexFromDirection(direction);
        if (segmentIndex < 0 || segmentIndex >= GrenadeOptions.Length)
        {
            return;
        }

        var nextGrenade = GrenadeOptions[segmentIndex];
        if (GetGrenadeCount(nextGrenade) <= 0)
        {
            return;
        }

        if (nextGrenade != _selectedGrenade)
        {
            _selectedGrenade = nextGrenade;
        }
    }

    static Color HeldGrenadeColor(GrenadeType grenadeType)
    {
        switch (grenadeType)
        {
            case GrenadeType.Flashbang:
                return new Color(0.34f, 0.35f, 0.36f, 1f);
            default:
                return new Color(0.42f, 0.44f, 0.46f, 1f);
        }
    }

    static string GrenadeDisplayName(GrenadeType grenadeType)
    {
        switch (grenadeType)
        {
            case GrenadeType.Frag:
                return "Frag";
            case GrenadeType.Flashbang:
                return "Flash";
            default:
                return grenadeType.ToString();
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
        Material c4Material = CreateHeldToolMaterial("Held C4 Material", new Color(0.08f, 0.08f, 0.08f, 1f));
        Material remoteMaterial = CreateHeldToolMaterial("Held C4 Remote Material", new Color(0.12f, 0.12f, 0.13f, 1f));
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

        _scopedAssaultRifleRoot = new GameObject("Held Scoped Assault Rifle");
        _scopedAssaultRifleRoot.transform.SetParent(viewCamera.transform, false);
        _scopedAssaultRifleRoot.transform.localPosition = new Vector3(0.3f, -0.24f, 0.58f);
        _scopedAssaultRifleRoot.transform.localRotation = Quaternion.Euler(0f, -4f, 0f);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Body", new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.14f, 0.52f), gunMaterial);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Barrel", new Vector3(0.03f, 0.02f, 0.42f), new Vector3(0.08f, 0.08f, 0.62f), gunMaterial);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Stock", new Vector3(-0.02f, -0.02f, -0.28f), new Vector3(0.1f, 0.12f, 0.22f), gunMaterial);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Grip", new Vector3(0f, -0.14f, -0.02f), new Vector3(0.07f, 0.18f, 0.08f), gunMaterial);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Mag", new Vector3(0f, -0.12f, 0.08f), new Vector3(0.06f, 0.16f, 0.1f), gunMaterial);
        CreateHeldCube(_scopedAssaultRifleRoot.transform, "Scoped AR Scope", new Vector3(0f, 0.09f, 0.06f), new Vector3(0.07f, 0.07f, 0.22f), gunMaterial);
        _scopedAssaultRifleMuzzleFlashRoot = CreateHeldCube(
            _scopedAssaultRifleRoot.transform,
            "Scoped AR Muzzle Flash",
            new Vector3(0.03f, 0.02f, 0.74f),
            new Vector3(0.16f, 0.16f, 0.08f),
            flashMaterial);
        _scopedAssaultRifleMuzzleFlashRoot.SetActive(false);

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

        _huntingRifleRoot = new GameObject("Held Hunting Rifle");
        _huntingRifleRoot.transform.SetParent(viewCamera.transform, false);
        _huntingRifleRoot.transform.localPosition = new Vector3(0.28f, -0.22f, 0.56f);
        _huntingRifleRoot.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
        CreateHeldCube(_huntingRifleRoot.transform, "Hunting Body", new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.12f, 0.62f), gunMaterial);
        CreateHeldCube(_huntingRifleRoot.transform, "Hunting Barrel", new Vector3(0.02f, 0.02f, 0.48f), new Vector3(0.06f, 0.06f, 0.82f), gunMaterial);
        CreateHeldCube(_huntingRifleRoot.transform, "Hunting Stock", new Vector3(-0.02f, -0.02f, -0.32f), new Vector3(0.1f, 0.11f, 0.22f), gunMaterial);
        CreateHeldCube(_huntingRifleRoot.transform, "Hunting Sight", new Vector3(0f, 0.08f, 0.04f), new Vector3(0.05f, 0.04f, 0.1f), gunMaterial);
        CreateHeldCube(_huntingRifleRoot.transform, "Hunting Grip", new Vector3(0f, -0.13f, -0.04f), new Vector3(0.06f, 0.16f, 0.08f), gunMaterial);
        _huntingRifleMuzzleFlashRoot = CreateHeldCube(
            _huntingRifleRoot.transform,
            "Hunting Muzzle Flash",
            new Vector3(0.02f, 0.02f, 0.9f),
            new Vector3(0.14f, 0.14f, 0.08f),
            flashMaterial);
        _huntingRifleMuzzleFlashRoot.SetActive(false);

        _antiMaterialRifleRoot = new GameObject("Held Anti-Material Rifle");
        _antiMaterialRifleRoot.transform.SetParent(viewCamera.transform, false);
        _antiMaterialRifleRoot.transform.localPosition = new Vector3(0.26f, -0.22f, 0.54f);
        _antiMaterialRifleRoot.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
        CreateHeldCube(_antiMaterialRifleRoot.transform, "AM Body", new Vector3(0f, 0f, 0f), new Vector3(0.18f, 0.14f, 0.82f), gunMaterial);
        CreateHeldCube(_antiMaterialRifleRoot.transform, "AM Barrel", new Vector3(0.02f, 0.02f, 0.62f), new Vector3(0.08f, 0.08f, 1.08f), gunMaterial);
        CreateHeldCube(_antiMaterialRifleRoot.transform, "AM Stock", new Vector3(-0.02f, -0.02f, -0.4f), new Vector3(0.12f, 0.12f, 0.28f), gunMaterial);
        CreateHeldCube(_antiMaterialRifleRoot.transform, "AM Scope", new Vector3(0f, 0.12f, 0.1f), new Vector3(0.1f, 0.1f, 0.34f), gunMaterial);
        CreateHeldCube(_antiMaterialRifleRoot.transform, "AM Grip", new Vector3(0f, -0.15f, -0.04f), new Vector3(0.07f, 0.18f, 0.09f), gunMaterial);
        _antiMaterialMuzzleFlashRoot = CreateHeldCube(
            _antiMaterialRifleRoot.transform,
            "AM Muzzle Flash",
            new Vector3(0.02f, 0.02f, 1.16f),
            new Vector3(0.18f, 0.18f, 0.1f),
            flashMaterial);
        _antiMaterialMuzzleFlashRoot.SetActive(false);

        _smgRoot = new GameObject("Held SMG");
        _smgRoot.transform.SetParent(viewCamera.transform, false);
        _smgRoot.transform.localPosition = new Vector3(0.32f, -0.25f, 0.6f);
        _smgRoot.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);
        CreateHeldCube(_smgRoot.transform, "SMG Body", new Vector3(0f, 0f, 0f), new Vector3(0.16f, 0.12f, 0.22f), gunMaterial);
        CreateHeldCube(_smgRoot.transform, "SMG Barrel", new Vector3(0.03f, 0.02f, 0.16f), new Vector3(0.07f, 0.07f, 0.24f), gunMaterial);
        CreateHeldCube(_smgRoot.transform, "SMG Grip", new Vector3(-0.02f, -0.12f, -0.02f), new Vector3(0.06f, 0.14f, 0.08f), gunMaterial);
        CreateHeldCube(_smgRoot.transform, "SMG Mag", new Vector3(0f, -0.1f, 0.02f), new Vector3(0.05f, 0.12f, 0.08f), gunMaterial);
        _smgMuzzleFlashRoot = CreateHeldCube(_smgRoot.transform, "SMG Muzzle Flash", new Vector3(0.03f, 0.02f, 0.28f), new Vector3(0.14f, 0.14f, 0.06f), flashMaterial);
        _smgMuzzleFlashRoot.SetActive(false);

        _machinePistolRoot = new GameObject("Held Machine Pistol");
        _machinePistolRoot.transform.SetParent(viewCamera.transform, false);
        _machinePistolRoot.transform.localPosition = new Vector3(0.34f, -0.26f, 0.62f);
        _machinePistolRoot.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);
        CreateHeldCube(_machinePistolRoot.transform, "MP Body", new Vector3(0f, 0f, 0f), new Vector3(0.14f, 0.11f, 0.18f), gunMaterial);
        CreateHeldCube(_machinePistolRoot.transform, "MP Barrel", new Vector3(0.03f, 0.02f, 0.12f), new Vector3(0.06f, 0.06f, 0.2f), gunMaterial);
        CreateHeldCube(_machinePistolRoot.transform, "MP Grip", new Vector3(-0.02f, -0.11f, -0.02f), new Vector3(0.05f, 0.12f, 0.07f), gunMaterial);
        CreateHeldCube(_machinePistolRoot.transform, "MP Mag", new Vector3(0f, -0.09f, 0.01f), new Vector3(0.04f, 0.1f, 0.07f), gunMaterial);
        _machinePistolMuzzleFlashRoot = CreateHeldCube(
            _machinePistolRoot.transform,
            "MP Muzzle Flash",
            new Vector3(0.03f, 0.02f, 0.22f),
            new Vector3(0.12f, 0.12f, 0.05f),
            flashMaterial);
        _machinePistolMuzzleFlashRoot.SetActive(false);

        _lmgRoot = new GameObject("Held LMG");
        _lmgRoot.transform.SetParent(viewCamera.transform, false);
        _lmgRoot.transform.localPosition = new Vector3(0.28f, -0.23f, 0.55f);
        _lmgRoot.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
        CreateHeldCube(_lmgRoot.transform, "LMG Body", new Vector3(0f, 0f, 0f), new Vector3(0.16f, 0.13f, 0.58f), gunMaterial);
        CreateHeldCube(_lmgRoot.transform, "LMG Barrel", new Vector3(0.02f, 0.02f, 0.46f), new Vector3(0.07f, 0.07f, 0.78f), gunMaterial);
        CreateHeldCube(_lmgRoot.transform, "LMG Stock", new Vector3(-0.02f, -0.02f, -0.3f), new Vector3(0.1f, 0.11f, 0.22f), gunMaterial);
        CreateHeldCube(_lmgRoot.transform, "LMG Grip", new Vector3(0f, -0.14f, -0.02f), new Vector3(0.07f, 0.17f, 0.08f), gunMaterial);
        CreateHeldCube(_lmgRoot.transform, "LMG Box Mag", new Vector3(0f, -0.1f, 0.04f), new Vector3(0.08f, 0.14f, 0.12f), gunMaterial);
        _lmgMuzzleFlashRoot = CreateHeldCube(_lmgRoot.transform, "LMG Muzzle Flash", new Vector3(0.02f, 0.02f, 0.86f), new Vector3(0.15f, 0.15f, 0.08f), flashMaterial);
        _lmgMuzzleFlashRoot.SetActive(false);

        _machineGunRoot = new GameObject("Held Machine Gun");
        _machineGunRoot.transform.SetParent(viewCamera.transform, false);
        _machineGunRoot.transform.localPosition = new Vector3(0.26f, -0.24f, 0.54f);
        _machineGunRoot.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);
        CreateHeldCube(_machineGunRoot.transform, "MG Body", new Vector3(0f, 0f, 0f), new Vector3(0.17f, 0.14f, 0.64f), gunMaterial);
        CreateHeldCube(_machineGunRoot.transform, "MG Barrel", new Vector3(0.02f, 0.02f, 0.5f), new Vector3(0.08f, 0.08f, 0.86f), gunMaterial);
        CreateHeldCube(_machineGunRoot.transform, "MG Stock", new Vector3(-0.02f, -0.02f, -0.34f), new Vector3(0.11f, 0.12f, 0.24f), gunMaterial);
        CreateHeldCube(_machineGunRoot.transform, "MG Grip", new Vector3(0f, -0.15f, -0.02f), new Vector3(0.08f, 0.18f, 0.09f), gunMaterial);
        CreateHeldCube(_machineGunRoot.transform, "MG Drum", new Vector3(0f, -0.12f, 0.06f), new Vector3(0.1f, 0.16f, 0.14f), gunMaterial);
        _machineGunMuzzleFlashRoot = CreateHeldCube(
            _machineGunRoot.transform,
            "MG Muzzle Flash",
            new Vector3(0.02f, 0.02f, 0.94f),
            new Vector3(0.16f, 0.16f, 0.08f),
            flashMaterial);
        _machineGunMuzzleFlashRoot.SetActive(false);

        Material laserMaterial = CreateHeldToolMaterial("Held Laser Material", new Color(0.92f, 0.12f, 0.1f, 1f));
        Material laserFlashMaterial = CreateHeldToolMaterial("Laser Muzzle Flash Material", new Color(1f, 0.28f, 0.22f, 1f));
        Material swordMaterial = CreateHeldToolMaterial("Held Laser Sword Material", new Color(0.95f, 0.14f, 0.12f, 1f));

        _cyborgLaserRoot = new GameObject("Held Cyborg Laser");
        _cyborgLaserRoot.transform.SetParent(viewCamera.transform, false);
        _cyborgLaserRoot.transform.localPosition = new Vector3(0.36f, -0.28f, 0.58f);
        _cyborgLaserRoot.transform.localRotation = Quaternion.Euler(8f, -8f, -18f);
        CreateHeldCube(_cyborgLaserRoot.transform, "Laser Forearm", new Vector3(0f, 0f, 0f), new Vector3(0.1f, 0.1f, 0.34f), gunMaterial);
        CreateHeldCube(_cyborgLaserRoot.transform, "Laser Emitter", new Vector3(0.02f, 0.02f, 0.24f), new Vector3(0.06f, 0.06f, 0.18f), laserMaterial);
        CreateHeldCube(_cyborgLaserRoot.transform, "Laser Barrel", new Vector3(0.02f, 0.02f, 0.38f), new Vector3(0.04f, 0.04f, 0.28f), laserMaterial);
        _cyborgLaserMuzzleFlashRoot = CreateHeldCube(
            _cyborgLaserRoot.transform,
            "Laser Muzzle Flash",
            new Vector3(0.02f, 0.02f, 0.54f),
            new Vector3(0.12f, 0.12f, 0.08f),
            laserFlashMaterial);
        _cyborgLaserMuzzleFlashRoot.SetActive(false);

        _laserSwordRoot = new GameObject("Held Laser Sword");
        _laserSwordRoot.transform.SetParent(viewCamera.transform, false);
        _laserSwordRoot.transform.localPosition = new Vector3(0.34f, -0.3f, 0.58f);
        _laserSwordRoot.transform.localRotation = Quaternion.Euler(-12f, 8f, 12f);
        CreateHeldCube(_laserSwordRoot.transform, "Sword Handle", new Vector3(0f, -0.06f, 0f), new Vector3(0.06f, 0.18f, 0.06f), gunMaterial);
        CreateHeldCube(_laserSwordRoot.transform, "Sword Guard", new Vector3(0f, 0.04f, 0f), new Vector3(0.16f, 0.03f, 0.05f), metalMaterial);
        CreateHeldCube(_laserSwordRoot.transform, "Sword Blade", new Vector3(0f, 0.22f, 0.02f), new Vector3(0.05f, 0.42f, 0.02f), swordMaterial);

        _c4ChargeRoot = new GameObject("Held C4 Charge");
        _c4ChargeRoot.transform.SetParent(viewCamera.transform, false);
        _c4ChargeRoot.transform.localPosition = new Vector3(0.32f, -0.28f, 0.56f);
        _c4ChargeRoot.transform.localRotation = Quaternion.Euler(12f, -8f, -8f);
        CreateHeldCube(_c4ChargeRoot.transform, "C4 Brick", Vector3.zero, new Vector3(0.32f, 0.24f, 0.1f), c4Material);
        CreateHeldCube(_c4ChargeRoot.transform, "C4 Strap", new Vector3(0f, 0.02f, 0f), new Vector3(0.38f, 0.05f, 0.12f), metalMaterial);
        CreateHeldCube(_c4ChargeRoot.transform, "C4 Button", new Vector3(-0.09f, 0.14f, 0f), new Vector3(0.09f, 0.04f, 0.1f), flashMaterial);

        _c4RemoteRoot = new GameObject("Held C4 Remote");
        _c4RemoteRoot.transform.SetParent(viewCamera.transform, false);
        _c4RemoteRoot.transform.localPosition = new Vector3(0.34f, -0.27f, 0.58f);
        _c4RemoteRoot.transform.localRotation = Quaternion.Euler(10f, -8f, -8f);
        CreateHeldCube(_c4RemoteRoot.transform, "Remote Body", Vector3.zero, new Vector3(0.16f, 0.22f, 0.08f), remoteMaterial);
        CreateHeldCube(_c4RemoteRoot.transform, "Remote Button", new Vector3(0f, 0.05f, 0.05f), new Vector3(0.08f, 0.08f, 0.03f), flashMaterial);

        Material grenadeMaterial = CreateHeldToolMaterial("Held Frag Grenade Material", new Color(0.42f, 0.44f, 0.46f, 1f));
        _heldFragGrenadeRoot = new GameObject("Held Grenade");
        _heldFragGrenadeRoot.transform.SetParent(viewCamera.transform, false);
        _heldFragGrenadeRoot.transform.localPosition = new Vector3(0.34f, -0.26f, 0.54f);
        _heldFragGrenadeRoot.transform.localRotation = Quaternion.Euler(18f, -8f, -12f);
        var grenadeBody = CreateHeldCube(_heldFragGrenadeRoot.transform, "Grenade Body", Vector3.zero, new Vector3(0.12f, 0.12f, 0.12f), grenadeMaterial);
        _heldGrenadeBodyRenderer = grenadeBody.GetComponent<MeshRenderer>();
        CreateHeldCube(_heldFragGrenadeRoot.transform, "Grenade Pin", new Vector3(0.05f, 0.06f, 0f), new Vector3(0.03f, 0.05f, 0.03f), metalMaterial);

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

        if (_scopedAssaultRifleRoot != null)
        {
            _scopedAssaultRifleRoot.SetActive(ShouldShowScopedArHeldModel());
        }

        if (_sniperRifleRoot != null)
        {
            _sniperRifleRoot.SetActive(ShouldShowSniperHeldModel());
        }

        if (_huntingRifleRoot != null)
        {
            _huntingRifleRoot.SetActive(ShouldShowHuntingRifleHeldModel());
        }

        if (_antiMaterialRifleRoot != null)
        {
            _antiMaterialRifleRoot.SetActive(ShouldShowAntiMaterialHeldModel());
        }

        if (_smgRoot != null)
        {
            _smgRoot.SetActive(SelectedTool == CardHotbarTool.Smg);
        }

        if (_machinePistolRoot != null)
        {
            _machinePistolRoot.SetActive(SelectedTool == CardHotbarTool.MachinePistol);
        }

        if (_lmgRoot != null)
        {
            _lmgRoot.SetActive(SelectedTool == CardHotbarTool.LightMachineGun);
        }

        if (_machineGunRoot != null)
        {
            _machineGunRoot.SetActive(SelectedTool == CardHotbarTool.MachineGun);
        }

        if (_cyborgLaserRoot != null)
        {
            _cyborgLaserRoot.SetActive(SelectedTool == CardHotbarTool.CyborgLaser);
        }

        if (_laserSwordRoot != null)
        {
            _laserSwordRoot.SetActive(SelectedTool == CardHotbarTool.LaserSword);
        }

        if (_c4ChargeRoot != null)
        {
            _c4ChargeRoot.SetActive(
                SelectedTool == CardHotbarTool.C4Charge &&
                _activeC4Charge == null &&
                _c4Ammo.CanFire);
        }

        if (_c4RemoteRoot != null)
        {
            _c4RemoteRoot.SetActive(
                SelectedTool == CardHotbarTool.C4Charge &&
                _activeC4Charge != null &&
                _activeC4Charge.CanRemoteDetonate &&
                (_c4RemoteDrawTimer > 0f || _c4RemoteReady));
        }

        if (_heldFragGrenadeRoot != null)
        {
            _heldFragGrenadeRoot.SetActive(_grenadePrimed);
            if (_heldGrenadeBodyRenderer != null && _grenadePrimed)
            {
                _heldGrenadeBodyRenderer.sharedMaterial.color = HeldGrenadeColor(_selectedGrenade);
            }
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

    bool ShouldShowScopedArHeldModel()
    {
        return SelectedTool == CardHotbarTool.ScopedAssaultRifle &&
            (!_scopedArAdsHeld || _scopedArScopeOverlayBlend < 0.35f);
    }

    bool ShouldShowSniperHeldModel()
    {
        return SelectedTool == CardHotbarTool.SniperRifle &&
            (!_sniperAimingHeld || !IsMagnifiedSniperScope(_sniperScopeIndex));
    }

    bool ShouldShowHuntingRifleHeldModel()
    {
        return SelectedTool == CardHotbarTool.HuntingRifle &&
            (!_sniperAimingHeld || _sniperScopeOverlayBlend < 0.35f);
    }

    bool ShouldShowAntiMaterialHeldModel()
    {
        return SelectedTool == CardHotbarTool.AntiMaterialRifle &&
            (!_sniperAimingHeld || _sniperScopeOverlayBlend < 0.35f);
    }

    void UpdateHeldToolVisuals()
    {
        if (_pistolRoot == null || _assaultRifleRoot == null || _scopedAssaultRifleRoot == null ||
            _sniperRifleRoot == null || _huntingRifleRoot == null || _antiMaterialRifleRoot == null ||
            _smgRoot == null || _machinePistolRoot == null || _lmgRoot == null || _cyborgLaserRoot == null ||
            _laserSwordRoot == null || _c4ChargeRoot == null || _c4RemoteRoot == null || _hammerRoot == null)
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

        if (_laserSwordSwingTimer > 0f)
        {
            _laserSwordSwingTimer = Mathf.Max(0f, _laserSwordSwingTimer - Time.deltaTime);
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

        Vector3 scopedArPosition = new Vector3(0.3f, -0.24f, 0.58f - (0.06f * kickProgress));
        Quaternion scopedArRotation = Quaternion.Euler(0f, -4f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.ScopedAssaultRifle, ref scopedArPosition);
        ApplyReloadDipToGun(CardHotbarTool.ScopedAssaultRifle, ref scopedArPosition, ref scopedArRotation);
        _scopedAssaultRifleRoot.transform.localPosition = scopedArPosition;
        _scopedAssaultRifleRoot.transform.localRotation = scopedArRotation;

        Vector3 sniperPosition = new Vector3(0.28f, -0.22f, 0.56f - (0.05f * kickProgress));
        Quaternion sniperRotation = Quaternion.Euler(0f, -3f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.SniperRifle, ref sniperPosition);
        ApplyReloadDipToGun(CardHotbarTool.SniperRifle, ref sniperPosition, ref sniperRotation);
        _sniperRifleRoot.transform.localPosition = sniperPosition;
        _sniperRifleRoot.transform.localRotation = sniperRotation;

        Vector3 huntingPosition = new Vector3(0.28f, -0.22f, 0.56f - (0.05f * kickProgress));
        Quaternion huntingRotation = Quaternion.Euler(0f, -3f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.HuntingRifle, ref huntingPosition);
        ApplyReloadDipToGun(CardHotbarTool.HuntingRifle, ref huntingPosition, ref huntingRotation);
        _huntingRifleRoot.transform.localPosition = huntingPosition;
        _huntingRifleRoot.transform.localRotation = huntingRotation;

        float chargeProgress = _antiMaterialCharging
            ? Mathf.Clamp01(_antiMaterialChargeTimer / Mathf.Max(0.01f, antiMaterialChargeSeconds))
            : 0f;
        Vector3 antiMaterialPosition;
        Quaternion antiMaterialRotation;
        if (IsAntiMaterialBraceActive())
        {
            antiMaterialPosition = new Vector3(
                0f,
                -0.2f,
                0.66f - (0.12f * chargeProgress) - (0.04f * kickProgress));
            antiMaterialRotation = Quaternion.Euler(
                -(4f * chargeProgress),
                0f,
                0f);
        }
        else
        {
            antiMaterialPosition = new Vector3(
                0.26f,
                -0.22f,
                0.54f - (0.18f * chargeProgress) - (0.05f * kickProgress));
            antiMaterialRotation = Quaternion.Euler(-4f * chargeProgress, -3f, 0f);
        }

        ApplyWeaponDrawOffset(CardHotbarTool.AntiMaterialRifle, ref antiMaterialPosition);
        ApplyReloadDipToGun(CardHotbarTool.AntiMaterialRifle, ref antiMaterialPosition, ref antiMaterialRotation);
        _antiMaterialRifleRoot.transform.localPosition = antiMaterialPosition;
        _antiMaterialRifleRoot.transform.localRotation = antiMaterialRotation;

        Vector3 smgPosition = new Vector3(0.32f, -0.25f, 0.6f - (0.07f * kickProgress));
        Quaternion smgRotation = Quaternion.Euler(0f, -5f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.Smg, ref smgPosition);
        ApplyReloadDipToGun(CardHotbarTool.Smg, ref smgPosition, ref smgRotation);
        _smgRoot.transform.localPosition = smgPosition;
        _smgRoot.transform.localRotation = smgRotation;

        Vector3 machinePistolPosition = new Vector3(0.34f, -0.26f, 0.62f - (0.075f * kickProgress));
        Quaternion machinePistolRotation = Quaternion.Euler(0f, -5f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.MachinePistol, ref machinePistolPosition);
        ApplyReloadDipToGun(CardHotbarTool.MachinePistol, ref machinePistolPosition, ref machinePistolRotation);
        _machinePistolRoot.transform.localPosition = machinePistolPosition;
        _machinePistolRoot.transform.localRotation = machinePistolRotation;

        Vector3 lmgPosition = new Vector3(0.28f, -0.23f, 0.55f - (0.055f * kickProgress));
        Quaternion lmgRotation = Quaternion.Euler(0f, -3f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.LightMachineGun, ref lmgPosition);
        ApplyReloadDipToGun(CardHotbarTool.LightMachineGun, ref lmgPosition, ref lmgRotation);
        _lmgRoot.transform.localPosition = lmgPosition;
        _lmgRoot.transform.localRotation = lmgRotation;

        Vector3 machineGunPosition = new Vector3(0.26f, -0.24f, 0.54f - (0.06f * kickProgress));
        Quaternion machineGunRotation = Quaternion.Euler(0f, -3f, 0f);
        ApplyWeaponDrawOffset(CardHotbarTool.MachineGun, ref machineGunPosition);
        ApplyReloadDipToGun(CardHotbarTool.MachineGun, ref machineGunPosition, ref machineGunRotation);
        _machineGunRoot.transform.localPosition = machineGunPosition;
        _machineGunRoot.transform.localRotation = machineGunRotation;

        Vector3 laserPosition = new Vector3(0.36f, -0.28f, 0.58f - (0.07f * kickProgress));
        Quaternion laserRotation = Quaternion.Euler(8f, -8f, -18f);
        ApplyWeaponDrawOffset(CardHotbarTool.CyborgLaser, ref laserPosition);
        _cyborgLaserRoot.transform.localPosition = laserPosition;
        _cyborgLaserRoot.transform.localRotation = laserRotation;

        float swordSwingProgress = _laserSwordSwingTimer > 0f
            ? Mathf.Sin((1f - (_laserSwordSwingTimer / laserSwordSwingSeconds)) * Mathf.PI)
            : 0f;
        Vector3 swordPosition = new Vector3(0.34f, -0.3f, 0.58f);
        Quaternion swordRotation = Quaternion.Euler(-12f - (48f * swordSwingProgress), 8f, 12f + (24f * swordSwingProgress));
        _laserSwordRoot.transform.localPosition = swordPosition;
        _laserSwordRoot.transform.localRotation = swordRotation;

        Vector3 c4Position = new Vector3(0.32f, -0.28f, 0.56f);
        Quaternion c4Rotation = Quaternion.Euler(12f, -8f, -8f);
        ApplyWeaponDrawOffset(CardHotbarTool.C4Charge, ref c4Position);
        _c4ChargeRoot.transform.localPosition = c4Position;
        _c4ChargeRoot.transform.localRotation = c4Rotation;

        float remoteProgress = c4RemoteDrawSeconds <= 0f || _c4RemoteReady
            ? 1f
            : 1f - (_c4RemoteDrawTimer / Mathf.Max(0.01f, c4RemoteDrawSeconds));
        remoteProgress = Mathf.Clamp01(remoteProgress);
        remoteProgress = remoteProgress * remoteProgress * (3f - (2f * remoteProgress));
        float remoteHiddenBlend = 1f - remoteProgress;
        Vector3 c4RemotePosition = new Vector3(
            0.34f,
            -0.27f + (weaponDrawHiddenLocalY * remoteHiddenBlend),
            0.58f + (0.05f * remoteHiddenBlend));
        Quaternion c4RemoteRotation = Quaternion.Euler(10f, -8f, -8f);
        _c4RemoteRoot.transform.localPosition = c4RemotePosition;
        _c4RemoteRoot.transform.localRotation = c4RemoteRotation;

        bool showPistolFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.Pistol;
        bool showArFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.AssaultRifle;
        bool showScopedArFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.ScopedAssaultRifle;
        bool showSniperFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.SniperRifle;
        bool showHuntingFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.HuntingRifle;
        bool showAntiMaterialFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.AntiMaterialRifle;
        bool showSmgFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.Smg;
        bool showMachinePistolFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.MachinePistol;
        bool showLmgFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.LightMachineGun;
        bool showMachineGunFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.MachineGun;
        bool showLaserFlash = _muzzleFlashTimer > 0f && SelectedTool == CardHotbarTool.CyborgLaser;
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

        if (_scopedAssaultRifleMuzzleFlashRoot != null)
        {
            _scopedAssaultRifleMuzzleFlashRoot.SetActive(showScopedArFlash);
            _scopedAssaultRifleMuzzleFlashRoot.transform.localScale = new Vector3(0.16f, 0.16f, 0.08f) * flashPulse;
        }

        if (_sniperMuzzleFlashRoot != null)
        {
            _sniperMuzzleFlashRoot.SetActive(showSniperFlash);
            _sniperMuzzleFlashRoot.transform.localScale = new Vector3(0.14f, 0.14f, 0.08f) * flashPulse;
        }

        if (_huntingRifleMuzzleFlashRoot != null)
        {
            _huntingRifleMuzzleFlashRoot.SetActive(showHuntingFlash);
            _huntingRifleMuzzleFlashRoot.transform.localScale = new Vector3(0.14f, 0.14f, 0.08f) * flashPulse;
        }

        if (_antiMaterialMuzzleFlashRoot != null)
        {
            _antiMaterialMuzzleFlashRoot.SetActive(showAntiMaterialFlash);
            _antiMaterialMuzzleFlashRoot.transform.localScale = new Vector3(0.18f, 0.18f, 0.1f) * flashPulse;
        }

        if (_smgMuzzleFlashRoot != null)
        {
            _smgMuzzleFlashRoot.SetActive(showSmgFlash);
            _smgMuzzleFlashRoot.transform.localScale = new Vector3(0.14f, 0.14f, 0.06f) * flashPulse;
        }

        if (_machinePistolMuzzleFlashRoot != null)
        {
            _machinePistolMuzzleFlashRoot.SetActive(showMachinePistolFlash);
            _machinePistolMuzzleFlashRoot.transform.localScale = new Vector3(0.12f, 0.12f, 0.05f) * flashPulse;
        }

        if (_lmgMuzzleFlashRoot != null)
        {
            _lmgMuzzleFlashRoot.SetActive(showLmgFlash);
            _lmgMuzzleFlashRoot.transform.localScale = new Vector3(0.15f, 0.15f, 0.08f) * flashPulse;
        }

        if (_machineGunMuzzleFlashRoot != null)
        {
            _machineGunMuzzleFlashRoot.SetActive(showMachineGunFlash);
            _machineGunMuzzleFlashRoot.transform.localScale = new Vector3(0.16f, 0.16f, 0.08f) * flashPulse;
        }

        if (_cyborgLaserMuzzleFlashRoot != null)
        {
            _cyborgLaserMuzzleFlashRoot.SetActive(showLaserFlash);
            _cyborgLaserMuzzleFlashRoot.transform.localScale = new Vector3(0.12f, 0.12f, 0.08f) * flashPulse;
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
