using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Short-lived fireball for frag grenade detonations.
/// </summary>
public class FragGrenadeSmokeEffect : MonoBehaviour
{
    public const float DurationSeconds = 0.5f;
    const float DiameterMeters = 5f;
    const float ExpandSeconds = 0.1f;

    static readonly List<FragGrenadeSmokeEffect> LiveEffects = new List<FragGrenadeSmokeEffect>();

    float _elapsed;
    Material _outerMaterial;
    Material _midMaterial;
    Material _coreMaterial;
    Transform _outer;
    Transform _mid;
    Transform _core;

    public static void Spawn(Vector3 center)
    {
        var effectObject = new GameObject("Frag Grenade Explosion");
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
        DestroyMaterial(_outerMaterial);
        DestroyMaterial(_midMaterial);
        DestroyMaterial(_coreMaterial);
    }

    void Awake()
    {
        _outer = CreateFireLayer("Outer Fire", new Color(1f, 0.42f, 0.08f, 1f), out _outerMaterial);
        _mid = CreateFireLayer("Mid Fire", new Color(1f, 0.72f, 0.16f, 1f), out _midMaterial);
        _core = CreateFireLayer("Core Fire", new Color(1f, 0.95f, 0.72f, 1f), out _coreMaterial);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(_elapsed / DurationSeconds);
        float expand = Mathf.Clamp01(_elapsed / ExpandSeconds);
        float expandCurve = 1f - Mathf.Pow(1f - expand, 3f);
        float flicker = 1f + (Mathf.Sin(_elapsed * 38f) * 0.05f * (1f - normalized));
        float fade = 1f - Mathf.Pow(normalized, 1.35f);
        float endShrink = normalized > 0.72f
            ? 1f - Mathf.InverseLerp(0.72f, 1f, normalized) * 0.35f
            : 1f;

        float outerScale = DiameterMeters * expandCurve * flicker * endShrink;
        _outer.localScale = Vector3.one * outerScale;
        _mid.localScale = Vector3.one * (outerScale * 0.68f);
        _core.localScale = Vector3.one * (outerScale * 0.34f);

        SetMaterialColor(
            _outerMaterial,
            Color.Lerp(new Color(1f, 0.42f, 0.08f, 1f), new Color(0.45f, 0.08f, 0.02f, 1f), normalized),
            fade);
        SetMaterialColor(
            _midMaterial,
            Color.Lerp(new Color(1f, 0.72f, 0.16f, 1f), new Color(0.72f, 0.18f, 0.04f, 1f), normalized),
            fade);
        SetMaterialColor(
            _coreMaterial,
            Color.Lerp(new Color(1f, 0.95f, 0.72f, 1f), new Color(0.95f, 0.42f, 0.08f, 1f), normalized),
            fade);

        if (_elapsed >= DurationSeconds)
        {
            Destroy(gameObject);
        }
    }

    Transform CreateFireLayer(string name, Color color, out Material material)
    {
        var layer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        layer.name = name;
        layer.transform.SetParent(transform, false);
        material = CreateFireMaterial(color);
        layer.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(layer.GetComponent<Collider>());
        return layer.transform;
    }

    static Material CreateFireMaterial(Color color)
    {
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = "Frag Grenade Explosion Fire",
            color = color
        };

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }

        return material;
    }

    static void SetMaterialColor(Material material, Color color, float fade)
    {
        if (material == null)
        {
            return;
        }

        color.a *= fade;
        material.color = color;
    }

    static void DestroyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }
}
