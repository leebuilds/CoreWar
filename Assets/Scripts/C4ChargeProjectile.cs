using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kamikaze C4: slow thrown charge, sticky attachment, remote-delayed detonation.
/// </summary>
public class C4ChargeProjectile : MonoBehaviour
{
    const float KillY = -12f;
    const float MaxLifetimeSeconds = 90f;
    const float AttachArmSeconds = 2f;
    const float DamageRadiusMeters = 10f;
    const float BuildDestroyRadiusMeters = 8f;
    const float MinEdgeDamage = 5f;
    const float MaxCenterDamage = 130f;
    const float EntityDetonationDamageThreshold = 30f;
    const float SurfaceOffset = 0.035f;
    static readonly Vector3 ChargeVisualScale = new Vector3(0.56f, 0.42f, 0.14f);

    static readonly List<C4ChargeProjectile> LiveCharges = new List<C4ChargeProjectile>();
    static Material _chargeMaterial;
    static Material _strapMaterial;
    static Material _buttonMaterial;

    Vector3 _velocity;
    float _fallAcceleration;
    float _spawnTime;
    bool _attached;
    bool _detonationQueued;
    float _attachedTimer;
    float _detonationTimer;
    Transform _stickTarget;
    Vector3 _stickLocalOffset;
    Quaternion _stickLocalRotation;
    GameObject _ownerRoot;
    float _ownerStickGraceSeconds;
    Transform _visual;
    BoxCollider _hitbox;
    float _accumulatedBodyDamage;
    bool _isDetonating;

    public System.Action<C4ChargeProjectile> Destroyed;

    public bool IsAttached => _attached;
    public bool CanRemoteDetonate => _attached && _attachedTimer >= AttachArmSeconds && !_detonationQueued;
    public bool IsDetonating => _isDetonating;

    public GameObject GetBlastLineOfSightRoot()
    {
        return _stickTarget != null ? _stickTarget.gameObject : gameObject;
    }

    public void Initialize(
        Vector3 velocity,
        float fallAcceleration,
        GameObject ownerRoot,
        float ownerStickGraceSeconds = 1f)
    {
        _velocity = velocity;
        _fallAcceleration = Mathf.Max(0f, fallAcceleration);
        _ownerRoot = ownerRoot;
        _ownerStickGraceSeconds = Mathf.Max(0f, ownerStickGraceSeconds);
        _spawnTime = Time.time;
        EnsureVisual();
        EnsureHitbox();
        LiveCharges.Add(this);
    }

    public static void DestroyAll()
    {
        for (int i = LiveCharges.Count - 1; i >= 0; i--)
        {
            if (LiveCharges[i] != null)
            {
                Destroy(LiveCharges[i].gameObject);
            }
        }

        LiveCharges.Clear();
    }

    public void QueueDetonation(float delaySeconds)
    {
        if (!CanRemoteDetonate)
        {
            return;
        }

        _detonationQueued = true;
        _detonationTimer = Mathf.Max(0f, delaySeconds);
    }

    void OnDestroy()
    {
        LiveCharges.Remove(this);
        Destroyed?.Invoke(this);
    }

    void Update()
    {
        if (Time.time - _spawnTime >= MaxLifetimeSeconds || transform.position.y < KillY)
        {
            Destroy(gameObject);
            return;
        }

        if (_attached)
        {
            TickAttached();
            return;
        }

        Vector3 start = transform.position;
        _velocity += Vector3.down * (_fallAcceleration * Time.deltaTime);
        Vector3 end = start + (_velocity * Time.deltaTime);
        ResolveFlight(start, end);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_attached || other == null || other.transform.IsChildOf(transform))
        {
            return;
        }

        var controller = other.GetComponentInParent<ThirdPersonController>();
        if (controller != null)
        {
            if (ShouldIgnoreOwner(controller.gameObject) || !CanStickToController(controller))
            {
                return;
            }

            StickTo(controller.transform, ClosestPointTo(other), ContactNormalFrom(other));
            return;
        }

