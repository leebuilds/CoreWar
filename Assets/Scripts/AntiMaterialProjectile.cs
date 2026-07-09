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
    const float ExplosionBlindnessMultiplier = 2f;
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
            start, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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
            return health.CurrentHealth <= 0f;
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
        ApplyExplosionDamage(center);
        DestroyBuildPiecesNear(center);
        AntiMaterialExplosionEffect.Spawn(center);
        Destroy(gameObject);
    }

    static void ApplyExplosionDamage(Vector3 center)
    {
        var damagedRoots = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(
            center, BlastRadiusMeters, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
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

                float distance = Vector3.Distance(center, dummy.transform.position);
                float damage = ExplosionDamageAtDistance(distance);
                if (damage > 0f)
                {
                    dummy.ApplyDirectDamage(damage, false);
                }

                continue;
            }

            var controller = hit.GetComponentInParent<ThirdPersonController>();
            if (controller == null)
            {
                continue;
            }

            if (!damagedRoots.Add(controller.gameObject))
            {
                continue;
            }

            float playerDistance = Vector3.Distance(center, controller.transform.position);
            float playerDamage = ExplosionDamageAtDistance(playerDistance);
            bool inFire = playerDistance <= AntiMaterialExplosionEffect.FireRadiusMeters;
            var health = controller.GetComponent<PlayerHealth>();
            if (health == null)
            {
                continue;
            }

            float blindDuration = 0f;
            if (playerDamage > 0f)
            {
                blindDuration = health.ApplyDamageWithoutBlindness(
                    playerDamage, false, ExplosionBlindnessMultiplier);
            }

            if (controller == ThirdPersonController.Local && (inFire || blindDuration > 0f))
            {
                PlayerBulletHitFlash.Instance?.BlindFromExplosionFire(blindDuration, inFire);
            }
        }
    }

    static float ExplosionDamageAtDistance(float distanceMeters)
    {
        if (distanceMeters >= BlastRadiusMeters)
        {
            return 0f;
        }

        float closeness = 1f - (distanceMeters / BlastRadiusMeters);
        return MinEdgeDamage * Mathf.Pow(MaxCenterDamage / MinEdgeDamage, closeness);
    }

    static void DestroyBuildPiecesNear(Vector3 center)
    {
        var world = Object.FindFirstObjectByType<VoxelLightingWorld>();
        if (world == null)
        {
            return;
        }

        var removed = new HashSet<PlayerBuiltVoxel>();
        Collider[] hits = Physics.OverlapSphere(
            center, BuildDestroyRadiusMeters, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var marker = hits[i].GetComponentInParent<PlayerBuiltVoxel>();
            if (marker == null || !removed.Add(marker))
            {
                continue;
            }

            world.TryRemovePlayerBuiltObject(marker);
        }
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
