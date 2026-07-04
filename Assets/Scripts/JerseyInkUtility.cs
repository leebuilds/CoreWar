using UnityEngine;

/// <summary>
/// Builds torn jersey textures with pen-and-ink crosshatch shading.
/// </summary>
public static class JerseyInkUtility
{
    static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);

    public static Texture2D CreateJerseyPanel(Color teamColor, bool includeNumber, int number)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        int tearStart = Random.Range(72, 88);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fold = Mathf.Clamp01((size - y) / (float)size);
                float noise = Mathf.PerlinNoise(x * 0.09f, y * 0.11f);
                Color baseColor = Color.Lerp(teamColor * 0.82f, teamColor * 1.05f, fold * 0.55f + noise * 0.2f);

                float hatchA = step(0.5f, frac((x + y) * 0.14f));
                float hatchB = step(0.5f, frac((x - y) * 0.14f));
                float hatch = (hatchA + hatchB) * 0.5f;
                float shadowMask = Mathf.Clamp01(0.35f + (1f - fold) * 0.55f + noise * 0.25f);
                baseColor = Color.Lerp(baseColor, Ink, hatch * shadowMask * 0.42f);

                if (y < tearStart)
                {
                    float edge = tearStart - y;
                    float ragged = Mathf.PerlinNoise(x * 0.21f, y * 0.07f);
                    if (edge < 4f + ragged * 6f)
                    {
                        baseColor = Color.Lerp(new Color(0.55f, 0.57f, 0.62f), baseColor, edge / 8f);
                    }
                }

                texture.SetPixel(x, y, baseColor);
            }
        }

        if (includeNumber)
        {
            DrawNumber(texture, number, Ink, teamColor * 1.1f);
        }

        texture.Apply();
        return texture;
    }

    static float frac(float v) => v - Mathf.Floor(v);

    static float step(float edge, float x) => x >= edge ? 1f : 0f;

    static void DrawNumber(Texture2D texture, int number, Color ink, Color highlight)
    {
        string text = number.ToString();
        int digitWidth = 18;
        int digitHeight = 28;
        int spacing = 6;
        int totalWidth = text.Length * digitWidth + (text.Length - 1) * spacing;
        int startX = (texture.width - totalWidth) / 2;
        int startY = texture.height / 2 - digitHeight / 2;

        for (int i = 0; i < text.Length; i++)
        {
            DrawDigit(texture, text[i], startX + i * (digitWidth + spacing), startY, digitWidth, digitHeight, ink, highlight);
        }
    }

    static void DrawDigit(Texture2D texture, char digit, int originX, int originY, int width, int height, Color ink, Color highlight)
    {
        bool[] segments = SegmentsForDigit(digit);
        DrawSegment(texture, originX + width * 0.15f, originY + height * 0.82f, width * 0.7f, height * 0.08f, segments[0], ink, highlight);
        DrawSegment(texture, originX + width * 0.15f, originY + height * 0.41f, width * 0.7f, height * 0.08f, segments[1], ink, highlight);
        DrawSegment(texture, originX + width * 0.15f, originY + height * 0.02f, width * 0.7f, height * 0.08f, segments[2], ink, highlight);
        DrawSegment(texture, originX + width * 0.02f, originY + height * 0.45f, width * 0.08f, height * 0.38f, segments[3], ink, highlight);
        DrawSegment(texture, originX + width * 0.02f, originY + height * 0.05f, width * 0.08f, height * 0.38f, segments[4], ink, highlight);
        DrawSegment(texture, originX + width * 0.82f, originY + height * 0.45f, width * 0.08f, height * 0.38f, segments[5], ink, highlight);
        DrawSegment(texture, originX + width * 0.82f, originY + height * 0.05f, width * 0.08f, height * 0.38f, segments[6], ink, highlight);
    }

    static void DrawSegment(Texture2D texture, float x, float y, float w, float h, bool on, Color ink, Color highlight)
    {
        if (!on)
        {
            return;
        }

        int minX = Mathf.Clamp(Mathf.FloorToInt(x), 0, texture.width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(y), 0, texture.height - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(x + w), 0, texture.width - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(y + h), 0, texture.height - 1);

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                float hatch = step(0.5f, frac((px - py) * 0.22f));
                Color c = Color.Lerp(highlight, ink, hatch * 0.55f);
                texture.SetPixel(px, py, c);
            }
        }
    }

    // top, middle, bottom, upper-left, lower-left, upper-right, lower-right
    static bool[] SegmentsForDigit(char digit)
    {
        switch (digit)
        {
            case '0': return new[] { true, false, true, true, true, true, true };
            case '1': return new[] { false, false, false, false, false, true, true };
            case '2': return new[] { true, true, true, false, true, true, false };
            case '3': return new[] { true, true, true, false, false, true, true };
            case '4': return new[] { false, true, false, true, false, true, true };
            case '5': return new[] { true, true, true, true, false, false, true };
            case '6': return new[] { true, true, true, true, true, false, true };
            case '7': return new[] { true, false, false, false, false, true, true };
            case '8': return new[] { true, true, true, true, true, true, true };
            case '9': return new[] { true, true, true, true, false, true, true };
            default: return new[] { false, false, false, false, false, false, false };
        }
    }
}
