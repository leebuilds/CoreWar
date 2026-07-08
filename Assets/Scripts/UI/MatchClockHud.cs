using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match elapsed time HUD (gray box, white M:SS text).
/// </summary>
public class MatchClockHud : MonoBehaviour
{
    public static MatchClockHud Instance { get; private set; }

    Text _clockText;
    bool _visible = true;

    public static MatchClockHud Create()
    {
        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateLayer("Match Clock");
        var hostRect = GameUICanvas.CreateScreenHost(layer, "Match Clock HUD");
        var hud = hostRect.gameObject.AddComponent<MatchClockHud>();
        hud.Build();
        return hud;
    }

    void Build()
    {
        var boxGo = new GameObject("Clock Box");
        boxGo.transform.SetParent(transform, false);
        var boxRect = boxGo.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(1f, 1f);
        boxRect.anchorMax = new Vector2(1f, 1f);
        boxRect.pivot = new Vector2(1f, 1f);
        boxRect.sizeDelta = new Vector2(96f, 40f);
        boxRect.anchoredPosition = new Vector2(-16f, -16f);

        var bg = boxGo.AddComponent<Image>();
        bg.sprite = MenuUiFactory.WhiteSprite;
        bg.color = new Color(0.42f, 0.42f, 0.42f, 0.92f);
        bg.raycastTarget = false;

        _clockText = MenuUiFactory.CreateAnchoredText(boxGo.transform, "Clock", "0:00",
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        MenuUiFactory.StretchFull(_clockText.GetComponent<RectTransform>());
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
        if (!_visible || !GameSession.IsMatchActive)
        {
            return;
        }

        if (_clockText != null)
        {
            _clockText.text = GameSession.FormatMatchElapsedClock();
        }
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;
        gameObject.SetActive(visible);
    }
}
