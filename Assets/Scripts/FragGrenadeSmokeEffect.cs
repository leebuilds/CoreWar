using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Short gray smoke puff for frag grenade detonations.
/// </summary>
public class FragGrenadeSmokeEffect : MonoBehaviour
{
    public const float DurationSeconds = 0.5f;
    const float DiameterMeters = 5f;

    static readonly List<FragGrenadeSmokeEffect> LiveEffects = new List<FragGrenadeSmokeEffect>();

    float _elapsed;
    Material _material;
    Transform _smoke;

    public static void Spawn(Vector3 center)
    {
        var effectObject = new GameObject("Frag Grenade Smoke");
        effectObject.transform.position = center;
        var effect = effectObject.AddComponent<FragGrenadeSmokeEffect>();
        LiveEffects.Add(effect);
    }

    public static void DestroyAll()
    {
        for (int i = LiveEffects.Count - 1; i >= 0; i--)
        {
            if (LiveEffects[i] != null)
            {
                Destroy(LiveEffects[i].gameObject);
            }
        }

        LiveEffects.Clear();
    }

    void OnDestroy()
    {
        LiveEffects.Remove(this);
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    void Awake()
    {
        var layer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        layer.name = "Smoke";
        layer.transform.SetParent(transform, false);
        Destroy(layer.GetComponent<Collider>());
        _smoke = layer.transform;

        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        _material = new Material(shader)
        {
            color = new Color(0.62f, 0.62f, 0.64f, 0.55f)
        };
        layer.GetComponent<Renderer>().sharedMaterial = _material;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(_elapsed / DurationSeconds);
        float expand = 1f - Mathf.Pow(1f - normalized, 2f);
        float fade = 1f - normalized;
        _smoke.localScale = Vector3.one * (DiameterMeters * expand);
        _material.color = new Color(0.62f, 0.62f, 0.64f, 0.55f * fade);

        if (_elapsed >= DurationSeconds)
        {
            Destroy(gameObject);
        }
    }
}
