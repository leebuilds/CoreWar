using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-speed ball projectile. Flies via raycast integration (no tunneling at
/// muzzle speed), penetrates player-built pieces with thickness-based speed
/// loss, and converts to a real Rigidbody sphere on first map impact so it
/// bounces and rolls until friction stops it.
/// </summary>
public class ProjectileBullet : MonoBehaviour
{
    public const int MaxLiveBullets = 35;

    const float Radius = 0.0275f;
    const float VisibilityRange = 50f;
    const float KillY = -12f;

    // Hits that deal no damage bounce off players and dummies.
    const float DamageEpsilon = 0.001f;

    // Speed retained after striking a floor, map surface, or object.
    const float SurfaceImpactSpeedRetention = 0.5f;

    const float Bounciness = 0.72f;
    const float TangentRetentionMapImpact = 1f;
    const float TangentRetentionCharacterBounce = 1f;
    const float SurfaceFriction = 1.6f;

    static readonly List<ProjectileBullet> LiveBullets = new List<ProjectileBullet>();
    static Material _bulletMaterial;
    static PhysicsMaterial _bulletPhysicsMaterial;

    Vector3 _velocity;
    float _muzzleSpeed;
    ProjectileWeaponType _weaponType;
    bool _physicsPhase;
    Rigidbody _rb;
    Renderer _renderer;
    Camera _camera;

    public void Initialize(Vector3 velocity, ProjectileWeaponType weaponType)
    {
        _velocity = velocity;
        _muzzleSpeed = velocity.magnitude;
        _weaponType = weaponType;
        EnsureVisual();

        LiveBullets.Add(this);
        while (LiveBullets.Count > MaxLiveBullets)
        {
            var oldest = LiveBullets[0];
            LiveBullets.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest.gameObject);
            }
        }
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
        if (transform.position.y < KillY)
        {
            Destroy(gameObject);
            return;
        }

        UpdateVisibility();

        if (_physicsPhase)
        {
            return;
        }

        Vector3 start = transform.position;
        _velocity += Physics.gravity * Time.deltaTime;
        float travelDistance = _velocity.magnitude * Time.deltaTime;
        ProjectileDamage.ApplyAirDrag(ref _velocity, _weaponType, travelDistance);
        Vector3 end = start + (_velocity * Time.deltaTime);
        ResolveHits(start, end);
    }

    void UpdateVisibility()
    {
        if (_renderer == null)
        {
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                return;
            }
        }

        float sqrDistance = (transform.position - _camera.transform.position).sqrMagnitude;
        bool visible = sqrDistance <= VisibilityRange * VisibilityRange;
        if (_renderer.enabled != visible)
        {
            _renderer.enabled = visible;
        }
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
        RaycastHit[] hits = Physics.RaycastAll(start, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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
                if (TryAbsorbCharacterHit(_velocity.magnitude, headshot, () =>
                    hitZone.dummy.ApplyHit(hitZone.zoneType, _velocity.magnitude, _muzzleSpeed, _weaponType)))
                {
                    Destroy(gameObject);
                    return;
                }

                BounceOffSurface(hit.normal, hit.point);
                return;
            }

            if (TryResolvePlayerHit(hit.collider, hit.point, hit.normal, _velocity.magnitude))
            {
                return;
            }

            var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
            if (marker != null)
            {
                PenetrateBuildPiece();
                continue;
            }

            BeginPhysicsPhase(hit);
            return;
        }

        transform.position = intendedEnd;
        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
        }
    }

    void PenetrateBuildPiece()
    {
        ApplySurfaceImpactSpeedLoss();
    }

    void ApplySurfaceImpactSpeedLoss()
    {
        _velocity *= SurfaceImpactSpeedRetention;
    }

    void BeginPhysicsPhase(RaycastHit hit)
    {
        _physicsPhase = true;
        transform.position = hit.point + (hit.normal * Radius);

        // Manual first bounce keeps the fast impact stable; PhysX handles the rest.
        Vector3 normalVelocity = Vector3.Project(_velocity, hit.normal);
        Vector3 tangentVelocity = _velocity - normalVelocity;
        _velocity = (tangentVelocity * TangentRetentionMapImpact) - (normalVelocity * Bounciness);
        ApplySurfaceImpactSpeedLoss();

        var sphere = gameObject.AddComponent<SphereCollider>();
        sphere.radius = Radius;
        sphere.material = BulletPhysicsMaterial();

        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = 0.01f;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.maxAngularVelocity = 150f;
        // PhysX has no rolling resistance; damping stands in for it so the
        // ball rolls out and settles instead of rolling forever.
        _rb.linearDamping = 0.9f;
        _rb.angularDamping = 1.6f;
        _rb.linearVelocity = _velocity;
        _rb.angularVelocity = Vector3.Cross(hit.normal, _velocity) / Radius;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!_physicsPhase)
        {
            return;
        }

        var hitZone = collision.collider.GetComponent<ShootingRangeHitZone>();
        if (hitZone != null && hitZone.dummy != null)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            bool headshot = hitZone.zoneType == ShootingRangeHitZoneType.Head;
            if (TryAbsorbCharacterHit(impactSpeed, headshot, () =>
                hitZone.dummy.ApplyHit(hitZone.zoneType, impactSpeed, _muzzleSpeed, _weaponType)))
            {
                Destroy(gameObject);
            }
            else
            {
                var contact = collision.GetContact(0);
                BounceOffSurface(contact.normal, contact.point);
            }

            return;
        }

        if (IsPlayerCollider(collision.collider))
        {
            var contact = collision.GetContact(0);
            TryResolvePlayerHit(collision.collider, contact.point, contact.normal, collision.relativeVelocity.magnitude);
            return;
        }

        _velocity = _rb.linearVelocity;
        ApplySurfaceImpactSpeedLoss();
        _rb.linearVelocity = _velocity;
    }

    bool TryAbsorbCharacterHit(float impactSpeed, bool headshot, System.Func<bool> applyHit)
    {
        float damage = ProjectileDamage.ComputeDamage(impactSpeed, _muzzleSpeed, _weaponType, headshot);
        if (damage <= DamageEpsilon)
        {
            return false;
        }

        return applyHit();
    }

    bool TryResolvePlayerHit(Collider collider, Vector3 hitPoint, Vector3 surfaceNormal, float impactSpeed)
    {
        var controller = collider.GetComponentInParent<ThirdPersonController>();
        if (controller == null)
        {
            return false;
        }

        if (impactSpeed < ProjectileDamage.PlayerBounceMinSpeed)
        {
            BounceOffSurface(surfaceNormal, hitPoint);
            return true;
        }

        if (impactSpeed > 0f)
        {
            bool headshot = IsHeadshotHit(controller.transform, hitPoint);
            float damage = ProjectileDamage.ComputeDamage(impactSpeed, _muzzleSpeed, _weaponType, headshot);
            var health = controller.GetComponent<PlayerHealth>();
            if (damage > DamageEpsilon && health != null)
            {
                health.ApplyDamage(damage, headshot);
                Destroy(gameObject);
                return true;
            }
        }

        if (_physicsPhase)
        {
            BounceOffSurface(surfaceNormal, hitPoint);
            return true;
        }

        BounceOffSurface(surfaceNormal, hitPoint);
        return true;
    }

    static bool IsHeadshotHit(Transform playerTransform, Vector3 hitPoint)
    {
        return playerTransform.InverseTransformPoint(hitPoint).y >= 1.35f;
    }

    void BounceOffSurface(Vector3 surfaceNormal, Vector3 contactPoint)
    {
        if (_physicsPhase && _rb != null)
        {
            _velocity = _rb.linearVelocity;
        }

        transform.position = contactPoint + (surfaceNormal * Radius);
        Vector3 normalVelocity = Vector3.Project(_velocity, surfaceNormal);
        Vector3 tangentVelocity = _velocity - normalVelocity;
        _velocity = (tangentVelocity * TangentRetentionCharacterBounce) - (normalVelocity * Bounciness);
        ApplySurfaceImpactSpeedLoss();

        if (_physicsPhase && _rb != null)
        {
            _rb.linearVelocity = _velocity;
        }
    }

    static bool IsPlayerCollider(Collider collider)
    {
        return collider != null && collider.GetComponentInParent<ThirdPersonController>() != null;
    }

    void EnsureVisual()
    {
        if (transform.childCount > 0)
        {
            return;
        }

        var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Bullet Visual";
        bullet.transform.SetParent(transform, false);
        bullet.transform.localScale = Vector3.one * (Radius * 2f);
        _renderer = bullet.GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = BulletMaterial();
        Destroy(bullet.GetComponent<Collider>());
    }

    static PhysicsMaterial BulletPhysicsMaterial()
    {
        if (_bulletPhysicsMaterial == null)
        {
            // Maximum combine wins over the slippery floor material (Minimum),
            // so the ball gets real friction on surfaces the player slides on.
            _bulletPhysicsMaterial = new PhysicsMaterial("Bullet Ball Grip")
            {
                dynamicFriction = SurfaceFriction,
                staticFriction = SurfaceFriction + 0.08f,
                bounciness = Bounciness,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };
        }

        return _bulletPhysicsMaterial;
    }

    static Material BulletMaterial()
    {
        if (_bulletMaterial == null)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            _bulletMaterial = new Material(shader)
            {
                name = "Projectile Bullet Material",
                mainTexture = CreateSpinTexture()
            };
        }

        return _bulletMaterial;
    }

    /// <summary>
    /// Two-tone texture so the ball visibly spins while rolling.
    /// </summary>
    static Texture2D CreateSpinTexture()
    {
        const int size = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        var dark = new Color(0.08f, 0.08f, 0.08f);
        var light = new Color(0.32f, 0.32f, 0.34f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool lightCell = ((x / 2) + (y / 2)) % 2 == 0;
                texture.SetPixel(x, y, lightCell ? light : dark);
            }
        }

        texture.Apply();
        return texture;
    }
}
