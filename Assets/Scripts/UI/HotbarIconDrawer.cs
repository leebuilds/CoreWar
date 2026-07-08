using UnityEngine;

/// <summary>
/// Procedural hotbar slot icons drawn via OnGUI.
/// </summary>
public static class HotbarIconDrawer
{
    static readonly Color GunMetal = new Color(0.1f, 0.1f, 0.11f, 1f);
    static readonly Color HammerWood = new Color(0.34f, 0.24f, 0.15f, 1f);
    static readonly Color HammerMetal = new Color(0.58f, 0.58f, 0.6f, 1f);
    static readonly Color BlueprintBlue = new Color(0.08f, 0.22f, 0.68f, 1f);
    static readonly Color BootBrown = new Color(0.28f, 0.18f, 0.12f, 1f);
    static readonly Color WingGray = new Color(0.42f, 0.42f, 0.44f, 1f);
    static readonly Color ScopeRed = new Color(0.92f, 0.12f, 0.1f, 1f);
    static readonly Color IronSightInk = new Color(0.12f, 0.12f, 0.12f, 1f);

    public static void DrawToolIcon(Rect slot, CardHotbarTool tool, bool dimmed)
    {
        Rect icon = Inset(slot, 7f);
        float alpha = dimmed ? 0.55f : 1f;

        switch (tool)
        {
            case CardHotbarTool.Pistol:
                DrawPistolIcon(icon, alpha);
                break;
            case CardHotbarTool.AssaultRifle:
                DrawAssaultRifleIcon(icon, alpha);
                break;
            case CardHotbarTool.SniperRifle:
                DrawSniperRifleIcon(icon, alpha);
                break;
            case CardHotbarTool.Hammer:
                DrawHammerIcon(icon, alpha);
                break;
            case CardHotbarTool.Blueprint:
                DrawBlueprintIcon(icon, alpha);
                break;
        }
    }

    public static void DrawInfantryAbilityIcon(Rect slot, bool dimmed)
    {
        DrawBootWithWingsIcon(Inset(slot, 6f), dimmed ? 0.55f : 1f);
    }

    public static void DrawSniperScopeAbilityIcon(Rect slot, int scopeIndex, bool dimmed)
    {
        float alpha = dimmed ? 0.55f : 1f;
        Rect icon = Inset(slot, 6f);

        switch (scopeIndex)
        {
            case 1:
                DrawScopeTextIcon(icon, "4X", alpha);
                break;
            case 2:
                DrawScopeTextIcon(icon, "10X", alpha);
                break;
            default:
                DrawIronSightIcon(icon, alpha);
                break;
        }
    }

    static void DrawPistolIcon(Rect r, float alpha)
    {
        Color body = WithAlpha(GunMetal, alpha);
        DrawBox(r, x: 0.18f, y: 0.42f, w: 0.42f, h: 0.18f, body);
        DrawBox(r, x: 0.48f, y: 0.4f, w: 0.34f, h: 0.12f, body);
        DrawBox(r, x: 0.24f, y: 0.58f, w: 0.12f, h: 0.24f, body);
    }

    static void DrawAssaultRifleIcon(Rect r, float alpha)
    {
        Color body = WithAlpha(GunMetal, alpha);
        DrawBox(r, x: 0.08f, y: 0.4f, w: 0.72f, h: 0.16f, body);
        DrawBox(r, x: 0.64f, y: 0.48f, w: 0.28f, h: 0.1f, body);
        DrawBox(r, x: 0.02f, y: 0.42f, w: 0.14f, h: 0.12f, body);
        DrawBox(r, x: 0.48f, y: 0.54f, w: 0.1f, h: 0.22f, body);
    }

    static void DrawSniperRifleIcon(Rect r, float alpha)
    {
        Color body = WithAlpha(GunMetal, alpha);
        DrawBox(r, x: 0.04f, y: 0.44f, w: 0.82f, h: 0.12f, body);
        DrawBox(r, x: 0.58f, y: 0.38f, w: 0.34f, h: 0.08f, body);
        DrawBox(r, x: 0.02f, y: 0.42f, w: 0.16f, h: 0.14f, body);
        DrawBox(r, x: 0.34f, y: 0.3f, w: 0.22f, h: 0.1f, body);
        DrawBox(r, x: 0.36f, y: 0.56f, w: 0.08f, h: 0.18f, body);
    }

