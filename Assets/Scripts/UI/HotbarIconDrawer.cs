using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural hotbar slot icons for gameplay HUD textures.
/// </summary>
public static class HotbarIconDrawer
{
    const int IconTextureSize = 48;

    static readonly Dictionary<int, Texture2D> ToolIconCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> InfantryAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> IronSightCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> DashAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> ShieldAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> HoldBreathAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> HunterMarkAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> CyborgRegenAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Dictionary<int, Texture2D> AntiMaterialBraceAbilityCache = new Dictionary<int, Texture2D>();
    static readonly Color GunMetal = new Color(0.1f, 0.1f, 0.11f, 1f);
    static readonly Color HammerWood = new Color(0.34f, 0.24f, 0.15f, 1f);
    static readonly Color HammerMetal = new Color(0.58f, 0.58f, 0.6f, 1f);
    static readonly Color BlueprintBlue = new Color(0.08f, 0.22f, 0.68f, 1f);
    static readonly Color BootBrown = new Color(0.28f, 0.18f, 0.12f, 1f);
    static readonly Color WingGray = new Color(0.42f, 0.42f, 0.44f, 1f);
    static readonly Color ScopeRed = new Color(0.92f, 0.12f, 0.1f, 1f);
    static readonly Color IronSightInk = new Color(0.12f, 0.12f, 0.12f, 1f);

