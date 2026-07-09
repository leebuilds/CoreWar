using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen blindness when the local player is struck. Hold is pitch black;
/// fade in and fade out pass through red. Explosion fire uses an orange-red hold
/// before any remaining damage blindness continues in black.
/// </summary>
public class PlayerBulletHitFlash : MonoBehaviour
{
    public static PlayerBulletHitFlash Instance { get; private set; }

    const float FadeInDuration = 0.04f;
    const float FadeOutMin = 0.1f;
    const float FadeOutMax = 1.35f;
    const float FadeOutReferenceBlind = 4f;
    const float RedPeakAlpha = 0.88f;
    const float FireOverlayAlpha = 0.72f;
    const float FireBlackAlpha = 1f;
    const float FireFollowupBlindnessMultiplier = 3f;
    const float MinBlackAfterFireSeconds = 1f;
    const float MaxBlindnessSeconds = 7f;

    static readonly Color RedFlash = new Color(0.82f, 0.08f, 0.08f, 1f);
    static readonly Color Blackout = new Color(0f, 0f, 0f, 1f);
    static readonly Color FireOrange = new Color(1f, 0.44f, 0.06f, 1f);
    static readonly Color FireRed = new Color(0.82f, 0.1f, 0.04f, 1f);

    Image _fireImage;
    Image _redImage;
    Image _blackImage;
    float _remaining;
    float _totalDuration;
    bool _skipFadeIn;
    float _fireRemaining;
    float _fireTotal;
    float _fireQueuedBlackRemaining;

    public bool IsBlind => _fireRemaining > 0f || _remaining > 0f || _fireQueuedBlackRemaining > 0f;

