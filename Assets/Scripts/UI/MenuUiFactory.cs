using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared runtime UI styling for menu screens.
/// </summary>
public static class MenuUiFactory
{
    static readonly Color LightBackground = new Color(0.97f, 0.97f, 0.97f);
    static readonly Color LightInk = new Color(0.08f, 0.08f, 0.08f);
    static readonly Color LightMutedInk = new Color(0.35f, 0.35f, 0.35f);
    static readonly Color LightPanelFill = Color.white;
    static readonly Color LightDisabledFill = new Color(0.92f, 0.92f, 0.92f);
    static readonly Color LightScrollViewport = new Color(0.98f, 0.98f, 0.98f);

    static readonly Color DarkBackground = Color.black;
    static readonly Color DarkInk = new Color(0.94f, 0.94f, 0.95f);
    static readonly Color DarkMutedInk = new Color(0.68f, 0.68f, 0.7f);
    static readonly Color DarkPanelFill = new Color(0.12f, 0.12f, 0.13f);
    static readonly Color DarkDisabledFill = new Color(0.18f, 0.18f, 0.19f);
    static readonly Color DarkScrollViewport = new Color(0.1f, 0.1f, 0.11f);

    public static Color Background
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkBackground : LightBackground;
        }
    }

    public static Color Ink
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkInk : LightInk;
        }
    }

    public static Color MutedInk
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkMutedInk : LightMutedInk;
        }
    }

    public static Color PanelFill
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkPanelFill : LightPanelFill;
        }
    }

    public static Color ButtonFill => PanelFill;
    public static Color InputFill => PanelFill;
    public static Color ScrollViewportFill
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkScrollViewport : LightScrollViewport;
        }
    }

    public static Color DisabledFill
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkDisabledFill : LightDisabledFill;
        }
    }

    public static Color OnInk
    {
        get
        {
            MenuSettings.EnsureLoaded();
            return MenuSettings.IsDarkMode ? DarkBackground : Color.white;
        }
    }

    public static readonly Color Disabled = new Color(0.55f, 0.55f, 0.55f);
    public static readonly Color LoadoutOutline = new Color(0.15f, 0.78f, 0.28f);
    public static readonly Color Error = new Color(0.75f, 0.12f, 0.12f);

    public const int WindowBorderWidth = 2;
    public const float TitleBarHeight = 56f;
    public const float HeaderHeight = 108f;
    public const float FooterHeight = 52f;
    public const float ContentPadding = 16f;
    public const int TitleFontSize = 28;
    public const int BodyFontSize = 24;
    public const int FooterFontSize = 20;
    public const int LinkFontSize = 24;
    public const int ButtonFontSize = 26;
    public const int InputFontSize = 24;
    public const int HintFontSize = 22;
    public const int SectionFontSize = 20;
    public const int SmallFontSize = 20;
    public const float WindowFadeDuration = 0.12f;
    public const float CardHoverScale = 1.04f;

    static Font _font;

    public static Font Font
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _font;
        }
    }

    public static Canvas CreateCanvas(string name, out RectTransform root)
    {
        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        root = canvasGo.GetComponent<RectTransform>();
        return canvas;
    }

    public static GameObject CreateModalOverlay(Transform parent, float dimAlpha)
    {
        var overlayGo = new GameObject("Modal Overlay");
        overlayGo.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(overlayGo.AddComponent<RectTransform>());

        var dim = new GameObject("Dim");
        dim.transform.SetParent(overlayGo.transform, false);
        var dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, dimAlpha);
        StretchFull(dim.GetComponent<RectTransform>());

        return overlayGo;
    }

    public static void CreateDivider(Transform parent, bool top)
    {
        var dividerGo = new GameObject(top ? "Divider Top" : "Divider Bottom");
        dividerGo.transform.SetParent(parent, false);
        var rect = dividerGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.sizeDelta = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        dividerGo.AddComponent<Image>().color = Ink;
    }

    public static Button CreateTitleBarButton(Transform parent, string name, string label, UnityAction onClick)
    {
        var button = CreateBorderedButton(parent, name, label, new Vector2(40f, 40f), Vector2.zero,
            anchorRight: true, onClick, enabled: true, anchored: false);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-12f, 0f);
        return button;
    }

    public static Text CreateText(Transform parent, string name, string content,
        int fontSize, FontStyle style, TextAnchor alignment,
        Vector2 anchoredPos, Vector2 size, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color ?? Ink;
        text.alignment = alignment;
        text.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return text;
    }

    public static Text CreateAnchoredText(Transform parent, string name, string content,
        int fontSize, FontStyle style, TextAnchor alignment, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color ?? Ink;
        text.alignment = alignment;
        text.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, UnityAction onClick, bool enabled = true)
    {
        return CreateBorderedButton(parent, name, label, size, anchoredPos, anchorRight: false,
            onClick, enabled, anchored: true, wireSounds: true);
    }

    public static Button CreateSettingsButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, UnityAction onClick, bool enabled = true)
    {
        return CreateBorderedButton(parent, name, label, size, anchoredPos, anchorRight: false,
            onClick, enabled, anchored: true, wireSounds: false);
    }

    static Button CreateBorderedButton(Transform parent, string name, string label, Vector2 size,
        Vector2 anchoredPos, bool anchorRight, UnityAction onClick, bool enabled, bool anchored,
        bool wireSounds = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = go.AddComponent<RectTransform>();
        }

        if (anchored)
        {
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
        }
        else
        {
            rect.sizeDelta = size;
        }

        var borderImage = go.AddComponent<Image>();
        borderImage.color = enabled ? Ink : Disabled;

        var button = go.AddComponent<Button>();
        button.targetGraphic = borderImage;
        button.interactable = enabled;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        var innerImage = innerGo.AddComponent<Image>();
        innerImage.color = enabled ? ButtonFill : DisabledFill;

        CreateAnchoredText(innerGo.transform, "Label", label, ButtonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter,
            enabled ? Ink : Disabled);

        if (wireSounds)
        {
            MenuUiSounds.WireButton(button);
        }
        else
        {
            DisableSelectableFeedback(button);
        }

        return button;
    }

    public static void DisableSelectableFeedback(Selectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        selectable.transition = Selectable.Transition.None;
    }

    static void ConfigureSlider(Slider slider, Image handleImage)
    {
        slider.direction = Slider.Direction.LeftToRight;
        slider.transition = Selectable.Transition.None;
        handleImage.raycastTarget = true;
    }

    public static Button CreateBodyButton(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, UnityAction onClick, bool enabled = true)
    {
        return CreateButton(parent, name, label, anchoredPos, size, onClick, enabled);
    }

    public static Button CreateTextLink(Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 size, UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = Color.clear;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        CreateAnchoredText(go.transform, "Label", label, LinkFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Ink);
        MenuUiSounds.WireButton(button);
        return button;
    }

    public static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 size,
        float minValue, float maxValue, float value, UnityAction<float> onValueChanged)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(go.transform, false);
        MenuUiFactory.StretchFull(backgroundGo.AddComponent<RectTransform>());
        var backgroundImage = backgroundGo.AddComponent<Image>();
        backgroundImage.color = MutedInk;
        backgroundImage.raycastTarget = true;

        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(fillAreaRect);

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = Ink;

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(go.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 28f);
        var handleImage = handleGo.AddComponent<Image>();
        handleImage.color = PanelFill;

        var slider = go.AddComponent<Slider>();
        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        ConfigureSlider(slider, handleImage);
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }

    public static Slider CreateStretchedSlider(Transform parent, string name,
        float minValue, float maxValue, float value, UnityAction<float> onValueChanged)
    {
        const float trackHeight = 28f;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, trackHeight);

        var backgroundGo = new GameObject("Background");
        backgroundGo.transform.SetParent(go.transform, false);
        StretchFull(backgroundGo.AddComponent<RectTransform>());
        var backgroundImage = backgroundGo.AddComponent<Image>();
        backgroundImage.color = MutedInk;
        backgroundImage.raycastTarget = true;

        var fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(go.transform, false);
        StretchFull(fillAreaGo.AddComponent<RectTransform>());

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = Ink;
        fillImage.raycastTarget = false;

        var handleGo = new GameObject("Handle");
        handleGo.transform.SetParent(go.transform, false);
        var handleRect = handleGo.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(0f, 1f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(28f, 0f);
        var handleImage = handleGo.AddComponent<Image>();
        handleImage.color = PanelFill;

        var slider = go.AddComponent<Slider>();
        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = value;
        ConfigureSlider(slider, handleImage);
        slider.onValueChanged.AddListener(onValueChanged);
        return slider;
    }

    public static InputField CreateInputField(Transform parent, string name, string placeholder,
        Vector2 anchoredPos, Vector2 size, bool password = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var border = go.AddComponent<Image>();
        border.color = Ink;

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        innerGo.AddComponent<Image>().color = InputFill;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(innerGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Font;
        text.fontSize = InputFontSize;
        text.color = Ink;
        text.supportRichText = false;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        var placeholderGo = new GameObject("Placeholder");
        placeholderGo.transform.SetParent(innerGo.transform, false);
        var placeholderText = placeholderGo.AddComponent<Text>();
        placeholderText.font = Font;
        placeholderText.fontSize = InputFontSize;
        placeholderText.color = MutedInk;
        placeholderText.text = placeholder;
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.raycastTarget = false;

        var placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12f, 6f);
        placeholderRect.offsetMax = new Vector2(-12f, -6f);

        var input = go.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholderText;
        if (password)
        {
            input.contentType = InputField.ContentType.Password;
        }

        return input;
    }

    public static void ApplyCardHover(CardTileView tile)
    {
        if (tile == null)
        {
            return;
        }

        var hover = tile.gameObject.GetComponent<CardTileHover>();
        if (hover == null)
        {
            hover = tile.gameObject.AddComponent<CardTileHover>();
        }

        hover.Initialize(tile);
    }

    public static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

