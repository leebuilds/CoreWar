using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared rigidbody throw physics for frag and flashbang grenades.
/// </summary>
public abstract class ThrownGrenadeProjectile : MonoBehaviour
{
    const float KillY = -12f;
    const float MaxLifetimeSeconds = 30f;
    const float ThrowSpeedMetersPerSecond = 30f;
    const float GravityAcceleration = 9.81f;
    const float StopSpeedThreshold = 0.35f;
    protected const float Radius = 0.09f;
    const float MassKg = 0.42f;

    static readonly List<ThrownGrenadeProjectile> LiveGrenades = new List<ThrownGrenadeProjectile>();
    static PhysicsMaterial _grenadePhysicsMaterial;

    Rigidbody _rb;
    float _fuseTimer;
    float _spawnTime;
    Transform _visual;

    protected abstract GrenadeType GrenadeType { get; }
    protected abstract Color BodyColor { get; }
    protected abstract float BodyMetallic { get; }
    protected abstract float BodyGlossiness { get; }
    protected abstract string VisualObjectName { get; }
    protected abstract void DetonateAt(Vector3 center);

    public static ThrownGrenadeProjectile Spawn(
        GrenadeType grenadeType,
        Vector3 position,
        Vector3 direction,
        float fuseSeconds)
    {
        var grenade = new GameObject(grenadeType == GrenadeType.Flashbang ? "Flashbang" : "Frag Grenade");
        grenade.transform.position = position;
        ThrownGrenadeProjectile projectile = grenadeType switch
        {
            GrenadeType.Flashbang => grenade.AddComponent<FlashbangGrenadeProjectile>(),
            _ => grenade.AddComponent<FragGrenadeProjectile>()
        };
        projectile.Initialize(direction, fuseSeconds);
        LiveGrenades.Add(projectile);
        return projectile;
    }

    public static void DestroyAll()
    {
        for (int i = LiveGrenades.Count - 1; i >= 0; i--)
        {
            if (LiveGrenades[i] != null)
            {
                Destroy(LiveGrenades[i].gameObject);
            }
        }

        LiveGrenades.Clear();
    }

    void Initialize(Vector3 direction, float fuseSeconds)
    {
        _fuseTimer = Mathf.Max(0f, fuseSeconds);
        _spawnTime = Time.time;
        EnsurePhysics();
        EnsureVisual();

        Vector3 throwDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.forward;
        _rb.linearVelocity = throwDirection * ThrowSpeedMetersPerSecond;
        Vector3 spinAxis = Vector3.Cross(Vector3.up, throwDirection);
        if (spinAxis.sqrMagnitude <= 0.0001f)
        {
            spinAxis = Vector3.right;
        }

        _rb.angularVelocity = spinAxis.normalized * ((ThrowSpeedMetersPerSecond / Radius) * 0.18f);
    }

    void OnDestroy()
    {
        LiveGrenades.Remove(this);
    }

    void Update()
    {
        if (_fuseTimer <= 0f)
        {
            DetonateAt(transform.position);
            Destroy(gameObject);
            return;
        }

        if (Time.time - _spawnTime >= MaxLifetimeSeconds || transform.position.y < KillY)
        {
            Destroy(gameObject);
            return;
        }

        _fuseTimer = Mathf.Max(0f, _fuseTimer - Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (_rb == null)
        {
            return;
        }

        _rb.linearVelocity += Vector3.down * (GravityAcceleration * Time.fixedDeltaTime);

        bool onGround = IsOnGround();
        if (onGround)
        {
            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                horizontal *= 0.94f;
                _rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
            }

            _rb.angularVelocity *= 0.82f;
        }

        if (IsNearlyStopped(onGround))
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    bool IsOnGround()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            Radius + 0.08f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    bool IsNearlyStopped(bool onGround)
    {
        if (_rb == null || !onGround || _rb.linearVelocity.magnitude > StopSpeedThreshold)
        {
            return false;
        }

        return _rb.angularVelocity.magnitude <= StopSpeedThreshold * 2f;
    }

    void EnsurePhysics()
    {
        _rb = gameObject.AddComponent<Rigidbody>();
        _rb.mass = MassKg;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 1.1f;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = Radius;
        collider.material = GrenadePhysicsMaterial();
    }

    void EnsureVisual()
    {
        if (_visual != null)
        {
            return;
        }

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = VisualObjectName;
        sphere.transform.SetParent(transform, false);
        sphere.transform.localScale = Vector3.one * (Radius * 2f);
        sphere.GetComponent<Renderer>().sharedMaterial = CreateBodyMaterial();
        Destroy(sphere.GetComponent<Collider>());
        _visual = sphere.transform;
    }

    Material CreateBodyMaterial()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        var material = new Material(shader)
        {
            name = $"{GrenadeType} Projectile Material",
            color = BodyColor
        };

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", BodyMetallic);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", BodyGlossiness);
        }
        else if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", BodyGlossiness);
        }

        return material;
    }

    static PhysicsMaterial GrenadePhysicsMaterial()
    {
        if (_grenadePhysicsMaterial != null)
        {
            return _grenadePhysicsMaterial;
        }

        _grenadePhysicsMaterial = new PhysicsMaterial("Thrown Grenade")
        {
            dynamicFriction = 0.92f,
            staticFriction = 0.96f,
            bounciness = 0.18f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        return _grenadePhysicsMaterial;
    }
}