    public static PlayerBulletHitFlash Create()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateLayer("Hit Flash");
        var hostRect = GameUICanvas.CreateScreenHost(layer, "Player Bullet Hit Flash");
        return hostRect.gameObject.AddComponent<PlayerBulletHitFlash>();
    }

    void Awake()
    {
        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Build()
    {
        _blackImage = CreateFlashImage("Black Flash", new Color(Blackout.r, Blackout.g, Blackout.b, 0f));
        _fireImage = CreateFlashImage("Fire Flash", new Color(FireOrange.r, FireOrange.g, FireOrange.b, 0f));
        _redImage = CreateFlashImage("Red Flash", new Color(RedFlash.r, RedFlash.g, RedFlash.b, 0f));
        _fireImage.raycastTarget = false;
        _redImage.raycastTarget = false;
        _blackImage.raycastTarget = false;
        SetVisible(false);
    }

    Image CreateFlashImage(string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(go.AddComponent<RectTransform>());
        var image = go.AddComponent<Image>();
        image.sprite = MenuUiFactory.WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void Update()
    {
        if (_fireRemaining > 0f)
        {
            TickFirePhase();
            return;
        }

        if (_remaining > 0f)
        {
            TickBlackPhase();
            return;
        }

        SetVisible(false);
    }

    void TickFirePhase()
    {
        _fireRemaining = Mathf.Max(0f, _fireRemaining - Time.deltaTime);
        float elapsed = Mathf.Max(0f, _fireTotal - _fireRemaining);
        float flicker = Mathf.Sin(elapsed * 34f) * 0.05f;
        float colorPulse = 0.5f + (Mathf.Sin(elapsed * 19f) * 0.5f);
        Color fireColor = Color.Lerp(FireOrange, FireRed, colorPulse);
        float alpha = Mathf.Clamp01(FireOverlayAlpha + flicker);

        SetVisible(true);
        _fireImage.gameObject.SetActive(true);
        _blackImage.color = new Color(Blackout.r, Blackout.g, Blackout.b, FireBlackAlpha);
        _fireImage.color = new Color(fireColor.r, fireColor.g, fireColor.b, alpha);
        _redImage.color = new Color(RedFlash.r, RedFlash.g, RedFlash.b, 0f);

        if (_fireRemaining > 0f)
        {
            return;
        }

        _fireTotal = 0f;
        _fireImage.gameObject.SetActive(false);
        _fireImage.color = new Color(FireOrange.r, FireOrange.g, FireOrange.b, 0f);

        if (_fireQueuedBlackRemaining > 0f)
        {
            BeginBlackPhase(_fireQueuedBlackRemaining, skipFadeIn: true);
            _fireQueuedBlackRemaining = 0f;
            RenderBlackPhase();
        }
        else
        {
            SetVisible(false);
        }
    }

    void TickBlackPhase()
    {
        _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
        if (_remaining <= 0f)
        {
            _skipFadeIn = false;
            SetVisible(false);
            return;
        }

        RenderBlackPhase();
    }

    void RenderBlackPhase()
    {
        ComputeBlindnessAlphas(
            _totalDuration,
            _remaining,
            _skipFadeIn,
            out float blackAlpha,
            out float redAlpha);

        SetVisible(true);
        _fireImage.gameObject.SetActive(false);
        _redImage.color = new Color(RedFlash.r, RedFlash.g, RedFlash.b, redAlpha);
        _blackImage.color = new Color(Blackout.r, Blackout.g, Blackout.b, blackAlpha);
    }

    static void ComputeBlindnessAlphas(float totalDuration, float remaining, bool skipFadeIn,
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

        if (!skipFadeIn && elapsed < fadeIn)
        {
            float t = fadeIn > 0f ? elapsed / fadeIn : 1f;
            float snap = t * t;
            redAlpha = (1f - snap) * RedPeakAlpha;
            blackAlpha = 1f;
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

    void SetVisible(bool visible)
    {
        if (_fireImage != null)
        {
            _fireImage.gameObject.SetActive(visible && _fireRemaining > 0f);
        }

        if (_redImage != null)
        {
            _redImage.gameObject.SetActive(visible);
        }

        if (_blackImage != null)
        {
            _blackImage.gameObject.SetActive(visible);
        }
    }

    public void Blind(float duration)
    {
        duration = ClampIncomingBlindDuration(duration);
        if (duration <= 0f)
        {
            return;
        }

        if (_fireRemaining > 0f)
        {
            _fireQueuedBlackRemaining = Mathf.Min(
                MaxBlindnessSeconds - _fireRemaining,
                Mathf.Max(_fireQueuedBlackRemaining, duration));
            return;
        }

        if (_remaining > 0f)
        {
            BeginBlackPhase(Mathf.Min(MaxBlindnessSeconds, duration), skipFadeIn: true);
            return;
        }

        BeginBlackPhase(duration, skipFadeIn: false);
    }

    public void BlindFromExplosionFire(float damageBlindDuration, bool inFire)
    {
        if (!inFire)
        {
            if (damageBlindDuration > 0f)
            {
                Blind(ClampIncomingBlindDuration(damageBlindDuration));
            }

            return;
        }

        float fireDuration = AntiMaterialExplosionEffect.FireDurationSeconds;
        float damageBlackAfter = Mathf.Max(0f, damageBlindDuration - fireDuration) * FireFollowupBlindnessMultiplier;
        float blackAfter = Mathf.Max(MinBlackAfterFireSeconds, damageBlackAfter);
        float activeFire = Mathf.Max(_fireRemaining, fireDuration);
        blackAfter = Mathf.Min(blackAfter, Mathf.Max(0f, MaxBlindnessSeconds - activeFire));

        _remaining = 0f;
        _fireRemaining = activeFire;
        _fireTotal = Mathf.Max(_fireTotal, _fireRemaining);
        _fireQueuedBlackRemaining = Mathf.Min(
            Mathf.Max(0f, MaxBlindnessSeconds - _fireRemaining),
            Mathf.Max(_fireQueuedBlackRemaining, blackAfter));
    }

    void BeginBlackPhase(float duration, bool skipFadeIn)
    {
        duration = ClampIncomingBlindDuration(duration);
        _remaining = duration;
        _totalDuration = duration;
        _skipFadeIn = skipFadeIn;
    }

    static float ClampIncomingBlindDuration(float duration)
    {
        return Mathf.Clamp(duration, 0f, MaxBlindnessSeconds);
    }
}
