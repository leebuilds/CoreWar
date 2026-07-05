using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Shared window chrome: title bar, optional header, body, footer, fade-in.
/// </summary>
public class MenuWindowFrame : MonoBehaviour
{
    public RectTransform Root { get; private set; }
    public RectTransform Header { get; private set; }
    public RectTransform Body { get; private set; }
    public Text FooterText { get; private set; }
    public Button BackButton { get; private set; }

    CanvasGroup _canvasGroup;
    Coroutine _fadeRoutine;

    public static MenuWindowFrame CreateScreen(Transform parent, string title, bool showBack,
        string footerText, Vector2 size, bool showHeader, UnityAction onBack)
    {
        return Create(parent, title, showBack, footerText, size, showHeader, onBack, modal: false);
    }

    public static MenuWindowFrame CreateModal(Transform parent, string title, bool showBack,
        string footerText, Vector2 size, UnityAction onBack)
    {
        return Create(parent, title, showBack, footerText, size, showHeader: false, onBack, modal: true);
    }

    static MenuWindowFrame Create(Transform parent, string title, bool showBack, string footerText,
        Vector2 size, bool showHeader, UnityAction onBack, bool modal)
    {
        Transform host = parent;
        if (modal)
        {
            host = MenuUiFactory.CreateModalOverlay(parent, 0.35f).transform;
        }

        var go = new GameObject($"Window_{title}");
        go.transform.SetParent(host, false);

        var frame = go.AddComponent<MenuWindowFrame>();
        frame.Build(title, showBack, footerText, size, showHeader, onBack);

        if (modal)
        {
            go.transform.SetAsLastSibling();
        }

        frame.PlayShowAnimation();
        return frame;
    }

    void Build(string title, bool showBack, string footerText, Vector2 size, bool showHeader, UnityAction onBack)
    {
        Root = gameObject.AddComponent<RectTransform>();
        Root.anchorMin = new Vector2(0.5f, 0.5f);
        Root.anchorMax = new Vector2(0.5f, 0.5f);
        Root.pivot = new Vector2(0.5f, 0.5f);
        Root.sizeDelta = size;

        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        CreateBorderFill();

        float headerHeight = showHeader ? MenuUiFactory.HeaderHeight : 0f;
        float topInset = MenuUiFactory.WindowBorderWidth + MenuUiFactory.TitleBarHeight + headerHeight;
        float bottomInset = MenuUiFactory.WindowBorderWidth + MenuUiFactory.FooterHeight;

        BuildTitleBar(title, showBack, onBack);

        if (showHeader)
        {
            Header = CreateBand("Header", MenuUiFactory.TitleBarHeight, headerHeight);
            MenuUiFactory.CreateDivider(Header, true);
        }

        FooterText = BuildFooter(footerText);

        Body = new GameObject("Body").AddComponent<RectTransform>();
        Body.transform.SetParent(transform, false);
        Body.anchorMin = Vector2.zero;
        Body.anchorMax = Vector2.one;
        Body.offsetMin = new Vector2(MenuUiFactory.WindowBorderWidth + MenuUiFactory.ContentPadding, bottomInset);
        Body.offsetMax = new Vector2(
            -(MenuUiFactory.WindowBorderWidth + MenuUiFactory.ContentPadding),
            -topInset);
    }

    void CreateBorderFill()
    {
        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(borderGo.AddComponent<RectTransform>());
        var borderImage = borderGo.AddComponent<Image>();
        borderImage.color = MenuUiFactory.Ink;
        borderImage.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        int inset = MenuUiFactory.WindowBorderWidth;
        fillRect.offsetMin = new Vector2(inset, inset);
        fillRect.offsetMax = new Vector2(-inset, -inset);
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = Color.white;
        fillImage.raycastTarget = false;
    }

    void BuildTitleBar(string title, bool showBack, UnityAction onBack)
    {
        var titleBar = CreateBand("Title Bar", 0f, MenuUiFactory.TitleBarHeight);

        var titleText = MenuUiFactory.CreateAnchoredText(titleBar, "Title", title.ToUpperInvariant(),
            MenuUiFactory.TitleFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, MenuUiFactory.Ink);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.04f, 0f);
        titleRect.anchorMax = new Vector2(0.8f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        if (showBack)
        {
            BackButton = MenuUiFactory.CreateTitleBarButton(titleBar, "Back", "←", onBack);
        }

        MenuUiFactory.CreateDivider(titleBar, false);
    }

    RectTransform CreateBand(string name, float topOffset, float height)
    {
        var band = new GameObject(name);
        band.transform.SetParent(transform, false);
        var rect = band.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = new Vector2(0f, -(MenuUiFactory.WindowBorderWidth + topOffset));
        return rect;
    }

    Text BuildFooter(string footerText)
    {
        var footer = new GameObject("Footer");
        footer.transform.SetParent(transform, false);
        var rect = footer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(0f, MenuUiFactory.FooterHeight);
        rect.anchoredPosition = new Vector2(0f, MenuUiFactory.WindowBorderWidth);

        MenuUiFactory.CreateDivider(footer.transform, true);

        var text = MenuUiFactory.CreateAnchoredText(footer.transform, "Footer Text", footerText ?? string.Empty,
            MenuUiFactory.FooterFontSize, FontStyle.Normal, TextAnchor.MiddleLeft, MenuUiFactory.MutedInk);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.04f, 0f);
        textRect.anchorMax = new Vector2(0.96f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return text;
    }

    public void SetFooterText(string text, bool isError = false)
    {
        if (FooterText == null)
        {
            return;
        }

        FooterText.text = text ?? string.Empty;
        FooterText.color = isError ? MenuUiFactory.Error : MenuUiFactory.MutedInk;
    }

    public void PlayShowAnimation()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < MenuUiFactory.WindowFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / MenuUiFactory.WindowFadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _fadeRoutine = null;
    }
}
