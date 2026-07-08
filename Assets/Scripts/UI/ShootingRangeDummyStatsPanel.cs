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
            "target health · 10 to 1000 hp", new Vector2(560f, 360f), showHeader: false, Close,
            animateFade: false);

        _valueLabel = MenuUiFactory.CreateText(frame.Body, "Health Value", "100 HP",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(420f, 50f),
            MenuUiFactory.Ink);

        _slider = MenuUiFactory.CreateSlider(frame.Body, "Health Slider",
            new Vector2(0f, -10f), new Vector2(420f, MenuUiFactory.CompactControlHeight),
            0f, 1f, ShootingRangeSession.HealthValueToSlider(ShootingRangeSession.DummyMaxHealth),
            OnSliderChanged);

        MenuUiFactory.CreateButton(frame.Body, "Apply", "APPLY",
            new Vector2(0f, -90f), MenuUiFactory.StandardButtonSize, ApplyAndClose);

        UpdateValueLabel(_slider.value);
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
