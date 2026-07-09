using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session-only shooting range state (not persisted to profile).
/// </summary>
public static class ShootingRangeSession
{
    public static readonly int[] TargetDistancesMeters = { 10, 50, 100, 200, 300, 400, 500, 600 };

    public const float FiringLineWorldZ = 0f;
    public const float BehindZoneDepthMeters = 10f;
    public const float TargetSpreadRightX = 16f;
    public const float TargetSpreadLeftX = -16f;

    static readonly List<ShootingRangeDummy> _dummies = new List<ShootingRangeDummy>();

    static float _dummyMaxHealth = 100f;
    static bool _movingDummies;
    static float _dummyMoveMinX = -22f;
    static float _dummyMoveMaxX = 22f;
    static VoxelLightingWorld _voxelWorld;
    static ThirdPersonController _player;

    public static IReadOnlyList<ShootingRangeDummy> Dummies => _dummies;

    public static float GridOriginWorldZ => -BehindZoneDepthMeters;

    public static Vector3 PlayerSpawnPosition =>
        new Vector3(0f, 1.1f, FiringLineWorldZ - BehindZoneDepthMeters * 0.5f);

    public static float TargetWorldZ(int distanceMeters) => FiringLineWorldZ + distanceMeters;

    public static float TargetWorldX(int distanceMeters)
    {
        int minDistance = TargetDistancesMeters[0];
        int maxDistance = TargetDistancesMeters[TargetDistancesMeters.Length - 1];
        float t = Mathf.InverseLerp(minDistance, maxDistance, distanceMeters);
        return Mathf.Lerp(TargetSpreadRightX, TargetSpreadLeftX, t);
    }

    public static float DummyMaxHealth
    {
        get => _dummyMaxHealth;
        set => _dummyMaxHealth = Mathf.Clamp(value, 10f, 1000f);
    }

    public static bool MovingDummies
    {
        get => _movingDummies;
        set => _movingDummies = value;
    }

    public const float DummyMoveSpeed = 2.2f;

    public static float DummyMoveMinX => _dummyMoveMinX;

    public static float DummyMoveMaxX => _dummyMoveMaxX;

    public static void SetMovementBounds(float minX, float maxX)
    {
        _dummyMoveMinX = Mathf.Min(minX, maxX);
        _dummyMoveMaxX = Mathf.Max(minX, maxX);
    }

    public static void Initialize(VoxelLightingWorld voxelWorld, ThirdPersonController player)
    {
        _voxelWorld = voxelWorld;
        _player = player;
        _dummyMaxHealth = 100f;
        _movingDummies = false;
        _dummies.Clear();
    }

    public static void SetPlayer(ThirdPersonController player)
    {
        _player = player;
    }

    public static void Clear()
    {
        ClearProjectileEntities();
        _dummies.Clear();
        _voxelWorld = null;
        _player = null;
    }

    public static void RegisterDummy(ShootingRangeDummy dummy)
    {
        if (dummy != null && !_dummies.Contains(dummy))
        {
            _dummies.Add(dummy);
        }
    }

    public static void ResetAllDummies()
    {
        for (int i = 0; i < _dummies.Count; i++)
        {
            _dummies[i]?.RefillHealth();
        }
    }

    public static void ResetDummyPositions()
    {
        for (int i = 0; i < _dummies.Count; i++)
        {
            _dummies[i]?.ResetToStandPosition();
        }
    }

    public static void ClearProjectileEntities()
    {
        ProjectileBullet.DestroyAll();
        AntiMaterialProjectile.DestroyAll();
        AntiMaterialExplosionEffect.DestroyAll();
    }

    public static void ResetMap()
    {
        ClearProjectileEntities();
        _voxelWorld?.ClearAllPlayerBuilt();
        ResetAllDummies();
        _player?.ResetToSpawn();
    }

    public static float HealthSliderToValue(float slider01)
    {
        float t = Mathf.Clamp01(slider01);
        return Mathf.Round(10f * Mathf.Pow(100f, t));
    }

    public static float HealthValueToSlider(float health)
    {
        health = Mathf.Clamp(health, 10f, 1000f);
        return Mathf.Log10(health / 10f) / 2f;
    }
}
