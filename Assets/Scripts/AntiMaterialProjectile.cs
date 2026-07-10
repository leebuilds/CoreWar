using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anti-material round: ballistic flight, sticks on impact, detonates after a fuse.
/// </summary>
public class AntiMaterialProjectile : MonoBehaviour
{
    const float Radius = 0.0275f;
    const float KillY = -12f;
    const float MaxLifetimeSeconds = 30f;
    const float FuseSeconds = 2f;
    const float BlastRadiusMeters = 10f;
    const float BuildDestroyRadiusMeters = 4.5f;
    const float MinEdgeDamage = 10f;
    const float MaxCenterDamage = 100f;
    const float BuildPenetrationSpeedRetention = 0.5f;
    const float MinApparentSizeDistanceMeters = 50f;
    const float GroundSnapRaycastHeight = 0.5f;
    const float GroundSnapRaycastDistance = 50f;

    static readonly List<AntiMaterialProjectile> LiveProjectiles = new List<AntiMaterialProjectile>();
    static Material _projectileMaterial;

    Vector3 _velocity;
    float _muzzleSpeed;
    float _spawnTime;
    bool _armed;
    float _fuseTimer;
    Transform _stickTarget;
    Vector3 _stickLocalOffset;
    Vector3 _stuckImpactWorldPoint;
    GameObject _ownerRoot;
    Renderer _renderer;
    Transform _projectileVisual;

    public void Initialize(Vector3 velocity, float muzzleSpeed, GameObject ownerRoot)
    {
        _velocity = velocity;
        _muzzleSpeed = muzzleSpeed;
        _ownerRoot = ownerRoot;
        _spawnTime = Time.time;
        EnsureVisual();
        LiveProjectiles.Add(this);
    }

    public static void DestroyAll()
    {
        for (int i = LiveProjectiles.Count - 1; i >= 0; i--)
        {
            if (LiveProjectiles[i] != null)
            {
                Destroy(LiveProjectiles[i].gameObject);
            }
        }

        LiveProjectiles.Clear();
    }

    void OnDestroy()
    {
        LiveProjectiles.Remove(this);
    }

    void Update()
    {
        if (Time.time - _spawnTime >= MaxLifetimeSeconds)
        {
            Destroy(gameObject);
            return;
        }

        if (_armed)
        {
            TickFuse();
            return;
        }

        if (transform.position.y < KillY)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        _velocity += Physics.gravity * Time.deltaTime;
        float travelDistance = _velocity.magnitude * Time.deltaTime;
        ProjectileDamage.ApplyAirDrag(ref _velocity, ProjectileWeaponType.AntiMaterialRifle, travelDistance);
        Vector3 end = start + (_velocity * Time.deltaTime);
        ResolveFlight(start, end);
        UpdateProjectileVisualScale();
    }

