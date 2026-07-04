using System;
using UnityEngine;

public class ProjectileBullet : MonoBehaviour
{
    const float DefaultHoleLifetime = 14f;

    static Material _bulletMaterial;
    static Material _holeMaterial;

    Vector3 _velocity;
    float _gravity;
    float _remainingLifetime;
    float _landedLifetime;
    bool _landed;

    public void Initialize(Vector3 velocity, float gravity, float lifetime, float landedLifetime)
    {
        _velocity = velocity;
        _gravity = gravity;
        _remainingLifetime = lifetime;
        _landedLifetime = landedLifetime;
        EnsureVisual();
    }

    void Update()
    {
        if (_landed)
        {
            _landedLifetime -= Time.deltaTime;
            if (_landedLifetime <= 0f)
            {
                Destroy(gameObject);
            }
            return;
        }

        _remainingLifetime -= Time.deltaTime;
        if (_remainingLifetime <= 0f || transform.position.y < -12f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        _velocity += Vector3.down * (_gravity * Time.deltaTime);
        Vector3 displacement = _velocity * Time.deltaTime;
        Vector3 end = start + displacement;
        ResolveHits(start, end);
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
            var marker = hit.collider.GetComponentInParent<PlayerBuiltVoxel>();
            if (marker != null && marker.IsPanelPiece)
            {
                CreateBulletHole(hit);
                DegradeThroughBuildPiece();
                continue;
            }

            transform.position = hit.point;
            _velocity = Vector3.zero;
            _landed = true;
            return;
        }

        transform.position = intendedEnd;
        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
        }
    }

    void DegradeThroughBuildPiece()
    {
        const float velocityRetention = 0.88f;
        _velocity *= velocityRetention;
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
        bullet.transform.localScale = Vector3.one * 0.055f;
        bullet.GetComponent<MeshRenderer>().sharedMaterial = BulletMaterial();
        Destroy(bullet.GetComponent<Collider>());
    }

    static void CreateBulletHole(RaycastHit hit)
    {
        var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hole.name = "Bullet Hole";
        hole.transform.position = hit.point + (hit.normal * 0.006f);
        hole.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        hole.transform.localScale = new Vector3(0.07f, 0.003f, 0.07f);
        hole.GetComponent<MeshRenderer>().sharedMaterial = HoleMaterial();
        Destroy(hole.GetComponent<Collider>());

        if (hit.collider.transform != null)
        {
            hole.transform.SetParent(hit.collider.transform, true);
        }

        Destroy(hole, DefaultHoleLifetime);
    }

    static Material BulletMaterial()
    {
        if (_bulletMaterial == null)
        {
            _bulletMaterial = CreateMaterial("Projectile Bullet Material", new Color(0.08f, 0.08f, 0.08f, 1f));
        }
        return _bulletMaterial;
    }

    static Material HoleMaterial()
    {
        if (_holeMaterial == null)
        {
            _holeMaterial = CreateMaterial("Bullet Hole Material", new Color(0.015f, 0.015f, 0.015f, 1f));
        }
        return _holeMaterial;
    }

    static Material CreateMaterial(string materialName, Color color)
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
}
