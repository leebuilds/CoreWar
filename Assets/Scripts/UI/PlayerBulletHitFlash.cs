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
    const float GunshotFlickDuration = 0.1f;
    const float GunshotFlickPeakAlpha = 0.24f;

    static readonly Color RedFlash = new Color(0.82f, 0.08f, 0.08f, 1f);
    static readonly Color Blackout = new Color(0f, 0f, 0f, 1f);
    static readonly Color FireOrange = new Color(1f, 0.44f, 0.06f, 1f);
    static readonly Color FireRed = new Color(0.82f, 0.1f, 0.04f, 1f);

    Image _fireImage;
    Image _redImage;
    Image _blackImage;
    Image _whiteImage;
    float _remaining;
    float _totalDuration;
    bool _skipFadeIn;
    float _fireRemaining;
    float _fireTotal;
    float _fireQueuedBlackRemaining;
    float _flashbangRemaining;
    float _flashbangTotal;
    float _flashbangCompleteWhiteSeconds;
    float _flashbangFadeSeconds;
    float _flashbangPeakAlpha;
    float _gunshotFlickRemaining;
    float _gunshotFlickTotal;
    float _gunshotFlickIntensityScale = 1f;

    public bool IsBlind =>
        _fireRemaining > 0f ||
        _remaining > 0f ||
        _fireQueuedBlackRemaining > 0f ||
        _flashbangRemaining > 0f;

    public bool BlocksGameplayInput =>
        _fireRemaining > 0f ||
        _remaining > 0f ||
        _fireQueuedBlackRemaining > 0f;

    public void Clear()
    {
        _remaining = 0f;
        _totalDuration = 0f;
        _skipFadeIn = false;
        _fireRemaining = 0f;
        _fireTotal = 0f;
        _fireQueuedBlackRemaining = 0f;
        _flashbangRemaining = 0f;
        _flashbangTotal = 0f;
        _flashbangCompleteWhiteSeconds = 0f;
        _flashbangFadeSeconds = 0f;
        _flashbangPeakAlpha = 0f;
        _gunshotFlickRemaining = 0f;
        _gunshotFlickTotal = 0f;
        SetVisible(false);
    }

    public static PlayerBulletHitFlash Create()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Hit Flash", 300);
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
        _whiteImage = CreateFlashImage("White Flash", new Color(1f, 0.98f, 0.94f, 0f));
        _blackImage = CreateFlashImage("Black Flash", new Color(Blackout.r, Blackout.g, Blackout.b, 0f));
        _fireImage = CreateFlashImage("Fire Flash", new Color(FireOrange.r, FireOrange.g, FireOrange.b, 0f));
        _redImage = CreateFlashImage("Red Flash", new Color(RedFlash.r, RedFlash.g, RedFlash.b, 0f));
        _fireImage.raycastTarget = false;
        _redImage.raycastTarget = false;
        _blackImage.raycastTarget = false;
        _whiteImage.raycastTarget = false;
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
        bool flashbangActive = TickFlashbangPhase();
        bool gunshotFlickActive = TickGunshotFlickPhase();
        bool fireActive = TickFirePhase();
        bool blackActive = TickBlackPhase();

        if (!flashbangActive && !gunshotFlickActive && !fireActive && !blackActive)
        {
            SetVisible(false);
            return;
        }

        RenderComposite(flashbangActive, gunshotFlickActive, fireActive, blackActive);
    }

    bool TickGunshotFlickPhase()
    {
        if (_gunshotFlickRemaining <= 0f)
        {
            return false;
        }

        _gunshotFlickRemaining = Mathf.Max(0f, _gunshotFlickRemaining - Time.deltaTime);
        if (_gunshotFlickRemaining <= 0f)
        {
            _gunshotFlickTotal = 0f;
            _gunshotFlickIntensityScale = 1f;
            return false;
        }

        return true;
    }

    float CurrentGunshotFlickAlpha()
    {
        if (_gunshotFlickRemaining <= 0f || _gunshotFlickTotal <= 0f)
        {
            return 0f;
        }

        float elapsed = _gunshotFlickTotal - _gunshotFlickRemaining;
        float t = Mathf.Clamp01(elapsed / _gunshotFlickTotal);
        return Mathf.Sin(t * Mathf.PI) * GunshotFlickPeakAlpha * _gunshotFlickIntensityScale;
    }

    public void FlickFromGunshot(float intensityScale = 1f)
    {
        _gunshotFlickIntensityScale = Mathf.Max(1f, intensityScale);
        _gunshotFlickTotal = GunshotFlickDuration;
        _gunshotFlickRemaining = GunshotFlickDuration;
    }

    bool TickFlashbangPhase()
    {
        if (_flashbangRemaining <= 0f)
        {
            return false;
        }

        _flashbangRemaining = Mathf.Max(0f, _flashbangRemaining - Time.deltaTime);
        if (_flashbangRemaining <= 0f)
        {
            _flashbangTotal = 0f;
            _flashbangCompleteWhiteSeconds = 0f;
            _flashbangFadeSeconds = 0f;
            _flashbangPeakAlpha = 0f;
            return false;
        }

        return true;
    }

    float CurrentFlashbangAlpha()
    {
        if (_flashbangRemaining <= 0f)
        {
            return 0f;
        }

        float elapsed = _flashbangTotal - _flashbangRemaining;
        if (elapsed < _flashbangCompleteWhiteSeconds)
        {
            return 1f;
        }

        float fadeElapsed = elapsed - _flashbangCompleteWhiteSeconds;
        float fadeT = _flashbangFadeSeconds > 0f
            ? Mathf.Clamp01(fadeElapsed / _flashbangFadeSeconds)
            : 1f;
        return Mathf.Clamp01(_flashbangPeakAlpha * (1f - fadeT));
    }

    void RenderComposite(bool flashbangActive, bool gunshotFlickActive, bool fireActive, bool blackActive)
    {
        if (transform.parent != null)
        {
            GameUICanvas.BringLayerToFront(transform.parent as RectTransform);
        }

        float whiteAlpha = flashbangActive ? CurrentFlashbangAlpha() : 0f;
        float blackAlpha = 0f;
        float redAlpha = gunshotFlickActive ? CurrentGunshotFlickAlpha() : 0f;
        float fireAlpha = 0f;
        Color fireColor = FireOrange;

        if (fireActive)
        {
            float elapsed = Mathf.Max(0f, _fireTotal - _fireRemaining);
            float flicker = Mathf.Sin(elapsed * 34f) * 0.05f;
            float colorPulse = 0.5f + (Mathf.Sin(elapsed * 19f) * 0.5f);
            fireColor = Color.Lerp(FireOrange, FireRed, colorPulse);
            fireAlpha = Mathf.Clamp01(FireOverlayAlpha + flicker);
            blackAlpha = FireBlackAlpha;
            redAlpha = 0f;
        }
        else if (blackActive)
        {
            ComputeBlindnessAlphas(
                _totalDuration,
                _remaining,
                _skipFadeIn,
                out blackAlpha,
                out float blindnessRedAlpha);
            redAlpha = Mathf.Max(redAlpha, blindnessRedAlpha);
        }

        SetVisible(whiteAlpha > 0f || blackAlpha > 0f || redAlpha > 0f || fireAlpha > 0f);

        _whiteImage.gameObject.SetActive(whiteAlpha > 0f);
        _whiteImage.color = new Color(1f, 1f, 0.98f, whiteAlpha);

        _blackImage.gameObject.SetActive(blackAlpha > 0f);
        _blackImage.color = new Color(Blackout.r, Blackout.g, Blackout.b, blackAlpha);

        _fireImage.gameObject.SetActive(fireAlpha > 0f);
        _fireImage.color = new Color(fireColor.r, fireColor.g, fireColor.b, fireAlpha);

        _redImage.gameObject.SetActive(redAlpha > 0f);
        _redImage.color = new Color(RedFlash.r, RedFlash.g, RedFlash.b, redAlpha);

        _whiteImage.transform.SetSiblingIndex(0);
        _blackImage.transform.SetSiblingIndex(1);
        _fireImage.transform.SetSiblingIndex(2);
        _redImage.transform.SetAsLastSibling();
    }

    bool TickFirePhase()
    {
        if (_fireRemaining <= 0f)
        {
            return false;
        }

        _fireRemaining = Mathf.Max(0f, _fireRemaining - Time.deltaTime);
        if (_fireRemaining > 0f)
        {
            return true;
        }

        _fireTotal = 0f;
        if (_fireQueuedBlackRemaining > 0f)
        {
            BeginBlackPhase(_fireQueuedBlackRemaining, skipFadeIn: true);
            _fireQueuedBlackRemaining = 0f;
        }

        return false;
    }

    bool TickBlackPhase()
    {
        if (_remaining <= 0f)
        {
            return false;
        }

        _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
        if (_remaining <= 0f)
        {
            _skipFadeIn = false;
            return false;
        }

        return true;
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

        if (_whiteImage != null)
        {
            _whiteImage.gameObject.SetActive(visible && _flashbangRemaining > 0f);
        }
    }

    public void BlindFromFlashbang(float completeWhiteSeconds, float fadeSeconds, float peakAlpha)
    {
        completeWhiteSeconds = Mathf.Max(0f, completeWhiteSeconds);
        fadeSeconds = Mathf.Max(0f, fadeSeconds);
        peakAlpha = Mathf.Clamp01(peakAlpha);
        if (fadeSeconds <= 0f || peakAlpha <= 0f)
        {
            return;
        }

        float totalDuration = completeWhiteSeconds + fadeSeconds;
        if (_flashbangRemaining > 0f)
        {
            if (totalDuration <= _flashbangTotal)
            {
                return;
            }

            float elapsed = _flashbangTotal - _flashbangRemaining;
            _flashbangCompleteWhiteSeconds = completeWhiteSeconds;
            _flashbangFadeSeconds = fadeSeconds;
            _flashbangPeakAlpha = peakAlpha;
            _flashbangTotal = totalDuration;
            _flashbangRemaining = Mathf.Max(_flashbangRemaining, totalDuration - elapsed);
            return;
        }

        _flashbangCompleteWhiteSeconds = completeWhiteSeconds;
        _flashbangFadeSeconds = fadeSeconds;
        _flashbangPeakAlpha = peakAlpha;
        _flashbangTotal = totalDuration;
        _flashbangRemaining = totalDuration;
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
