using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Raycast-integrated bullet with gravity and air drag. Penetrates player builds
/// (speed loss), sniper rounds penetrate players above a speed threshold, and
/// all bullets stop on map geometry without bouncing.
/// </summary>
public class ProjectileBullet : MonoBehaviour
{
    const float Radius = 0.0275f;
    const float KillY = -12f;
    const float MaxLifetimeSeconds = 20f;
    const float SniperPlayerPenetrationMinSpeed = 500f;
    const float DamageEpsilon = 0.001f;
    const float SurfaceImpactSpeedRetention = 0.5f;
    const float SniperPlayerSpeedRetention = 0.85f;
    const float SniperAccuracyDeflectionDegrees = 0.24f;
    const float MinApparentSizeDistanceMeters = 50f;

    static readonly List<ProjectileBullet> LiveBullets = new List<ProjectileBullet>();
    static Material _bulletMaterial;

    Vector3 _velocity;
    float _muzzleSpeed;
    ProjectileWeaponType _weaponType;
    float _spawnTime;
    float _traveledDistance;
    float _maxTravelDistance = -1f;
    readonly HashSet<GameObject> _penetratedCharacters = new HashSet<GameObject>();
    Renderer _renderer;
    Transform _bulletVisual;

    GameObject _ownerRoot;
    bool _canHitOwner;

    public void Initialize(Vector3 velocity, ProjectileWeaponType weaponType, GameObject ownerRoot = null,
        bool canHitOwner = false)
    {
        _velocity = velocity;
        _muzzleSpeed = velocity.magnitude;
        _weaponType = weaponType;
        _ownerRoot = ownerRoot;
        _canHitOwner = canHitOwner;
        _spawnTime = Time.time;
        _traveledDistance = 0f;
        _maxTravelDistance = weaponType == ProjectileWeaponType.CyborgLaser ? 120f : -1f;
        EnsureVisual();

        LiveBullets.Add(this);
    }

    public static void DestroyAll()
    {
        for (int i = LiveBullets.Count - 1; i >= 0; i--)
        {
            if (LiveBullets[i] != null)
            {
                Destroy(LiveBullets[i].gameObject);
            }
        }

        LiveBullets.Clear();
    }

    void OnDestroy()
    {
        LiveBullets.Remove(this);
    }

    void Update()
    {
        if (Time.time - _spawnTime >= MaxLifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.position.y < KillY)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        float travelDistance = _velocity.magnitude * Time.deltaTime;
        if (_weaponType == ProjectileWeaponType.CyborgLaser)
        {
            if (travelDistance > 0f)
            {
                _traveledDistance += travelDistance;
                if (_maxTravelDistance > 0f && _traveledDistance >= _maxTravelDistance)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            _velocity += Physics.gravity * Time.deltaTime;
            ProjectileDamage.ApplyAirDrag(ref _velocity, _weaponType, travelDistance);
        }

        Vector3 end = start + (_velocity * Time.deltaTime);
        ResolveHits(start, end);
        UpdateBulletVisualScale();
    }

    void ResolveHits(Vector3 start, Vector3 intendedEnd)
    {
        Vector3 segment = intendedEnd - start;
        float distance = segment.magnitude;
        if (distance <= 0.0001f)
        {
            return;
        }

        Vector3 direction = segment / distance;
        RaycastHit[] hits = Physics.RaycastAll(
            start, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            var hitZone = hit.collider.GetComponent<ShootingRangeHitZone>();
            if (hitZone != null && hitZone.dummy != null)
            {
                bool headshot = hitZone.zoneType == ShootingRangeHitZoneType.Head;
                if (TryResolveCharacterHit(
                    hitZone.dummy.gameObject,
                    hit.point,
                    _velocity.magnitude,
                    headshot,
                    () => hitZone.dummy.ApplyHit(
                        hitZone.zoneType, _velocity.magnitude, _muzzleSpeed, _weaponType)))
                {
                    return;
                }

                continue;
            }

            var controller = hit.collider.GetComponentInParent<ThirdPersonController>();
            if (controller != null)
            {
                if (ShouldIgnoreCharacter(controller.gameObject))
                {
                    continue;
                }

                if (TryResolveCharacterHit(
                    controller.gameObject,
                    hit.point,
                    _velocity.magnitude,
                    IsHeadshotHit(controller.transform, hit.point),
                    () => ApplyPlayerDamage(controller, hit.point, _velocity.magnitude)))
                {
                    return;
                }

                continue;
            }

            var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
            if (marker != null)
            {
                PenetrateBuildPiece();
                continue;
            }

            LandOnSurface(hit.point, hit.normal);
            return;
        }

        transform.position = intendedEnd;
        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
        }
    }

    bool ShouldIgnoreCharacter(GameObject characterRoot)
    {
        return !_canHitOwner && _ownerRoot != null && characterRoot == _ownerRoot;
    }

