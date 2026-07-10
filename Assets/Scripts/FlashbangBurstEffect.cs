using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Brief bright burst for flashbang detonations.
/// </summary>
public class FlashbangBurstEffect : MonoBehaviour
{
    public const float DurationSeconds = 0.35f;
    const float DiameterMeters = 8f;

    static readonly List<FlashbangBurstEffect> LiveEffects = new List<FlashbangBurstEffect>();

    float _elapsed;
    Material _material;
    Transform _burst;

    public static void Spawn(Vector3 center)
    {
        var effectObject = new GameObject("Flashbang Burst");
        effectObject.transform.position = center;
        var effect = effectObject.AddComponent<FlashbangBurstEffect>();
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
        layer.name = "Burst";
        layer.transform.SetParent(transform, false);
        Destroy(layer.GetComponent<Collider>());
        _burst = layer.transform;

        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        _material = new Material(shader)
        {
            color = new Color(1f, 0.98f, 0.92f, 1f)
        };
        layer.GetComponent<Renderer>().sharedMaterial = _material;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(_elapsed / DurationSeconds);
        float expand = 1f - Mathf.Pow(1f - normalized, 3f);
        float fade = 1f - normalized;
        _burst.localScale = Vector3.one * (DiameterMeters * expand);
        _material.color = new Color(1f, 0.98f, 0.92f, fade);
        _burst.GetComponent<Renderer>().sharedMaterial = _material;

        if (_elapsed >= DurationSeconds)
        {
            Destroy(gameObject);
        }
    }
}
