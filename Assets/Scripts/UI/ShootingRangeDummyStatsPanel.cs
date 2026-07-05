using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logarithmic dummy health slider for shooting range tuning.
/// </summary>
public class ShootingRangeDummyStatsPanel : MonoBehaviour
{
    GameObject _overlayRoot;
    Slider _slider;
    Text _valueLabel;
    Action _onApplied;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    public static ShootingRangeDummyStatsPanel Create(Transform parent, Action onApplied)
    {
        var go = new GameObject("Shooting Range Dummy Stats");
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<ShootingRangeDummyStatsPanel>();
        panel._onApplied = onApplied;
        panel.Build();
        return panel;
    }

    void Build()
    {
        MenuUiFactory.EnsureEventSystem();

        var canvasGo = new GameObject("Dummy Stats Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _overlayRoot = new GameObject("Overlay Root");
        _overlayRoot.transform.SetParent(canvasGo.transform, false);
        MenuUiFactory.StretchFull(_overlayRoot.AddComponent<RectTransform>());
        _overlayRoot.SetActive(false);
    }

    public void Show()
    {
        if (_isOpen)
        {
            return;
        }

        ClearOverlayChildren();
        _isOpen = true;
        _overlayRoot.SetActive(true);

        var dimGo = MenuUiFactory.CreateModalOverlay(_overlayRoot.transform, 0.35f);
        var frame = MenuWindowFrame.CreateScreen(dimGo.transform, "DUMMY STATS", showBack: true,
            "target health · 10 to 1000 hp", new Vector2(560f, 360f), showHeader: false, () => Hide());

        _valueLabel = MenuUiFactory.CreateText(frame.Body, "Health Value", "100 HP",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(420f, 50f), MenuUiFactory.Ink);

        _slider = MenuUiFactory.CreateSlider(frame.Body, "Health Slider",
            new Vector2(0f, -10f), new Vector2(420f, MenuUiFactory.CompactControlHeight),
            0f, 1f, ShootingRangeSession.HealthValueToSlider(ShootingRangeSession.DummyMaxHealth), OnSliderChanged);

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
        ShootingRangeSession.DummyMaxHealth = ShootingRangeSession.HealthSliderToValue(_slider.value);
        ShootingRangeSession.ResetAllDummies();
        _onApplied?.Invoke();
        Hide();
    }

    public void Hide()
    {
        _isOpen = false;
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        ClearOverlayChildren();
        _slider = null;
        _valueLabel = null;
    }

    void ClearOverlayChildren()
    {
        if (_overlayRoot == null)
        {
            return;
        }

        for (int i = _overlayRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(_overlayRoot.transform.GetChild(i).gameObject);
        }
    }
}
