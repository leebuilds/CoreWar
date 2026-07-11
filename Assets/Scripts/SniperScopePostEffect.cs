using UnityEngine;

/// <summary>
/// Blurs the screen outside the sniper ADS clear zone. Magnified scopes also darken the periphery.
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

    [Range(0f, 0.03f)]
    public float blurSizeIron = 0.007f;

    [Range(0.05f, 0.9f)]
    public float ironSightScopeRadius = 0.62f;

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
    bool _ironSightFrame;

    float _fullScreenBlurBlend;
    Material _fullScreenBlurMaterial;

    public void SetFullScreenBlur(float blend)
    {
        _fullScreenBlurBlend = Mathf.Clamp01(blend);
    }

    public void ClaimAsLocalInstance()
    {
        Instance = this;
    }

    void Awake()
    {
        var attachedCamera = GetComponent<Camera>();
        if (attachedCamera == null || attachedCamera.enabled)
        {
            ClaimAsLocalInstance();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyMaterial();
        DestroyFullScreenBlurMaterial();
    }

    public void SetActive(bool active, float blend, int scopeIndex, bool ironSightFrame = false)
    {
        _active = active;
        _blend = Mathf.Clamp01(blend);
        _scopeIndex = scopeIndex;
        _ironSightFrame = ironSightFrame;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture workingSource = source;

        if (_fullScreenBlurBlend > 0.001f)
        {
            EnsureFullScreenBlurMaterial();
            if (_fullScreenBlurMaterial != null)
            {
                _fullScreenBlurMaterial.SetFloat("_BlurSize", 0.014f * _fullScreenBlurBlend);
                Graphics.Blit(workingSource, destination, _fullScreenBlurMaterial);
                return;
            }
        }

        if (!_active || _blend <= 0.001f || _scopeIndex < 0)
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

        bool ironSights = _scopeIndex == 0 || _ironSightFrame;
        bool tenX = _scopeIndex >= 2;
        float scopeRadiusValue = ironSights ? ironSightScopeRadius : this.scopeRadius;
        float blurSize = ironSights ? blurSizeIron : (tenX ? blurSize10x : blurSize4x);
        float vignetteDarkness = ironSights ? 0f : (tenX ? vignetteDarkness10x : vignetteDarkness4x);
        float darkBandWidth = ironSights ? darkBandWidth4x : (tenX ? darkBandWidth10x : darkBandWidth4x);

        _material.SetFloat("_ScopeRadius", scopeRadiusValue);
        _material.SetFloat("_ScopeBlend", _blend);
        _material.SetFloat("_VignetteDarkness", vignetteDarkness);
        _material.SetFloat("_BlurSize", blurSize);
        _material.SetFloat("_DarkBandWidth", darkBandWidth);

        Graphics.Blit(source, destination, _material);
    }

    void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        DestroyMaterial();
        DestroyFullScreenBlurMaterial();
    }

    void EnsureFullScreenBlurMaterial()
    {
        if (_fullScreenBlurMaterial != null)
        {
            return;
        }

        var shader = Shader.Find("Hidden/CoreWar/FullScreenBlur");
        if (shader == null)
        {
            return;
        }

        _fullScreenBlurMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    void DestroyFullScreenBlurMaterial()
    {
        if (_fullScreenBlurMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_fullScreenBlurMaterial);
        }
        else
        {
            DestroyImmediate(_fullScreenBlurMaterial);
        }

        _fullScreenBlurMaterial = null;
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
