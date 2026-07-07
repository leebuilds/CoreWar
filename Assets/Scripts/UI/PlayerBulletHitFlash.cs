using UnityEngine;

/// <summary>
/// Full-screen blindness when the local player is struck. Hold is pitch black;
/// fade in and fade out pass through red. Drawn via ThirdPersonController.OnGUI.
/// Re-hits while already blind skip the flash and restart the hold timer.
/// </summary>
public class PlayerBulletHitFlash : MonoBehaviour
{
    public static PlayerBulletHitFlash Instance { get; private set; }

    const float FadeInDuration = 0.04f;
    const float FadeOutMin = 0.1f;
    const float FadeOutMax = 1.35f;
    const float FadeOutReferenceBlind = 4f;
    const float RedPeakAlpha = 0.88f;

    static readonly Color RedFlash = new Color(0.82f, 0.08f, 0.08f, 1f);
    static readonly Color Blackout = new Color(0f, 0f, 0f, 1f);

    float _remaining;
    float _totalDuration;
    bool _skipFadeIn;

    public static PlayerBulletHitFlash Create()
    {
        var host = new GameObject("Player Bullet Hit Flash");
        return host.AddComponent<PlayerBulletHitFlash>();
    }

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
    }

    void Update()
    {
        if (_remaining > 0f)
        {
            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            if (_remaining <= 0f)
            {
                _skipFadeIn = false;
            }
        }
    }

    public static void DrawOverlay()
    {
        if (Instance == null || Instance._remaining <= 0f)
        {
            return;
        }

        ComputeAlphas(
            Instance._totalDuration,
            Instance._remaining,
            Instance._skipFadeIn,
            out float blackAlpha,
            out float redAlpha);
        var screen = new Rect(0f, 0f, Screen.width, Screen.height);
        Color previousColor = GUI.color;

        if (redAlpha > 0.001f)
        {
            GUI.color = new Color(RedFlash.r, RedFlash.g, RedFlash.b, redAlpha);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
        }

        if (blackAlpha > 0.001f)
        {
            GUI.color = new Color(Blackout.r, Blackout.g, Blackout.b, blackAlpha);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
        }

        GUI.color = previousColor;
    }

    static void ComputeAlphas(float totalDuration, float remaining, bool skipFadeIn,
        out float blackAlpha, out float redAlpha)
    {
        blackAlpha = 0f;
        redAlpha = 0f;

        if (totalDuration <= 0f || remaining <= 0f)
        {
            return;
        }

        float elapsed = totalDuration - remaining;
        float fadeIn = skipFadeIn ? 0f : Mathf.Min(FadeInDuration, totalDuration * 0.25f);
        float fadeOut = ComputeFadeOutDuration(totalDuration);
        fadeOut = Mathf.Min(fadeOut, Mathf.Max(0f, totalDuration - fadeIn));
        float holdEnd = fadeIn + Mathf.Max(0f, totalDuration - fadeIn - fadeOut);

        if (elapsed < fadeIn)
        {
            float t = fadeIn > 0f ? elapsed / fadeIn : 1f;
            float snap = t * t;
            redAlpha = (1f - snap) * RedPeakAlpha;
            blackAlpha = snap;
            return;
        }

        if (elapsed < holdEnd)
        {
            blackAlpha = 1f;
            return;
        }

        float fadeElapsed = elapsed - holdEnd;
        float tOut = fadeOut > 0f ? fadeElapsed / fadeOut : 1f;
        blackAlpha = 1f - tOut;
        redAlpha = Mathf.Sin(tOut * Mathf.PI) * RedPeakAlpha;
    }

    static float ComputeFadeOutDuration(float totalDuration)
    {
        float t = Mathf.InverseLerp(0.125f, FadeOutReferenceBlind, totalDuration);
        return Mathf.Lerp(FadeOutMin, FadeOutMax, Mathf.Clamp01(t));
    }

    public void Blind(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (_remaining > 0f)
        {
            _remaining = duration;
            _totalDuration = duration;
            _skipFadeIn = true;
            return;
        }

        _remaining = duration;
        _totalDuration = duration;
        _skipFadeIn = false;
    }
}
