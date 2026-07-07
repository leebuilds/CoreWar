using UnityEngine;

/// <summary>
/// Darkens and blurs the screen outside the magnified sniper scope ring.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public class SniperScopePostEffect : MonoBehaviour
{
    public static SniperScopePostEffect Instance { get; private set; }

    [Range(0.05f, 0.9f)]
    public float scopeRadius = 1f / 3f;

    [Range(0f, 0.03f)]
    public float blurSize4x = 0.008f;

    [Range(0f, 0.03f)]
    public float blurSize10x = 0.02f;

    [Range(0f, 1f)]
    public float vignetteDarkness4x = 0.88f;

    [Range(0f, 1f)]
    public float vignetteDarkness10x = 0.98f;

    [Range(0.05f, 0.6f)]
    public float darkBandWidth4x = 0.2f;

    [Range(0.05f, 0.6f)]
    public float darkBandWidth10x = 0.28f;

    Material _material;
    bool _active;
    float _blend;
    int _scopeIndex;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyMaterial();
    }

    public void SetActive(bool active, float blend, int scopeIndex)
    {
        _active = active;
        _blend = Mathf.Clamp01(blend);
        _scopeIndex = scopeIndex;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_active || _blend <= 0.001f || _scopeIndex < 1)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (_material == null)
        {
            var shader = Shader.Find("Hidden/CoreWar/SniperScopePost");
            if (shader == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        bool tenX = _scopeIndex >= 2;
        float blurSize = tenX ? blurSize10x : blurSize4x;
        float vignetteDarkness = tenX ? vignetteDarkness10x : vignetteDarkness4x;
        float darkBandWidth = tenX ? darkBandWidth10x : darkBandWidth4x;

        _material.SetFloat("_ScopeRadius", scopeRadius);
        _material.SetFloat("_ScopeBlend", _blend);
        _material.SetFloat("_VignetteDarkness", vignetteDarkness);
        _material.SetFloat("_BlurSize", blurSize);
        _material.SetFloat("_DarkBandWidth", darkBandWidth);

        Graphics.Blit(source, destination, _material);
    }

    void OnDisable()
    {
        DestroyMaterial();
    }

    void DestroyMaterial()
    {
        if (_material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_material);
        }
        else
        {
            DestroyImmediate(_material);
        }

        _material = null;
    }
}
