using UnityEngine;

/// <summary>
/// Lightweight post-effect that adds cross-hatching in darker areas
/// so cast shadows read with a pen-and-ink style.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PenInkShadowEffect : MonoBehaviour
{
    [Range(0.2f, 0.9f)]
    public float shadowThreshold = 0.28f;
    [Range(8f, 80f)]
    public float hatchScale = 20f;
    [Range(0f, 1f)]
    public float paperBlend = 0.03f;
    [Range(0f, 2f)]
    public float centerDarkness = 0.78f;
    [Range(0.5f, 6f)]
    public float circularFalloff = 3.2f;
    [Range(0.3f, 1f)]
    public float topSurfaceThreshold = 0.82f;
    public float voxelSize = 1f;
    public Color inkColor = new Color(0.34f, 0.34f, 0.36f, 1f);
    public Color paperTint = new Color(0.985f, 0.985f, 0.985f, 1f);

    Material _material;
    Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_material == null)
        {
            var shader = Shader.Find("Hidden/CoreWar/PenInkShadowPost");
            if (shader == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
        }

        _material.SetFloat("_ShadowThreshold", shadowThreshold);
        _material.SetFloat("_HatchScale", hatchScale);
        _material.SetFloat("_PaperBlend", paperBlend);
        _material.SetFloat("_CenterDarkness", centerDarkness);
        _material.SetFloat("_CircularFalloff", circularFalloff);
        _material.SetFloat("_TopSurfaceThreshold", topSurfaceThreshold);
        _material.SetFloat("_VoxelSize", Mathf.Max(0.0001f, voxelSize));
        _material.SetColor("_InkColor", inkColor);
        _material.SetColor("_PaperTint", paperTint);
        _material.SetMatrix("_InverseViewProjection",
            (_camera.projectionMatrix * _camera.worldToCameraMatrix).inverse);

        Graphics.Blit(source, destination, _material);
    }

    void OnDisable()
    {
        if (_material != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_material);
            }
            else
            {
                DestroyImmediate(_material);
            }
        }
    }
}