        var dummy = other.GetComponentInParent<ShootingRangeDummy>();
        if (dummy != null)
        {
            if (dummy.IsDown)
            {
                return;
            }

            StickTo(dummy.transform, ClosestPointTo(other), ContactNormalFrom(other));
        }
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
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            var controller = hit.collider.GetComponentInParent<ThirdPersonController>();
            if (controller != null)
            {
                if (ShouldIgnoreOwner(controller.gameObject))
                {
                    continue;
                }

                if (!CanStickToController(controller))
                {
                    continue;
                }

                StickTo(controller.transform, hit.point, hit.normal);
                return;
            }

            var dummy = hit.collider.GetComponentInParent<ShootingRangeDummy>();
            if (dummy != null)
            {
                if (dummy.IsDown)
                {
                    continue;
                }

                StickTo(dummy.transform, hit.point, hit.normal);
                return;
            }

            StickTo(null, hit.point, hit.normal);
            return;
        }

        transform.position = intendedEnd;
        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
        }
    }

    bool ShouldIgnoreOwner(GameObject characterRoot)
    {
        return _ownerRoot != null &&
            characterRoot == _ownerRoot &&
            Time.time - _spawnTime < _ownerStickGraceSeconds;
    }

    static bool CanStickToController(ThirdPersonController controller)
    {
        var health = controller.GetComponent<PlayerHealth>();
        return health == null || health.IsAlive;
    }

    void StickTo(Transform target, Vector3 point, Vector3 normal)
    {
        _attached = true;
        _attachedTimer = 0f;
        _velocity = Vector3.zero;

        Vector3 surfaceNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        transform.position = point + (surfaceNormal * SurfaceOffset);
        transform.rotation = Quaternion.LookRotation(surfaceNormal, Vector3.up);

        _stickTarget = target;
        if (_stickTarget != null)
        {
            _stickLocalOffset = _stickTarget.InverseTransformPoint(transform.position);
            _stickLocalRotation = Quaternion.Inverse(_stickTarget.rotation) * transform.rotation;
        }
    }

    void TickAttached()
    {
        _attachedTimer += Time.deltaTime;
        if (_stickTarget != null)
        {
            if (IsStickTargetLost())
            {
                DropFromTarget();
            }
            else
            {
                transform.position = _stickTarget.TransformPoint(_stickLocalOffset);
                transform.rotation = _stickTarget.rotation * _stickLocalRotation;
            }
        }

        if (!_detonationQueued)
        {
            return;
        }

        _detonationTimer -= Time.deltaTime;
        if (_detonationTimer <= 0f)
        {
            Detonate(transform.position);
        }
    }

    public void ApplyEntityDamage(float damage, bool headshot, Vector3 hitPoint)
    {
        if (headshot || damage <= 0f)
        {
            return;
        }

        _accumulatedBodyDamage += damage;
        if (_accumulatedBodyDamage >= EntityDetonationDamageThreshold)
        {
            Detonate(hitPoint);
        }
    }

    public void DetonateFromBullet(Vector3 hitPoint, float damage, bool headshot)
    {
        ApplyEntityDamage(damage, headshot, hitPoint);
    }

    public static void ApplyBlastDamage(
        Vector3 center,
        float radiusMeters,
        System.Func<float, float> damageAtDistance,
        bool requireLineOfSight = false,
        System.Func<Vector3, Vector3, GameObject, bool> lineOfSightCheck = null)
    {
        for (int i = LiveCharges.Count - 1; i >= 0; i--)
        {
            C4ChargeProjectile charge = LiveCharges[i];
            if (charge == null || charge.IsDetonating)
            {
                continue;
            }

            Vector3 chargePoint = charge.transform.position;
            float distance = Vector3.Distance(center, chargePoint);
            if (distance >= radiusMeters)
            {
                continue;
            }

            float damage = damageAtDistance(distance);
            if (damage <= 0f)
            {
                continue;
            }

            if (requireLineOfSight &&
                lineOfSightCheck != null &&
                !lineOfSightCheck(center, chargePoint, charge.GetBlastLineOfSightRoot()))
            {
                continue;
            }

            charge.ApplyEntityDamage(damage, headshot: false, chargePoint);
        }
    }

    public static void ApplyChargesInRange(
        Vector3 origin,
        float rangeMeters,
        float damage,
        System.Func<Vector3, bool> isValidTarget)
    {
        if (damage <= 0f || isValidTarget == null)
        {
            return;
        }

        for (int i = LiveCharges.Count - 1; i >= 0; i--)
        {
            C4ChargeProjectile charge = LiveCharges[i];
            if (charge == null || charge.IsDetonating)
            {
                continue;
            }

            Vector3 chargePoint = charge.transform.position;
            if (!isValidTarget(chargePoint))
            {
                continue;
            }

            charge.ApplyEntityDamage(damage, headshot: false, chargePoint);
        }
    }

    void Detonate(Vector3 center)
    {
        if (_isDetonating)
        {
            return;
        }

        _isDetonating = true;
        _detonationQueued = false;
        _detonationTimer = 0f;

        ExplosionBlastUtility.Detonate(center, C4ExplosionProfile());
        Destroy(gameObject);
    }

    static ExplosionBlastUtility.Profile C4ExplosionProfile()
    {
        return new ExplosionBlastUtility.Profile
        {
            damageRadiusMeters = DamageRadiusMeters,
            buildDestroyRadiusMeters = BuildDestroyRadiusMeters,
            minEdgeDamage = MinEdgeDamage,
            maxCenterDamage = MaxCenterDamage,
            falloff = ExplosionBlastUtility.DamageFalloff.Linear
        };
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

    void DropFromTarget()
    {
        _stickTarget = null;
        _attached = false;
        _detonationQueued = false;
        _detonationTimer = 0f;
        _velocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }

    Vector3 ClosestPointTo(Collider other)
    {
        return other.ClosestPoint(transform.position);
    }

    Vector3 ContactNormalFrom(Collider other)
    {
        Vector3 normal = transform.position - other.ClosestPoint(transform.position);
        if (normal.sqrMagnitude <= 0.0001f)
        {
            normal = _velocity.sqrMagnitude > 0.0001f ? -_velocity.normalized : Vector3.up;
        }

        return normal.normalized;
    }

    void EnsureVisual()
    {
        if (_visual != null)
        {
            return;
        }

        var root = new GameObject("C4 Charge Visual");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        _visual = root.transform;

        CreateVisualCube("C4 Brick", Vector3.zero, ChargeVisualScale, ChargeMaterial());
        CreateVisualCube("C4 Strap", new Vector3(0f, 0.03f, 0f), new Vector3(0.64f, 0.08f, 0.17f), StrapMaterial());
        CreateVisualCube("C4 Button", new Vector3(-0.16f, 0.24f, 0f), new Vector3(0.16f, 0.08f, 0.16f), ButtonMaterial());
    }

    void EnsureHitbox()
    {
        if (_hitbox != null)
        {
            return;
        }

        var body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
        _hitbox = gameObject.AddComponent<BoxCollider>();
        _hitbox.isTrigger = true;
        _hitbox.size = ChargeVisualScale;
    }

    void CreateVisualCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(_visual, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        Destroy(cube.GetComponent<Collider>());
    }

    static Material ChargeMaterial()
    {
        if (_chargeMaterial != null)
        {
            return _chargeMaterial;
        }

        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        _chargeMaterial = new Material(shader)
        {
            name = "C4 Charge Material"
        };
        _chargeMaterial.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        return _chargeMaterial;
    }

    static Material StrapMaterial()
    {
        if (_strapMaterial != null)
        {
            return _strapMaterial;
        }

        _strapMaterial = CreateMaterial("C4 Strap Material", new Color(0.58f, 0.58f, 0.6f, 1f));
        return _strapMaterial;
    }

    static Material ButtonMaterial()
    {
        if (_buttonMaterial != null)
        {
            return _buttonMaterial;
        }

        _buttonMaterial = CreateMaterial("C4 Button Material", new Color(0.92f, 0.12f, 0.1f, 1f));
        return _buttonMaterial;
    }

    static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return new Material(shader)
        {
            name = materialName,
            color = color
        };
    }
}
