using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen blindness when the local player is struck. Hold is pitch black;
/// fade in and fade out pass through red. Re-hits while already blind skip the flash.
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

    Image _redImage;
    Image _blackImage;
    float _remaining;
    float _totalDuration;
    bool _skipFadeIn;

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
        _redImage = CreateFlashImage("Red Flash", new Color(RedFlash.r, RedFlash.g, RedFlash.b, 0f));
        _blackImage = CreateFlashImage("Black Flash", new Color(Blackout.r, Blackout.g, Blackout.b, 0f));
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
        if (_remaining > 0f)
        {
            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            if (_remaining <= 0f)
            {
                _skipFadeIn = false;
                SetVisible(false);
                return;
            }
        }

        if (_remaining <= 0f)
        {
            return;
        }

        ComputeAlphas(
            _totalDuration,
            _remaining,
            _skipFadeIn,
            out float blackAlpha,
            out float redAlpha);

        SetVisible(true);
        _redImage.color = new Color(RedFlash.r, RedFlash.g, RedFlash.b, redAlpha);
        _blackImage.color = new Color(Blackout.r, Blackout.g, Blackout.b, blackAlpha);
    }

    void SetVisible(bool visible)
    {
        if (_redImage != null)
        {
            _redImage.gameObject.SetActive(visible);
        }

        if (_blackImage != null)
        {
            _blackImage.gameObject.SetActive(visible);
        }
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
