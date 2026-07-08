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
    public static readonly Color LoadoutOutline = new Color(0.10f, 0.52f, 0.22f);
    public static readonly Color Error = new Color(0.62f, 0.10f, 0.10f);

    // Military title-bar chrome (dark olive panel, metal crease, corner nails).
    static readonly Color DarkMilitaryPanelBorder = new Color(0.06f, 0.11f, 0.07f);
    static readonly Color LightMilitaryPanelBorder = new Color(0.09f, 0.16f, 0.10f);
    static readonly Color DarkMilitaryPanelFill = new Color(0.13f, 0.22f, 0.15f);
    static readonly Color LightMilitaryPanelFill = new Color(0.19f, 0.32f, 0.21f);
    static readonly Color DarkMilitaryPanelHighlight = new Color(0.18f, 0.28f, 0.19f);
    static readonly Color LightMilitaryPanelHighlight = new Color(0.24f, 0.36f, 0.25f);
    static readonly Color DarkMilitaryCreaseHighlight = new Color(0.34f, 0.40f, 0.30f, 0.75f);
    static readonly Color LightMilitaryCreaseHighlight = new Color(0.40f, 0.48f, 0.36f, 0.75f);
    static readonly Color DarkMilitaryCreaseShadow = new Color(0.04f, 0.07f, 0.04f, 0.85f);
    static readonly Color LightMilitaryCreaseShadow = new Color(0.07f, 0.11f, 0.07f, 0.85f);
    static readonly Color DarkMilitaryTitleInk = new Color(0.84f, 0.86f, 0.76f);
    static readonly Color LightMilitaryTitleInk = new Color(0.90f, 0.92f, 0.82f);
    static readonly Color MilitaryNailFill = new Color(0.36f, 0.30f, 0.18f);
    static readonly Color MilitaryNailRim = new Color(0.20f, 0.16f, 0.10f);

    public static Color MilitaryPanelBorder => SelectMilitary(DarkMilitaryPanelBorder, LightMilitaryPanelBorder);
    public static Color MilitaryPanelFill => SelectMilitary(DarkMilitaryPanelFill, LightMilitaryPanelFill);
    public static Color MilitaryPanelHighlight => SelectMilitary(DarkMilitaryPanelHighlight, LightMilitaryPanelHighlight);
    public static Color MilitaryCreaseHighlight => SelectMilitary(DarkMilitaryCreaseHighlight, LightMilitaryCreaseHighlight);
    public static Color MilitaryCreaseShadow => SelectMilitary(DarkMilitaryCreaseShadow, LightMilitaryCreaseShadow);
    public static Color MilitaryTitleInk => SelectMilitary(DarkMilitaryTitleInk, LightMilitaryTitleInk);

    static Color SelectMilitary(Color dark, Color light)
    {
        MenuSettings.EnsureLoaded();
        return MenuSettings.IsDarkMode ? dark : light;
    }

    public const int WindowBorderWidth = 2;
    public const float TitleBarHeight = 56f;
    public const float HeaderHeight = 108f;
    public const float FooterHeight = 52f;
    public const float ContentPadding = 16f;
    public const float BackButtonSize = 40f;
    public const float MilitaryNailSize = 6f;
    public const float SliderTrackHeight = 28f;
    public const float SettingsLabeledRowHeight = 58f;
    public const float SectionLabelHeight = 36f;

    public const float StandardButtonWidth = 320f;
    public const float StandardButtonHeight = 64f;
    public const float CompactControlHeight = 52f;
    public const float StandardInputWidth = 420f;
    public const float TextLinkHeight = 44f;
    public const float PrimaryButtonHeight = 80f;
    public const float ModalSplitButtonWidth = 220f;
    public const float ModalSplitButtonOffset = 230f;
    public const float SettingsToggleWidth = 120f;

    public static float BackButtonInset => ContentPadding;
    public static float TitleBarVerticalInset => (TitleBarHeight - BackButtonSize) * 0.5f;
    public static float MilitaryNailInset => ContentPadding * 0.5f;
    public static float SettingsRowSpacing => CompactControlHeight + ContentPadding;
    public static float SettingsSliderRowSpacing => SettingsLabeledRowHeight + ContentPadding;
    public static float SectionLabelSpacing => SectionLabelHeight + ContentPadding * 0.5f;
    public static int BackButtonFontSize => ButtonFontSize;
    public static float InputTextPaddingH => ContentPadding * 0.75f;
    public static float InputTextPaddingV => ContentPadding * 0.375f;
    public static Vector2 StandardButtonSize => new Vector2(StandardButtonWidth, StandardButtonHeight);
    public static Vector2 CompactButtonSize => new Vector2(200f, CompactControlHeight);
    public static Vector2 StandardInputSize => new Vector2(StandardInputWidth, CompactControlHeight);
    public static Vector2 TextLinkSize => new Vector2(StandardButtonWidth, TextLinkHeight);
    public static Vector2 PrimaryButtonSize => new Vector2(StandardButtonWidth, PrimaryButtonHeight);
    public static Vector2 ModalSplitButtonSize => new Vector2(ModalSplitButtonWidth, CompactControlHeight);
    public static Vector2 SettingsToggleSize => new Vector2(SettingsToggleWidth, CompactControlHeight);
    public static float ThemeButtonOffset => CompactButtonSize.x * 0.5f + ContentPadding * 0.625f;
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
    static Sprite _whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (_whiteSprite == null)
            {
                var texture = Texture2D.whiteTexture;
                _whiteSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                _whiteSprite.name = "UI White Sprite";
            }

            return _whiteSprite;
        }
    }

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

        CreateFullscreenDim(overlayGo.transform, dimAlpha);

        return overlayGo;
    }

    public static Image CreateFullscreenDim(Transform parent, float dimAlpha, bool blockRaycasts = true)
    {
        var dim = new GameObject("Dim");
        dim.transform.SetParent(parent, false);
        dim.transform.SetAsFirstSibling();
        var dimImage = dim.AddComponent<Image>();
        dimImage.sprite = WhiteSprite;
        dimImage.color = new Color(0f, 0f, 0f, dimAlpha);
        dimImage.raycastTarget = blockRaycasts;
        StretchFull(dim.GetComponent<RectTransform>());
        return dimImage;
    }

    public static void CreateDivider(Transform parent, bool top)
    {
        var dividerGo = new GameObject(top ? "Divider Top" : "Divider Bottom");
        dividerGo.transform.SetParent(parent, false);
        var rect = dividerGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.sizeDelta = new Vector2(0f, WindowBorderWidth);
        rect.anchoredPosition = Vector2.zero;
        dividerGo.AddComponent<Image>().color = MilitaryPanelBorder;
    }

    /// <summary>
    /// Dark-green military title strip with crease, corner nails, screen label, and optional back control.
    /// </summary>
    public static RectTransform BuildMilitaryTitleBar(Transform windowRoot, float topOffset, string title,
        bool showBack, UnityAction onBack, out Button backButton)
    {
        backButton = null;

        var bar = new GameObject("Military Title Bar");
        bar.transform.SetParent(windowRoot, false);
        var barRect = bar.AddComponent<RectTransform>();
        ApplyTopChromeBand(barRect, topOffset, TitleBarHeight);

        var borderImage = bar.AddComponent<Image>();
        borderImage.color = MilitaryPanelBorder;
        borderImage.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bar.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        ApplyInnerBorder(fillRect);
        fillGo.AddComponent<Image>().color = MilitaryPanelFill;

        CreateMilitaryNails(fillGo.transform);
        CreateMilitaryCrease(fillGo.transform);

        var titleText = CreateAnchoredText(fillGo.transform, "Title", title.ToUpperInvariant(),
            TitleFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, MilitaryTitleInk);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(ContentPadding, 0f);
        titleRect.offsetMax = new Vector2(-TitleTextRightInset(showBack), 0f);

        if (showBack && onBack != null)
        {
            backButton = CreateMilitaryBackButton(fillGo.transform, onBack);
        }

        return barRect;
    }

    public static Button CreateTitleBarButton(Transform parent, string name, string label, UnityAction onClick)
    {
        return CreateMilitaryBackButton(parent, onClick);
    }

    static Button CreateMilitaryBackButton(Transform parent, UnityAction onClick)
    {
        var go = new GameObject("Back");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-BackButtonInset, 0f);
        rect.sizeDelta = new Vector2(BackButtonSize, BackButtonSize);

        var borderImage = go.AddComponent<Image>();
        borderImage.sprite = WhiteSprite;
        borderImage.color = MilitaryPanelBorder;

        var button = go.AddComponent<Button>();
        button.targetGraphic = borderImage;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        ApplyInnerBorder(innerRect);
        innerGo.AddComponent<Image>().color = MilitaryPanelHighlight;

        CreateAnchoredText(innerGo.transform, "Label", "←", BackButtonFontSize, FontStyle.Bold,
            TextAnchor.MiddleCenter, MilitaryTitleInk);

        MenuUiSounds.WireButton(button);
        return button;
    }

    static void CreateMilitaryCrease(Transform parent)
    {
        CreateHorizontalLine(parent, "Crease Highlight", 0.40f, MilitaryCreaseHighlight);
        CreateHorizontalLine(parent, "Crease Shadow", 0.37f, MilitaryCreaseShadow);
    }

    static void CreateHorizontalLine(Transform parent, string name, float anchorY, Color color)
    {
        var lineGo = new GameObject(name);
        lineGo.transform.SetParent(parent, false);
        var rect = lineGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(1f, anchorY);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(-ContentPadding * 2f, WindowBorderWidth);
        rect.anchoredPosition = Vector2.zero;

        var image = lineGo.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static void CreateMilitaryNails(Transform parent)
    {
        CreateMilitaryNail(parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(MilitaryNailInset, -MilitaryNailInset), MilitaryNailSize);
        CreateMilitaryNail(parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-MilitaryNailInset, -MilitaryNailInset), MilitaryNailSize);
        CreateMilitaryNail(parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(MilitaryNailInset, MilitaryNailInset), MilitaryNailSize);
        CreateMilitaryNail(parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-MilitaryNailInset, MilitaryNailInset), MilitaryNailSize);
    }

    static void CreateMilitaryNail(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, float size)
    {
        var nailGo = new GameObject("Nail");
        nailGo.transform.SetParent(parent, false);
        var rect = nailGo.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = anchoredPosition;

        var rim = nailGo.AddComponent<Image>();
        rim.color = MilitaryNailRim;
        rim.raycastTarget = false;

        var headGo = new GameObject("Head");
        headGo.transform.SetParent(nailGo.transform, false);
        var headRect = headGo.AddComponent<RectTransform>();
        SetRectInset(headRect, 1f);
        headGo.AddComponent<Image>().color = MilitaryNailFill;
    }

    static void SetRectInset(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static void ApplyInnerBorder(RectTransform rect)
    {
        SetRectInset(rect, WindowBorderWidth);
    }

    /// <summary>
    /// Top band aligned with the window fill (inset by <see cref="WindowBorderWidth"/> on left/right).
    /// </summary>
    public static void ApplyTopChromeBand(RectTransform rect, float topOffset, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -(WindowBorderWidth + topOffset));
        rect.sizeDelta = new Vector2(-WindowBorderWidth * 2f, height);
    }

    /// <summary>
    /// Bottom band aligned with the window fill (inset by <see cref="WindowBorderWidth"/> on left/right).
    /// </summary>
    public static void ApplyBottomChromeBand(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, WindowBorderWidth);
        rect.sizeDelta = new Vector2(-WindowBorderWidth * 2f, height);
    }

    public static void ApplyHorizontalPadding(RectTransform rect, float left, float right)
    {
        rect.offsetMin = new Vector2(left, rect.offsetMin.y);
        rect.offsetMax = new Vector2(-right, rect.offsetMax.y);
    }

    static float TitleTextRightInset(bool showBack)
    {
        return showBack ? BackButtonInset + BackButtonSize + ContentPadding * 0.5f : ContentPadding;
    }

    /// <summary>
    /// Anchors a control to the right edge using the same inset as the title-bar back button.
    /// </summary>
    public static void ApplyRightAlignedControl(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-BackButtonInset, 0f);
    }

    public static Button CreateSettingsButtonRight(Transform parent, string name, string label,
        Vector2 size, UnityAction onClick)
    {
        var button = CreateSettingsButton(parent, name, label, Vector2.zero, size, onClick);
        ApplyRightAlignedControl(button.GetComponent<RectTransform>());
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
        borderImage.sprite = WhiteSprite;
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
        ApplyInnerBorder(innerRect);
        var innerImage = innerGo.AddComponent<Image>();
        innerImage.sprite = WhiteSprite;
        innerImage.color = enabled ? ButtonFill : DisabledFill;

        CreateAnchoredText(innerGo.transform, "Label", label, ButtonFontSize, FontStyle.Bold, TextAnchor.MiddleCenter,
            enabled ? Ink : Disabled);

        if (wireSounds)
        {
            MenuUiSounds.WireButton(button);
        }
        else
        {
            ApplyButtonHover(button);
        }

        return button;
    }

    /// <summary>
    /// Settings controls: hover scale like menu buttons, but no press tint or UI sounds.
    /// </summary>
    public static void ApplyButtonHover(Button button)
    {
        if (button == null)
        {
            return;
        }

        DisableSelectableFeedback(button);

        if (button.GetComponent<MenuButtonHover>() == null)
        {
            button.gameObject.AddComponent<MenuButtonHover>();
        }
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
        handleRect.sizeDelta = new Vector2(SliderTrackHeight - 4f, SliderTrackHeight);
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
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(ContentPadding, 0f);
        rect.offsetMax = new Vector2(-ContentPadding, SliderTrackHeight);

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
        handleRect.sizeDelta = new Vector2(SliderTrackHeight, 0f);
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
        border.sprite = WhiteSprite;
        border.color = Ink;

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(go.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        ApplyInnerBorder(innerRect);
        var innerImage = innerGo.AddComponent<Image>();
        innerImage.sprite = WhiteSprite;
        innerImage.color = InputFill;

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
        textRect.offsetMin = new Vector2(InputTextPaddingH, InputTextPaddingV);
        textRect.offsetMax = new Vector2(-InputTextPaddingH, -InputTextPaddingV);

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
        placeholderRect.offsetMin = new Vector2(InputTextPaddingH, InputTextPaddingV);
        placeholderRect.offsetMax = new Vector2(-InputTextPaddingH, -InputTextPaddingV);

        var input = go.AddComponent<InputField>();
        input.targetGraphic = border;
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

    public static void AddButtonLockIcon(Transform buttonRoot)
    {
        var inner = buttonRoot.Find("Inner");
        if (inner == null)
        {
            return;
        }

        var iconRoot = new GameObject("Lock Icon");
        iconRoot.transform.SetParent(inner, false);
        var iconRect = iconRoot.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-10f, 0f);
        iconRect.sizeDelta = new Vector2(18f, 22f);

        var shackleGo = new GameObject("Shackle");
        shackleGo.transform.SetParent(iconRoot.transform, false);
        var shackleRect = shackleGo.AddComponent<RectTransform>();
        shackleRect.anchorMin = new Vector2(0.5f, 1f);
        shackleRect.anchorMax = new Vector2(0.5f, 1f);
        shackleRect.pivot = new Vector2(0.5f, 1f);
        shackleRect.sizeDelta = new Vector2(12f, 9f);
        shackleRect.anchoredPosition = new Vector2(0f, -1f);
        shackleGo.AddComponent<Image>().color = MutedInk;

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(iconRoot.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0f);
        bodyRect.anchorMax = new Vector2(0.5f, 0f);
        bodyRect.pivot = new Vector2(0.5f, 0f);
        bodyRect.sizeDelta = new Vector2(12f, 10f);
        bodyRect.anchoredPosition = new Vector2(0f, 1f);
        bodyGo.AddComponent<Image>().color = MutedInk;

        var keyholeGo = new GameObject("Keyhole");
        keyholeGo.transform.SetParent(bodyGo.transform, false);
        var keyholeRect = keyholeGo.AddComponent<RectTransform>();
        keyholeRect.anchorMin = new Vector2(0.5f, 0.5f);
        keyholeRect.anchorMax = new Vector2(0.5f, 0.5f);
        keyholeRect.sizeDelta = new Vector2(3f, 4f);
        keyholeGo.AddComponent<Image>().color = DisabledFill;
    }

    /// <summary>
    /// HUD-style corner brackets: thin frame plus thicker L-shaped accents at each corner.
    /// </summary>
    public static GameObject CreateCornerBracketFrame(Transform parent, Color color,
        float armLength = 24f, float cornerThickness = 4f, float frameThickness = 2f, float outset = 7f)
    {
        var root = new GameObject("Corner Bracket Frame");
        root.transform.SetParent(parent, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = new Vector2(-outset, -outset);
        rootRect.offsetMax = new Vector2(outset, outset);

        CreateThinFrameEdge(root.transform, color, frameThickness, horizontal: true, top: true);
        CreateThinFrameEdge(root.transform, color, frameThickness, horizontal: true, top: false);
        CreateThinFrameEdge(root.transform, color, frameThickness, horizontal: false, left: true);
        CreateThinFrameEdge(root.transform, color, frameThickness, horizontal: false, left: false);

        CreateCornerBracket(root.transform, color, armLength, cornerThickness, top: true, left: true);
        CreateCornerBracket(root.transform, color, armLength, cornerThickness, top: true, left: false);
        CreateCornerBracket(root.transform, color, armLength, cornerThickness, top: false, left: true);
        CreateCornerBracket(root.transform, color, armLength, cornerThickness, top: false, left: false);

        return root;
    }

    static void CreateThinFrameEdge(Transform parent, Color color, float thickness, bool horizontal, bool top = false, bool left = false)
    {
        var edgeGo = new GameObject("Frame Edge");
        edgeGo.transform.SetParent(parent, false);
        var rect = edgeGo.AddComponent<RectTransform>();

        if (horizontal)
        {
            float y = top ? 1f : 0f;
            rect.anchorMin = new Vector2(0f, y);
            rect.anchorMax = new Vector2(1f, y);
            rect.pivot = new Vector2(0.5f, y);
            rect.sizeDelta = new Vector2(0f, thickness);
        }
        else
        {
            float x = left ? 0f : 1f;
            rect.anchorMin = new Vector2(x, 0f);
            rect.anchorMax = new Vector2(x, 1f);
            rect.pivot = new Vector2(x, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
        }

        rect.anchoredPosition = Vector2.zero;
        var image = edgeGo.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    static void CreateCornerBracket(Transform parent, Color color, float armLength, float thickness, bool top, bool left)
    {
        float anchorX = left ? 0f : 1f;
        float anchorY = top ? 1f : 0f;
        var pivot = new Vector2(anchorX, anchorY);

        var horizontal = new GameObject("Corner H");
        horizontal.transform.SetParent(parent, false);
        var hRect = horizontal.AddComponent<RectTransform>();
        hRect.anchorMin = pivot;
        hRect.anchorMax = pivot;
        hRect.pivot = pivot;
        hRect.sizeDelta = new Vector2(armLength, thickness);
        hRect.anchoredPosition = Vector2.zero;
        var hImage = horizontal.AddComponent<Image>();
        hImage.color = color;
        hImage.raycastTarget = false;

        var vertical = new GameObject("Corner V");
        vertical.transform.SetParent(parent, false);
        var vRect = vertical.AddComponent<RectTransform>();
        vRect.anchorMin = pivot;
        vRect.anchorMax = pivot;
        vRect.pivot = pivot;
        vRect.sizeDelta = new Vector2(thickness, armLength);
        vRect.anchoredPosition = Vector2.zero;
        var vImage = vertical.AddComponent<Image>();
        vImage.color = color;
        vImage.raycastTarget = false;
    }

    public static void EnsureEventSystem()
    {
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem keeper = null;

        foreach (var system in systems)
        {
            if (system == null)
            {
                continue;
            }

            if (keeper != null)
            {
                Object.Destroy(system.gameObject);
                continue;
            }

            keeper = system;
        }

        if (keeper == null)
        {
            var go = new GameObject("EventSystem");
            keeper = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            return;
        }

        if (!keeper.gameObject.activeInHierarchy)
        {
            keeper.gameObject.SetActive(true);
        }

        if (keeper.GetComponent<StandaloneInputModule>() == null)
        {
            var existingModule = keeper.GetComponent<BaseInputModule>();
            if (existingModule != null)
            {
                Object.Destroy(existingModule);
            }

            keeper.gameObject.AddComponent<StandaloneInputModule>();
        }
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
/// Hover scale for settings buttons (no press tint or click sound).
/// </summary>
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Button _button;
    Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _button = GetComponent<Button>();
        _baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable)
        {
            return;
        }

        transform.localScale = _baseScale * MenuUiFactory.CardHoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _baseScale;
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