/// <summary>
/// Forwards mouse-wheel scroll to the nearest parent ScrollRect.
/// </summary>
public class ScrollWheelForwarder : MonoBehaviour, IScrollHandler
{
    public void OnScroll(PointerEventData eventData)
    {
        var scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.OnScroll(eventData);
        }
    }
}

/// <summary>
/// Scale + shadow feedback for collection card tiles.
/// </summary>
public class CardTileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    CardTileView _tile;
    GameObject _shadow;
    Vector3 _baseScale = Vector3.one;
    bool _initialized;

    public void Initialize(CardTileView tile)
    {
        _tile = tile;
        _baseScale = transform.localScale;
        EnsureShadow();
        _initialized = true;
    }

    void EnsureShadow()
    {
        if (_shadow != null)
        {
            return;
        }

        _shadow = new GameObject("Hover Shadow");
        _shadow.transform.SetParent(transform, false);
        _shadow.transform.SetAsFirstSibling();

        var rect = _shadow.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, -6f);
        rect.offsetMax = new Vector2(8f, -2f);

        var image = _shadow.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.18f);
        image.raycastTarget = false;
        _shadow.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_initialized || _tile == null || _tile.Locked)
        {
            return;
        }

        transform.localScale = _baseScale * MenuUiFactory.CardHoverScale;
        if (_shadow != null)
        {
            _shadow.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_initialized)
        {
            return;
        }

        transform.localScale = _baseScale;
        if (_shadow != null)
        {
            _shadow.SetActive(false);
        }
    }
}