    static void DrawHammerIcon(Rect r, float alpha)
    {
        DrawBox(r, x: 0.44f, y: 0.34f, w: 0.12f, h: 0.5f, WithAlpha(HammerWood, alpha));
        DrawBox(r, x: 0.18f, y: 0.16f, w: 0.64f, h: 0.18f, WithAlpha(HammerMetal, alpha));
    }

    static void DrawBlueprintIcon(Rect r, float alpha)
    {
        Color page = WithAlpha(BlueprintBlue, alpha);
        DrawBox(r, x: 0.16f, y: 0.22f, w: 0.68f, h: 0.56f, page);
        DrawBox(r, x: 0.58f, y: 0.22f, w: 0.12f, h: 0.12f, WithAlpha(Color.white, alpha * 0.35f));
        DrawBox(r, x: 0.24f, y: 0.38f, w: 0.34f, h: 0.04f, WithAlpha(Color.white, alpha * 0.45f));
        DrawBox(r, x: 0.24f, y: 0.48f, w: 0.28f, h: 0.04f, WithAlpha(Color.white, alpha * 0.45f));
    }

    static void DrawBootWithWingsIcon(Rect r, float alpha)
    {
        Color boot = WithAlpha(BootBrown, alpha);
        Color wing = WithAlpha(WingGray, alpha);

        DrawBox(r, x: 0.36f, y: 0.28f, w: 0.28f, h: 0.48f, boot);
        DrawBox(r, x: 0.3f, y: 0.66f, w: 0.37f, h: 0.12f, boot);

        DrawBox(r, x: 0.66f, y: 0.46f, w: 0.18f, h: 0.14f, wing);
        DrawBox(r, x: 0.64f, y: 0.6f, w: 0.12f, h: 0.08f, wing);
    }

    static void DrawIronSightIcon(Rect r, float alpha)
    {
        Color ink = WithAlpha(IronSightInk, alpha);
        DrawBox(r, x: 0.14f, y: 0.18f, w: 0.06f, h: 0.64f, ink);
        DrawBox(r, x: 0.8f, y: 0.18f, w: 0.06f, h: 0.64f, ink);

        float cx = r.x + (r.width * 0.5f);
        float cy = r.y + (r.height * 0.56f);
        float size = Mathf.Min(r.width, r.height) * 0.22f;
        DrawTriangleUp(new Rect(cx - size, cy - size, size * 2f, size * 1.5f), ink);
    }

    static void DrawScopeTextIcon(Rect r, string text, float alpha)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = text == "10X" ? 10 : 11,
            fontStyle = FontStyle.Bold,
            normal = { textColor = WithAlpha(ScopeRed, alpha) }
        };
        GUI.Label(r, text, style);
    }

    static void DrawTriangleUp(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;

        float half = rect.width * 0.5f;
        Vector2 top = new Vector2(rect.x + half, rect.y);
        Vector2 left = new Vector2(rect.x, rect.yMax);
        Vector2 right = new Vector2(rect.xMax, rect.yMax);

        for (int i = 0; i < Mathf.CeilToInt(rect.height); i++)
        {
            float t = i / Mathf.Max(1f, rect.height);
            float span = half * t;
            float y = rect.y + i;
            GUI.DrawTexture(new Rect(top.x - span, y, span * 2f, 1f), Texture2D.whiteTexture);
        }

        GUI.color = previous;
    }

    static void DrawBox(Rect parent, float x, float y, float w, float h, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(
            new Rect(
                parent.x + (parent.width * x),
                parent.y + (parent.height * y),
                parent.width * w,
                parent.height * h),
            Texture2D.whiteTexture);
        GUI.color = previous;
    }

    static Rect Inset(Rect rect, float padding)
    {
        return new Rect(rect.x + padding, rect.y + padding, rect.width - (padding * 2f), rect.height - (padding * 2f));
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
