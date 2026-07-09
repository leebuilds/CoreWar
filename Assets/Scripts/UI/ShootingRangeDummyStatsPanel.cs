using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logarithmic dummy health slider for shooting range tuning (pause submenu).
/// </summary>
public class ShootingRangeDummyStatsPanel : MonoBehaviour
{
    Text _valueLabel;
    Slider _slider;
    Button _movingToggle;
    Action _onClosed;

    public static void BuildInto(Transform parent, Action onClosed)
    {
        var host = new GameObject("Dummy Stats Controller");
        host.transform.SetParent(parent, false);
        var panel = host.AddComponent<ShootingRangeDummyStatsPanel>();
        panel._onClosed = onClosed;
        panel.BuildUi(parent);
    }

    void BuildUi(Transform parent)
    {
        MenuUiFactory.CreateFullscreenDim(parent, 0.35f);

        var frame = MenuWindowFrame.CreateScreen(parent, "DUMMY STATS", showBack: true,
            "target health · 10 to 1000 hp", new Vector2(560f, 400f), showHeader: false, Close,
            animateFade: false);

        _valueLabel = MenuUiFactory.CreateText(frame.Body, "Health Value", "100 HP",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 90f), new Vector2(420f, 50f),
            MenuUiFactory.Ink);

        _slider = MenuUiFactory.CreateSlider(frame.Body, "Health Slider",
            new Vector2(0f, 20f), new Vector2(420f, MenuUiFactory.CompactControlHeight),
            0f, 1f, ShootingRangeSession.HealthValueToSlider(ShootingRangeSession.DummyMaxHealth),
            OnSliderChanged);

        CreateMovingDummiesRow(frame.Body);

        MenuUiFactory.CreateButton(frame.Body, "Apply", "APPLY",
            new Vector2(0f, -120f), MenuUiFactory.StandardButtonSize, ApplyAndClose);

        UpdateValueLabel(_slider.value);
        RefreshMovingToggle();
    }

    void CreateMovingDummiesRow(Transform body)
    {
        var row = new GameObject("Moving Dummies Row");
        row.transform.SetParent(body, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, -50f);
        rowRect.sizeDelta = new Vector2(420f, MenuUiFactory.CompactControlHeight);

        MenuUiFactory.CreateText(row.transform, "Moving Dummies Label", "Moving dummies",
            24, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(-150f, 0f), new Vector2(220f, 36f),
            MenuUiFactory.Ink);

        _movingToggle = MenuUiFactory.CreateButton(row.transform, "Moving Dummies Toggle", "OFF",
            new Vector2(150f, 0f), MenuUiFactory.SettingsToggleSize, ToggleMovingDummies);
    }

    void OnSliderChanged(float value)
    {
        UpdateValueLabel(value);
    }

    void UpdateValueLabel(float sliderValue)
    {
        if (_valueLabel == null)
        {
            return;
        }

        int hp = Mathf.RoundToInt(ShootingRangeSession.HealthSliderToValue(sliderValue));
        _valueLabel.text = $"{hp} HP";
    }

    void ToggleMovingDummies()
    {
        ShootingRangeSession.MovingDummies = !ShootingRangeSession.MovingDummies;
        if (!ShootingRangeSession.MovingDummies)
        {
            ShootingRangeSession.ResetDummyPositions();
        }

        RefreshMovingToggle();
    }

    void RefreshMovingToggle()
    {
        if (_movingToggle == null)
        {
            return;
        }

        var label = _movingToggle.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = ShootingRangeSession.MovingDummies ? "ON" : "OFF";
        }
    }

    void ApplyAndClose()
    {
        if (_slider != null)
        {
            ShootingRangeSession.DummyMaxHealth = ShootingRangeSession.HealthSliderToValue(_slider.value);
            ShootingRangeSession.ResetAllDummies();
        }

        Close();
    }

    void Close()
    {
        _onClosed?.Invoke();
    }
}