    void ResolveFlight(Vector3 start, Vector3 intendedEnd)
    {
        Vector3 segment = intendedEnd - start;
        float distance = segment.magnitude;
        if (distance <= 0.0001f)
        {
            return;
        }

        Vector3 direction = segment / distance;
        RaycastHit[] hits = Physics.RaycastAll(
            start, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            var c4Charge = hit.collider.GetComponentInParent<C4ChargeProjectile>();
            if (c4Charge != null)
            {
                float damage = ProjectileDamage.ComputeDamage(
                    _velocity.magnitude,
                    _muzzleSpeed,
                    ProjectileWeaponType.AntiMaterialRifle,
                    headshot: false);
                c4Charge.DetonateFromBullet(hit.point, damage, headshot: false);
                Destroy(gameObject);
                return;
            }

            var hitZone = hit.collider.GetComponent<ShootingRangeHitZone>();
            if (hitZone != null && hitZone.dummy != null)
            {
                bool headshot = hitZone.zoneType == ShootingRangeHitZoneType.Head;
                hitZone.dummy.ApplyHit(
                    hitZone.zoneType, _velocity.magnitude, _muzzleSpeed, ProjectileWeaponType.AntiMaterialRifle);
                StickTo(hitZone.dummy.transform, hit.point);
                return;
            }

            var controller = hit.collider.GetComponentInParent<ThirdPersonController>();
            if (controller != null && !ShouldIgnoreCharacter(controller.gameObject))
            {
                ApplyDirectPlayerHit(controller, hit.point);
                StickTo(controller.transform, hit.point);
                return;
            }

            var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
            if (marker != null)
            {
                _velocity *= BuildPenetrationSpeedRetention;
                continue;
            }

            StickTo(null, hit.point + (hit.normal * Radius));
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
        return _ownerRoot != null && characterRoot == _ownerRoot;
    }

    void ApplyDirectPlayerHit(ThirdPersonController controller, Vector3 hitPoint)
    {
        bool headshot = IsHeadshotHit(controller.transform, hitPoint);
        float damage = ProjectileDamage.ComputeDamage(
            _velocity.magnitude, _muzzleSpeed, ProjectileWeaponType.AntiMaterialRifle, headshot);
        if (damage <= 0.001f)
        {
            return;
        }

        controller.GetComponent<PlayerHealth>()?.ApplyDamage(damage, headshot);
    }

    static bool IsHeadshotHit(Transform playerTransform, Vector3 hitPoint)
    {
        return playerTransform.InverseTransformPoint(hitPoint).y >= 1.35f;
    }

    void StickTo(Transform target, Vector3 worldPoint)
    {
        _armed = true;
        _fuseTimer = FuseSeconds;
        _velocity = Vector3.zero;
        _stickTarget = target;
        _stuckImpactWorldPoint = worldPoint;
        if (target != null)
        {
            _stickLocalOffset = target.InverseTransformPoint(worldPoint);
            transform.position = worldPoint;
            SetProjectileVisualVisible(false);
        }
        else
        {
            transform.position = worldPoint;
        }
    }

    void TickFuse()
    {
        if (_stickTarget != null)
        {
            if (IsStickTargetLost())
            {
                DropToGroundFromImpact();
            }
            else
            {
                transform.position = _stickTarget.TransformPoint(_stickLocalOffset);
            }
        }

        _fuseTimer -= Time.deltaTime;
        if (_fuseTimer <= 0f)
        {
            Detonate(transform.position);
        }
    }

    bool IsStickTargetLost()
    {
        if (_stickTarget == null)
        {
            return false;
        }

        var dummy = _stickTarget.GetComponentInParent<ShootingRangeDummy>();
        if (dummy != null)
        {
            return dummy.IsDown;
        }

        var health = _stickTarget.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            return !health.IsAlive;
        }

        return !_stickTarget.gameObject.activeInHierarchy;
    }

    void DropToGroundFromImpact()
    {
        _stickTarget = null;
        Vector3 point = _stuckImpactWorldPoint;
        if (Physics.Raycast(
                point + (Vector3.up * GroundSnapRaycastHeight),
                Vector3.down,
                out RaycastHit hit,
                GroundSnapRaycastDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            point = hit.point + (hit.normal * Radius);
        }

        transform.position = point;
        SetProjectileVisualVisible(true);
    }

    void SetProjectileVisualVisible(bool visible)
    {
        if (_renderer != null)
        {
            _renderer.enabled = visible;
        }
    }

    void Detonate(Vector3 center)
    {
        ExplosionBlastUtility.Detonate(center, AntiMaterialExplosionProfile());
        Destroy(gameObject);
    }

    static ExplosionBlastUtility.Profile AntiMaterialExplosionProfile()
    {
        return new ExplosionBlastUtility.Profile
        {
            damageRadiusMeters = BlastRadiusMeters,
            buildDestroyRadiusMeters = BuildDestroyRadiusMeters,
            minEdgeDamage = MinEdgeDamage,
            maxCenterDamage = MaxCenterDamage,
            falloff = ExplosionBlastUtility.DamageFalloff.Exponential
        };
    }

    void EnsureVisual()
    {
        if (transform.childCount > 0)
        {
            _projectileVisual = transform.GetChild(0);
            _renderer = _projectileVisual.GetComponent<Renderer>();
            return;
        }

        var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Anti-Material Projectile Visual";
        projectile.transform.SetParent(transform, false);
        _projectileVisual = projectile.transform;
        _projectileVisual.localScale = Vector3.one * (Radius * 2f);
        _renderer = projectile.GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = ProjectileMaterial();
        Destroy(projectile.GetComponent<Collider>());
    }

    void UpdateProjectileVisualScale()
    {
        if (_projectileVisual == null)
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
        _projectileVisual.localScale = Vector3.one * scale;
    }

    static Material ProjectileMaterial()
    {
        if (_projectileMaterial != null)
        {
            return _projectileMaterial;
        }

        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        _projectileMaterial = new Material(shader)
        {
            name = "Anti-Material Projectile Material"
        };
        _projectileMaterial.color = new Color(0.18f, 0.16f, 0.14f, 1f);
        return _projectileMaterial;
    }
}
