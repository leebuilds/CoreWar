using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helpers for materials that use CoreWar/VoxelFaceLit (_MainTex) instead of _Color.
/// </summary>
public static class VoxelMaterialUtility
{
    static readonly Dictionary<Color32, Texture2D> SolidTextures = new Dictionary<Color32, Texture2D>();

    public static Material CreateSolidMaterial(Color color, string materialName = "Solid Voxel")
    {
        var shader = Shader.Find("CoreWar/VoxelFaceLit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            mainTexture = GetSolidTexture(color)
        };

        if (material.HasProperty("_ShadowLevel"))
        {
            material.SetFloat("_ShadowLevel", 0.58f);
        }

        return material;
    }

    public static Texture GetSolidTexture(Color color)
    {
        var key = (Color32)color;
        if (SolidTextures.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        SolidTextures[key] = texture;
        return texture;
    }

    public static void SetRendererAlbedo(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        var material = renderer.material;
        if (material.HasProperty("_MainTex"))
        {
            material.mainTexture = GetSolidTexture(color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    public static Texture GetRendererAlbedo(Renderer renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        var material = renderer.material;
        if (material.HasProperty("_MainTex"))
        {
            return material.mainTexture;
        }

        return null;
    }

    public static void SetRendererAlbedoTexture(Renderer renderer, Texture texture)
    {
        if (renderer == null || texture == null)
        {
            return;
        }

        var material = renderer.material;
        if (material.HasProperty("_MainTex"))
        {
            material.mainTexture = texture;
        }
    }
}
