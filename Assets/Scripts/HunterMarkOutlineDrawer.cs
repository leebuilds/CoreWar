using UnityEngine;

/// <summary>
/// Procedural hunter mark: wide head bullseye with cross ticks and droplet guide lines.
/// </summary>
public static class HunterMarkOutlineDrawer
{
    const int Width = 56;
    const int Height = 154;
    const float LogicalTopY = 0.84f + 0.24f + 0.055f;

    // Pivot sits on the head-center bullseye (fitted into the texture with top padding).
    public const float PivotYNormalized = 0.84f / LogicalTopY;
    public const float SpriteBottomYNormalized = 0.05f / LogicalTopY;
    public const float SpriteTopArtYNormalized = 1f;

    static Sprite _sprite;

    public static Sprite GetTargetMarkSprite()
    {
        if (_sprite != null)
        {
            return _sprite;
        }

        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[Width * Height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }

        var ink = new Color32(235, 30, 26, 255);
        float bullseyeY = Y(0.84f);
        const float outerRadius = 0.24f;
        const float innerRadius = 0.1f;

        RasterRing(pixels, 0.5f, bullseyeY, outerRadius, 2, ink);
        RasterRing(pixels, 0.5f, bullseyeY, innerRadius, 2, ink);
        RasterDisc(pixels, 0.5f, bullseyeY, 0.035f, ink);

        RasterCrossTicks(pixels, 0.5f, bullseyeY, outerRadius, 0.055f, 2, ink);

        float dropletTop = bullseyeY - outerRadius - Y(0.03f);
        float dropletBottom = Y(0.05f);
        RasterTeardrop(pixels, 0.36f, dropletTop, dropletBottom, 0.028f, 0.008f, ink);
        RasterTeardrop(pixels, 0.64f, dropletTop, dropletBottom, 0.028f, 0.008f, ink);

        texture.SetPixels32(pixels);
        texture.Apply();

        _sprite = Sprite.Create(
            texture,
            new Rect(0, 0, Width, Height),
            new Vector2(0.5f, PivotYNormalized),
            100f);
        _sprite.hideFlags = HideFlags.HideAndDontSave;
        return _sprite;
    }

    static float Y(float logicalY)
    {
        return logicalY / LogicalTopY;
    }

    static void RasterRing(Color32[] pixels, float cx, float cy, float radius, int thickness, Color32 color)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt((cx - radius) * Width), 0, Width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((cx + radius) * Width), 0, Width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt((cy - radius) * Height), 0, Height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt((cy + radius) * Height), 0, Height - 1);
        float inner = radius - (thickness / (float)Width * 2.4f);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x + 0.5f) / Width;
                float ny = (y + 0.5f) / Height;
                float dx = nx - cx;
                float dy = ny - cy;
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                if (dist <= radius && dist >= inner)
                {
                    pixels[(y * Width) + x] = color;
                }
            }
        }
    }

    static void RasterDisc(Color32[] pixels, float cx, float cy, float radius, Color32 color)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt((cx - radius) * Width), 0, Width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((cx + radius) * Width), 0, Width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt((cy - radius) * Height), 0, Height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt((cy + radius) * Height), 0, Height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x + 0.5f) / Width;
                float ny = (y + 0.5f) / Height;
                float dx = nx - cx;
                float dy = ny - cy;
                if ((dx * dx) + (dy * dy) <= radius * radius)
                {
                    pixels[(y * Width) + x] = color;
                }
            }
        }
    }

    static void RasterCrossTicks(
        Color32[] pixels,
        float cx,
        float cy,
        float radius,
        float tickLength,
        int thickness,
        Color32 color)
    {
        RasterSegment(pixels, cx - radius - tickLength, cy, cx - radius, cy, thickness, color);
        RasterSegment(pixels, cx + radius, cy, cx + radius + tickLength, cy, thickness, color);
        RasterSegment(pixels, cx, cy + radius, cx, cy + radius + tickLength, thickness, color);
        RasterSegment(pixels, cx, cy - radius - tickLength, cx, cy - radius, thickness, color);
    }

    static void RasterSegment(
        Color32[] pixels,
        float x0,
        float y0,
        float x1,
        float y1,
        int thickness,
        Color32 color)
    {
        int steps = Mathf.Max(
            Mathf.Abs(Mathf.RoundToInt((x1 - x0) * Width)),
            Mathf.Abs(Mathf.RoundToInt((y1 - y0) * Height)),
            1);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float nx = Mathf.Lerp(x0, x1, t);
            float ny = Mathf.Lerp(y0, y1, t);
            int px = Mathf.RoundToInt(nx * Width);
            int py = Mathf.RoundToInt(ny * Height);

            for (int oy = -thickness; oy <= thickness; oy++)
            {
                for (int ox = -thickness; ox <= thickness; ox++)
                {
                    int x = px + ox;
                    int y = py + oy;
                    if (x < 0 || x >= Width || y < 0 || y >= Height)
                    {
                        continue;
                    }

                    pixels[(y * Width) + x] = color;
                }
            }
        }
    }

    static void RasterTeardrop(
        Color32[] pixels,
        float cx,
        float topY,
        float bottomY,
        float maxHalfWidth,
        float topHalfWidth,
        Color32 color)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt((cx - maxHalfWidth) * Width), 0, Width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((cx + maxHalfWidth) * Width), 0, Width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(bottomY * Height), 0, Height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(topY * Height), 0, Height - 1);
        float span = Mathf.Max(0.0001f, topY - bottomY);

        for (int y = minY; y <= maxY; y++)
        {
            float ny = (y + 0.5f) / Height;
            float fall = (topY - ny) / span;
            if (fall < 0f || fall > 1f)
            {
                continue;
            }

            float bulge = Mathf.Sin(fall * Mathf.PI);
            float halfWidth = (topHalfWidth * (1f - bulge) + maxHalfWidth * bulge) * (1f - fall);

            int xCenter = Mathf.RoundToInt(cx * Width);
            int halfPx = Mathf.Max(1, Mathf.RoundToInt(halfWidth * Width));
            for (int x = xCenter - halfPx; x <= xCenter + halfPx; x++)
            {
                if (x < 0 || x >= Width)
                {
                    continue;
                }

                pixels[(y * Width) + x] = color;
            }
        }
    }
}