    bool TryResolveCharacterHit(
        GameObject characterRoot,
        Vector3 hitPoint,
        float impactSpeed,
        bool headshot,
        Func<bool> applyHit)
    {
        if (_penetratedCharacters.Contains(characterRoot))
        {
            return false;
        }

        if (impactSpeed < ProjectileDamage.PlayerBounceMinSpeed)
        {
            DestroyAt(hitPoint);
            return true;
        }

        float damage = ProjectileDamage.ComputeDamage(impactSpeed, _muzzleSpeed, _weaponType, headshot);
        if (damage > DamageEpsilon)
        {
            applyHit();
        }

        if (_weaponType != ProjectileWeaponType.SniperRifle)
        {
            DestroyAt(hitPoint);
            return true;
        }

        if (impactSpeed <= SniperPlayerPenetrationMinSpeed)
        {
            DestroyAt(hitPoint);
            return true;
        }

        _penetratedCharacters.Add(characterRoot);
        ApplySniperPenetrationPenalty();
        return false;
    }

    bool ApplyPlayerDamage(ThirdPersonController controller, Vector3 hitPoint, float impactSpeed)
    {
        bool headshot = IsHeadshotHit(controller.transform, hitPoint);
        float damage = ProjectileDamage.ComputeDamage(impactSpeed, _muzzleSpeed, _weaponType, headshot);
        if (damage <= DamageEpsilon)
        {
            return false;
        }

        var health = controller.GetComponent<PlayerHealth>();
        if (health == null)
        {
            return false;
        }

        health.ApplyDamage(damage, headshot);
        return true;
    }

    void ApplySniperPenetrationPenalty()
    {
        float speed = _velocity.magnitude * SniperPlayerSpeedRetention;
        if (speed <= 0.0001f)
        {
            _velocity = Vector3.zero;
            return;
        }

        Vector3 direction = _velocity / _velocity.magnitude;
        Vector3 reference = Mathf.Abs(direction.y) < 0.9f ? Vector3.up : Vector3.right;
        Vector3 axisA = Vector3.Cross(direction, reference).normalized;
        Vector3 axisB = Vector3.Cross(direction, axisA);
        float yaw = UnityEngine.Random.Range(-SniperAccuracyDeflectionDegrees, SniperAccuracyDeflectionDegrees) *
            Mathf.Deg2Rad;
        float pitch = UnityEngine.Random.Range(-SniperAccuracyDeflectionDegrees, SniperAccuracyDeflectionDegrees) *
            Mathf.Deg2Rad;
        Vector3 deflected = (direction + (axisA * Mathf.Tan(pitch)) + (axisB * Mathf.Tan(yaw))).normalized;
        _velocity = deflected * speed;
    }

    void PenetrateBuildPiece()
    {
        _velocity *= SurfaceImpactSpeedRetention;
    }

    void LandOnSurface(Vector3 contactPoint, Vector3 surfaceNormal)
    {
        transform.position = contactPoint + (surfaceNormal * Radius);
        Destroy(gameObject);
    }

    void DestroyAt(Vector3 contactPoint)
    {
        transform.position = contactPoint;
        Destroy(gameObject);
    }

    static bool IsHeadshotHit(Transform playerTransform, Vector3 hitPoint)
    {
        return playerTransform.InverseTransformPoint(hitPoint).y >= 1.35f;
    }

    void EnsureVisual()
    {
        if (transform.childCount > 0)
        {
            _bulletVisual = transform.GetChild(0);
            _renderer = _bulletVisual.GetComponent<Renderer>();
            return;
        }

        var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet Visual";
        bullet.transform.SetParent(transform, false);
        _bulletVisual = bullet.transform;
        _bulletVisual.localScale = Vector3.one * (Radius * 2f);
        _renderer = bullet.GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = BulletMaterial(_weaponType);
        Destroy(bullet.GetComponent<Collider>());
    }

    static Material BulletMaterial(ProjectileWeaponType weaponType)
    {
        if (weaponType == ProjectileWeaponType.CyborgLaser)
        {
            return LaserBulletMaterial();
        }

        if (_bulletMaterial != null)
        {
            return _bulletMaterial;
        }

        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        _bulletMaterial = new Material(shader)
        {
            name = "Projectile Bullet Material"
        };
        _bulletMaterial.color = new Color(0.12f, 0.12f, 0.13f, 1f);

        return _bulletMaterial;
    }

    static Material _laserBulletMaterial;

    static Material LaserBulletMaterial()
    {
        if (_laserBulletMaterial != null)
        {
            return _laserBulletMaterial;
        }

        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        _laserBulletMaterial = new Material(shader)
        {
            name = "Projectile Laser Material"
        };
        _laserBulletMaterial.color = new Color(0.95f, 0.12f, 0.1f, 1f);
        return _laserBulletMaterial;
    }

    void UpdateBulletVisualScale()
    {
        if (_bulletVisual == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float distance = Vector3.Distance(camera.transform.position, transform.position);
        float diameter = Radius * 2f;
        float scale = diameter * Mathf.Max(1f, distance / MinApparentSizeDistanceMeters);
        _bulletVisual.localScale = Vector3.one * scale;
    }
}