    public static Texture2D GetToolIconTexture(CardHotbarTool tool, bool dimmed = false)
    {
        int key = ((int)tool << 1) | (dimmed ? 1 : 0);
        if (ToolIconCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        float alpha = dimmed ? 0.55f : 1f;
        var texture = CreateIconTexture();
        switch (tool)
        {
            case CardHotbarTool.Pistol:
                RasterPistolIcon(texture, alpha);
                break;
            case CardHotbarTool.AssaultRifle:
                RasterAssaultRifleIcon(texture, alpha);
                break;
            case CardHotbarTool.ScopedAssaultRifle:
                RasterScopedAssaultRifleIcon(texture, alpha);
                break;
            case CardHotbarTool.SniperRifle:
                RasterSniperRifleIcon(texture, alpha);
                break;
            case CardHotbarTool.HuntingRifle:
                RasterHuntingRifleIcon(texture, alpha);
                break;
            case CardHotbarTool.Smg:
                RasterSmgIcon(texture, alpha);
                break;
            case CardHotbarTool.MachinePistol:
                RasterMachinePistolIcon(texture, alpha);
                break;
            case CardHotbarTool.LightMachineGun:
                RasterLmgIcon(texture, alpha);
                break;
            case CardHotbarTool.CyborgLaser:
                RasterCyborgLaserIcon(texture, alpha);
                break;
            case CardHotbarTool.AntiMaterialRifle:
                RasterAntiMaterialRifleIcon(texture, alpha);
                break;
            case CardHotbarTool.LaserSword:
                RasterLaserSwordIcon(texture, alpha);
                break;
            case CardHotbarTool.Hammer:
                RasterHammerIcon(texture, alpha);
                break;
            case CardHotbarTool.Blueprint:
                RasterBlueprintIcon(texture, alpha);
                break;
        }

        texture.Apply();
        ToolIconCache[key] = texture;
        return texture;
    }

    public static Texture2D GetInfantryAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (InfantryAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterBootWithWingsIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        InfantryAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetDashAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (DashAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterDashIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        DashAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetShieldAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (ShieldAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterShieldIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        ShieldAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetHoldBreathAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (HoldBreathAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterHoldBreathIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        HoldBreathAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetHunterMarkAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (HunterMarkAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterHunterMarkIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        HunterMarkAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetCyborgRegenAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (CyborgRegenAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterCyborgRegenIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        CyborgRegenAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetAntiMaterialBraceAbilityIconTexture(bool dimmed = false)
    {
        int key = dimmed ? 1 : 0;
        if (AntiMaterialBraceAbilityCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterAntiMaterialBraceIcon(texture, dimmed ? 0.55f : 1f);
        texture.Apply();
        AntiMaterialBraceAbilityCache[key] = texture;
        return texture;
    }

    public static Texture2D GetIronSightIconTexture(bool dimmed = false)
    {
        int key = (dimmed ? 1 : 0) | 2;
        if (IronSightCache.TryGetValue(key, out Texture2D cached))
        {
            return cached;
        }

        var texture = CreateIconTexture();
        RasterIronSightIcon(texture, dimmed ? 0.55f : 1f);
        FlipTextureVertically(texture);
        texture.Apply();
        IronSightCache[key] = texture;
        return texture;
    }

    static Texture2D CreateIconTexture()
    {
        var texture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var clear = new Color32(0, 0, 0, 0);
        var pixels = new Color32[IconTextureSize * IconTextureSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        texture.SetPixels32(pixels);
        return texture;
    }

    static void RasterPistolIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.18f, 0.42f, 0.42f, 0.18f, body);
        RasterBox(texture, 0.48f, 0.4f, 0.34f, 0.12f, body);
        RasterBox(texture, 0.24f, 0.58f, 0.12f, 0.24f, body);
    }

    static void RasterAssaultRifleIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.08f, 0.4f, 0.72f, 0.16f, body);
        RasterBox(texture, 0.64f, 0.48f, 0.28f, 0.1f, body);
        RasterBox(texture, 0.02f, 0.42f, 0.14f, 0.12f, body);
        RasterBox(texture, 0.48f, 0.54f, 0.1f, 0.22f, body);
    }

    static void RasterScopedAssaultRifleIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.08f, 0.4f, 0.72f, 0.16f, body);
        RasterBox(texture, 0.64f, 0.48f, 0.28f, 0.1f, body);
        RasterBox(texture, 0.02f, 0.42f, 0.14f, 0.12f, body);
        RasterBox(texture, 0.48f, 0.54f, 0.1f, 0.22f, body);
        RasterBox(texture, 0.34f, 0.52f, 0.16f, 0.1f, body);
    }

    static void RasterSniperRifleIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.04f, 0.44f, 0.82f, 0.12f, body);
        RasterBox(texture, 0.58f, 0.38f, 0.34f, 0.08f, body);
        RasterBox(texture, 0.02f, 0.42f, 0.16f, 0.14f, body);
        RasterBox(texture, 0.34f, 0.3f, 0.22f, 0.1f, body);
        RasterBox(texture, 0.36f, 0.56f, 0.08f, 0.18f, body);
    }

    static void RasterHuntingRifleIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.06f, 0.44f, 0.76f, 0.12f, body);
        RasterBox(texture, 0.58f, 0.4f, 0.3f, 0.08f, body);
        RasterBox(texture, 0.02f, 0.42f, 0.14f, 0.12f, body);
        RasterBox(texture, 0.34f, 0.52f, 0.08f, 0.06f, body);
    }

    static void RasterSmgIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.22f, 0.42f, 0.34f, 0.14f, body);
        RasterBox(texture, 0.46f, 0.44f, 0.18f, 0.1f, body);
        RasterBox(texture, 0.28f, 0.56f, 0.08f, 0.2f, body);
    }

    static void RasterMachinePistolIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.28f, 0.44f, 0.24f, 0.12f, body);
        RasterBox(texture, 0.44f, 0.46f, 0.14f, 0.08f, body);
        RasterBox(texture, 0.32f, 0.56f, 0.06f, 0.16f, body);
    }

    static void RasterLmgIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.06f, 0.42f, 0.76f, 0.14f, body);
        RasterBox(texture, 0.62f, 0.46f, 0.26f, 0.1f, body);
        RasterBox(texture, 0.02f, 0.42f, 0.12f, 0.12f, body);
        RasterBox(texture, 0.42f, 0.54f, 0.1f, 0.2f, body);
    }

    static void RasterCyborgLaserIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        Color32 laser = ToColor32(WithAlpha(ScopeRed, alpha));
        RasterBox(texture, 0.34f, 0.42f, 0.18f, 0.34f, body);
        RasterBox(texture, 0.46f, 0.46f, 0.24f, 0.08f, laser);
        RasterBox(texture, 0.58f, 0.46f, 0.16f, 0.06f, laser);
    }

    static void RasterAntiMaterialRifleIcon(Texture2D texture, float alpha)
    {
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.04f, 0.4f, 0.84f, 0.16f, body);
        RasterBox(texture, 0.64f, 0.44f, 0.24f, 0.1f, body);
        RasterBox(texture, 0.02f, 0.4f, 0.14f, 0.14f, body);
        RasterBox(texture, 0.34f, 0.52f, 0.12f, 0.24f, body);
        RasterBox(texture, 0.18f, 0.48f, 0.1f, 0.08f, ToColor32(WithAlpha(ScopeRed, alpha)));
    }

    static void RasterLaserSwordIcon(Texture2D texture, float alpha)
    {
        Color32 blade = ToColor32(WithAlpha(ScopeRed, alpha));
        Color32 body = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.42f, 0.58f, 0.1f, 0.24f, body);
        RasterBox(texture, 0.4f, 0.24f, 0.14f, 0.42f, blade);
        RasterBox(texture, 0.36f, 0.5f, 0.22f, 0.04f, body);
    }

    static void RasterHammerIcon(Texture2D texture, float alpha)
    {
        RasterBox(texture, 0.44f, 0.34f, 0.12f, 0.5f, ToColor32(WithAlpha(HammerWood, alpha)));
        RasterBox(texture, 0.18f, 0.16f, 0.64f, 0.18f, ToColor32(WithAlpha(HammerMetal, alpha)));
    }

    static void RasterBlueprintIcon(Texture2D texture, float alpha)
    {
        Color32 page = ToColor32(WithAlpha(BlueprintBlue, alpha));
        RasterBox(texture, 0.16f, 0.22f, 0.68f, 0.56f, page);
        RasterBox(texture, 0.58f, 0.22f, 0.12f, 0.12f, ToColor32(WithAlpha(Color.white, alpha * 0.35f)));
        RasterBox(texture, 0.24f, 0.38f, 0.34f, 0.04f, ToColor32(WithAlpha(Color.white, alpha * 0.45f)));
        RasterBox(texture, 0.24f, 0.48f, 0.28f, 0.04f, ToColor32(WithAlpha(Color.white, alpha * 0.45f)));
    }

    static void RasterBootWithWingsIcon(Texture2D texture, float alpha)
    {
        Color32 boot = ToColor32(WithAlpha(BootBrown, alpha));
        Color32 wing = ToColor32(WithAlpha(WingGray, alpha));
        RasterBox(texture, 0.36f, 0.28f, 0.28f, 0.48f, boot);
        RasterBox(texture, 0.3f, 0.66f, 0.37f, 0.12f, boot);
        RasterBox(texture, 0.66f, 0.46f, 0.18f, 0.14f, wing);
        RasterBox(texture, 0.64f, 0.6f, 0.12f, 0.08f, wing);
    }

    static void RasterDashIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        RasterBox(texture, 0.18f, 0.46f, 0.52f, 0.1f, ink);
        RasterTriangleUp(texture, 0.72f, 0.5f, 0.16f, ink);
    }

    static void RasterShieldIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        RasterBox(texture, 0.34f, 0.24f, 0.32f, 0.08f, ink);
        RasterBox(texture, 0.28f, 0.32f, 0.44f, 0.34f, ink);
        RasterBox(texture, 0.36f, 0.62f, 0.28f, 0.12f, ink);
    }

    static void RasterHoldBreathIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        RasterBox(texture, 0.34f, 0.34f, 0.32f, 0.34f, ink);
        RasterBox(texture, 0.4f, 0.68f, 0.2f, 0.08f, ink);
        RasterBox(texture, 0.58f, 0.68f, 0.2f, 0.08f, ink);
    }

    static void RasterAntiMaterialBraceIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        Color32 metal = ToColor32(WithAlpha(GunMetal, alpha));
        RasterBox(texture, 0.46f, 0.18f, 0.08f, 0.52f, ink);
        RasterBox(texture, 0.34f, 0.18f, 0.08f, 0.52f, ink);
        RasterBox(texture, 0.58f, 0.18f, 0.08f, 0.52f, ink);
        RasterBox(texture, 0.24f, 0.42f, 0.52f, 0.08f, metal);
    }

    static void RasterCyborgRegenIcon(Texture2D texture, float alpha)
    {
        Color32 red = ToColor32(WithAlpha(ScopeRed, alpha));
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        RasterBox(texture, 0.34f, 0.24f, 0.32f, 0.52f, ink);
        RasterBox(texture, 0.4f, 0.34f, 0.2f, 0.24f, red);
        RasterTriangleUp(texture, 0.5f, 0.7f, 0.12f, red);
    }

    static void RasterHunterMarkIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(new Color(0.92f, 0.12f, 0.1f, 1f), alpha));
        RasterBox(texture, 0.28f, 0.34f, 0.44f, 0.44f, ink);
        RasterBox(texture, 0.4f, 0.24f, 0.2f, 0.08f, ink);
    }

    static void RasterIronSightIcon(Texture2D texture, float alpha)
    {
        Color32 ink = ToColor32(WithAlpha(IronSightInk, alpha));
        RasterBox(texture, 0.14f, 0.18f, 0.06f, 0.64f, ink);
        RasterBox(texture, 0.8f, 0.18f, 0.06f, 0.64f, ink);
        RasterTriangleUp(texture, 0.5f, 0.56f, 0.22f, ink);
    }

    static void RasterTriangleUp(Texture2D texture, float centerX, float centerY, float size, Color32 color)
    {
        int pixelSize = texture.width;
        float half = size * pixelSize;
        float topX = centerX * pixelSize;
        float topY = (1f - centerY) * pixelSize;
        float baseY = topY + (half * 1.5f);

        for (int y = Mathf.FloorToInt(topY); y <= Mathf.CeilToInt(baseY); y++)
        {
            if (y < 0 || y >= pixelSize)
            {
                continue;
            }

            float t = (y - topY) / Mathf.Max(1f, baseY - topY);
            float span = half * t;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(topX - span), 0, pixelSize - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(topX + span), 0, pixelSize - 1);
            for (int x = x0; x <= x1; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }

    static void RasterBox(Texture2D texture, float x, float y, float w, float h, Color32 color)
    {
        int size = texture.width;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x * size), 0, size - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt((1f - y - h) * size), 0, size - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((x + w) * size) - 1, 0, size - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt((1f - y) * size) - 1, 0, size - 1);

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                texture.SetPixel(px, py, color);
            }
        }
    }

    static void FlipTextureVertically(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        for (int y = 0; y < height / 2; y++)
        {
            int oppositeY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                Color top = texture.GetPixel(x, oppositeY);
                Color bottom = texture.GetPixel(x, y);
                texture.SetPixel(x, y, top);
                texture.SetPixel(x, oppositeY, bottom);
            }
        }
    }

    static Color32 ToColor32(Color color)
    {
        return new Color32(
            (byte)(color.r * 255f),
            (byte)(color.g * 255f),
            (byte)(color.b * 255f),
            (byte)(color.a * 255f));
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
