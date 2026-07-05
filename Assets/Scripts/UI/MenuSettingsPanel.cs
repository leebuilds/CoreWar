using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the shared settings form used in the hub and in-match pause menu.
/// </summary>
public static class MenuSettingsPanel
{
    public static void Build(Transform body, bool showAccountSection)
    {
        MenuSettings.EnsureLoaded();

        float y = 230f;
        CreateSectionLabel(body, "APPEARANCE", ref y);
        CreateThemeRow(body, ref y);

        CreateSectionLabel(body, "AUDIO", ref y);
        CreateVolumeRow(body, ref y);
        CreateUiSoundsRow(body, ref y);

        CreateSectionLabel(body, "CONTROLS", ref y);
        CreateSensitivityRow(body, ref y);

        if (showAccountSection && ProfileSession.IsSignedIn)
        {
            CreateSectionLabel(body, "ACCOUNT", ref y);
            var username = ProfileSession.ActiveProfile?.username ?? "player";
            CreateBodyLabel(body, "Account Name", $"signed in as {username}", y,
                MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.MiddleCenter, MenuUiFactory.MutedInk);
        }
    }

    static void CreateSectionLabel(Transform body, string label, ref float y)
    {
        CreateBodyLabel(body, $"Section {label}", label, y,
            MenuUiFactory.SectionFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, MenuUiFactory.Ink);
        y -= MenuUiFactory.SectionLabelSpacing;
    }

    static void CreateThemeRow(Transform body, ref float y)
    {
        var row = CreateRow(body, "Theme Row", y, MenuUiFactory.CompactControlHeight);
        bool dark = MenuSettings.IsDarkMode;
        float themeOffset = MenuUiFactory.ThemeButtonOffset;
        MenuUiFactory.CreateSettingsButton(row.transform, "Light Theme",
            dark ? "LIGHT" : "LIGHT ✓",
            new Vector2(-themeOffset, 0f), MenuUiFactory.CompactButtonSize, () => MenuSettings.SetDarkMode(false));
        MenuUiFactory.CreateSettingsButton(row.transform, "Dark Theme",
            dark ? "DARK ✓" : "DARK",
            new Vector2(themeOffset, 0f), MenuUiFactory.CompactButtonSize, () => MenuSettings.SetDarkMode(true));
        y -= MenuUiFactory.SettingsRowSpacing;
    }

    static void CreateVolumeRow(Transform body, ref float y)
    {
        var row = CreateRow(body, "Volume Row", y, MenuUiFactory.SettingsLabeledRowHeight);
        CreateRowLeftLabel(row.transform, "Volume Label", "UI volume");

        var valueLabel = CreateRowRightLabel(row.transform, "Volume Value", FormatPercent(MenuSettings.MasterVolume));

        CreateRowSlider(row.transform, "Volume Slider", 0f, 1f, MenuSettings.MasterVolume, value =>
        {
            MenuSettings.SetMasterVolume(value);
            valueLabel.text = FormatPercent(MenuSettings.MasterVolume);
        });
        y -= MenuUiFactory.SettingsSliderRowSpacing;
    }

    static void CreateUiSoundsRow(Transform body, ref float y)
    {
        var row = CreateRow(body, "Ui Sounds Row", y, MenuUiFactory.CompactControlHeight);
        CreateRowLeftLabel(row.transform, "Ui Sounds Label", "UI sounds");
        var toggle = MenuUiFactory.CreateSettingsButtonRight(row.transform, "Ui Sounds Toggle",
            MenuSettings.UiSoundsEnabled ? "ON" : "OFF",
            MenuUiFactory.SettingsToggleSize, null);
        toggle.onClick.AddListener(() =>
        {
            MenuSettings.SetUiSoundsEnabled(!MenuSettings.UiSoundsEnabled);
            RefreshToggleLabel(toggle);
        });
        y -= MenuUiFactory.SettingsRowSpacing;
    }

    static void CreateSensitivityRow(Transform body, ref float y)
    {
        var row = CreateRow(body, "Sensitivity Row", y, MenuUiFactory.SettingsLabeledRowHeight);
        CreateRowLeftLabel(row.transform, "Sensitivity Label", "Mouse sensitivity");

        var valueLabel = CreateRowRightLabel(row.transform, "Sensitivity Value",
            $"{MenuSettings.MouseSensitivity:0.0}x");

        CreateRowSlider(row.transform, "Sensitivity Slider", 0.25f, 2.5f, MenuSettings.MouseSensitivity, value =>
        {
            MenuSettings.SetMouseSensitivity(value);
            valueLabel.text = $"{MenuSettings.MouseSensitivity:0.0}x";
        });
        y -= MenuUiFactory.SettingsSliderRowSpacing;
    }

    static GameObject CreateRow(Transform body, string name, float y, float height)
    {
        var row = new GameObject(name);
        row.transform.SetParent(body, false);
        var rect = row.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, height);
        return row;
    }

    static Text CreateBodyLabel(Transform body, string name, string content, float y,
        int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(body, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-MenuUiFactory.ContentPadding * 2f, MenuUiFactory.SectionLabelHeight);

        var text = go.AddComponent<Text>();
        text.font = MenuUiFactory.Font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    static Text CreateRowLeftLabel(Transform row, string name, string content)
    {
        var go = new GameObject(name);
        go.transform.SetParent(row, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0.62f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = new Vector2(MenuUiFactory.ContentPadding, 0f);
        rect.sizeDelta = new Vector2(0f, MenuUiFactory.SliderTrackHeight);

        var text = go.AddComponent<Text>();
        text.font = MenuUiFactory.Font;
        text.text = content;
        text.fontSize = MenuUiFactory.BodyFontSize;
        text.fontStyle = FontStyle.Normal;
        text.color = MenuUiFactory.Ink;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    static Text CreateRowRightLabel(Transform row, string name, string content)
    {
        var go = new GameObject(name);
        go.transform.SetParent(row, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.62f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMax = new Vector2(-MenuUiFactory.ContentPadding, 0f);
        rect.sizeDelta = new Vector2(0f, MenuUiFactory.SliderTrackHeight);

        var text = go.AddComponent<Text>();
        text.font = MenuUiFactory.Font;
        text.text = content;
        text.fontSize = MenuUiFactory.BodyFontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = MenuUiFactory.MutedInk;
        text.alignment = TextAnchor.MiddleRight;
        text.raycastTarget = false;
        return text;
    }

    static void CreateRowSlider(Transform row, string name, float minValue, float maxValue, float value,
        UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        MenuUiFactory.CreateStretchedSlider(row, name, minValue, maxValue, value, onValueChanged);
    }

    static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }

    static void RefreshToggleLabel(Button toggle)
    {
        if (toggle == null)
        {
            return;
        }

        var label = toggle.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = MenuSettings.UiSoundsEnabled ? "ON" : "OFF";
        }
    }
}
