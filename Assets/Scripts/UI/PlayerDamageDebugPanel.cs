using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug panel to inflict test damage on the local player (100 HP, instant refill).
/// </summary>
public class PlayerDamageDebugPanel : MonoBehaviour
{
    GameObject _overlayRoot;
    Slider _slider;
    Text _valueLabel;
    Text _modeLabel;
    Button _bodyButton;
    Button _headshotButton;
    ThirdPersonController _player;
    Action _onApplied;
    bool _headshot;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    public static PlayerDamageDebugPanel Create(Transform parent, ThirdPersonController player, Action onApplied)
    {
        var go = new GameObject("Player Damage Debug");
        go.transform.SetParent(parent, false);
        var panel = go.AddComponent<PlayerDamageDebugPanel>();
        panel._player = player;
        panel._onApplied = onApplied;
        panel.Build();
        return panel;
    }

    void Build()
    {
        MenuUiFactory.EnsureEventSystem();

        var canvasGo = new GameObject("Damage Debug Canvas");
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
        var frame = MenuWindowFrame.CreateScreen(dimGo.transform, "TEST DAMAGE", showBack: true,
            "debug · 100 hp · instant refill", new Vector2(560f, 420f), showHeader: false, Hide);

        _valueLabel = MenuUiFactory.CreateText(frame.Body, "Damage Value", "40 DMG",
            36, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 110f), new Vector2(420f, 50f), MenuUiFactory.Ink);

        _slider = MenuUiFactory.CreateSlider(frame.Body, "Damage Slider",
            new Vector2(0f, 40f), new Vector2(420f, MenuUiFactory.CompactControlHeight),
            1f, 99f, 40f, OnSliderChanged);

        _modeLabel = MenuUiFactory.CreateText(frame.Body, "Hit Mode", "HIT TYPE: BODY",
            22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -20f), new Vector2(420f, 32f), MenuUiFactory.MutedInk);

        _bodyButton = MenuUiFactory.CreateButton(frame.Body, "Body Hit", "BODY",
            new Vector2(-110f, -80f), new Vector2(200f, MenuUiFactory.StandardButtonSize.y), () => SetHeadshot(false));
        _headshotButton = MenuUiFactory.CreateButton(frame.Body, "Headshot Hit", "HEADSHOT",
            new Vector2(110f, -80f), new Vector2(200f, MenuUiFactory.StandardButtonSize.y), () => SetHeadshot(true));

        MenuUiFactory.CreateButton(frame.Body, "Inflict", "INFLICT",
            new Vector2(0f, -150f), MenuUiFactory.StandardButtonSize, InflictDamage);

        SetHeadshot(false);
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

        int damage = Mathf.RoundToInt(sliderValue);
        _valueLabel.text = $"{damage} DMG";
    }

    void SetHeadshot(bool headshot)
    {
        _headshot = headshot;
        if (_modeLabel != null)
        {
            _modeLabel.text = _headshot ? "HIT TYPE: HEADSHOT" : "HIT TYPE: BODY";
        }

        RefreshModeButtons();
    }

    void RefreshModeButtons()
    {
        SetButtonSelected(_bodyButton, !_headshot);
        SetButtonSelected(_headshotButton, _headshot);
    }

    static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.16f, 0.68f, 0.24f, 0.95f)
                : MenuUiFactory.MutedInk;
        }
    }

    void InflictDamage()
    {
        if (_player == null)
        {
            return;
        }

        var health = _player.GetComponent<PlayerHealth>();
        if (health == null)
        {
            return;
        }

        int damage = Mathf.RoundToInt(_slider.value);
        health.ApplyDebugDamage(damage, _headshot);
        Hide();
        _onApplied?.Invoke();
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
        _modeLabel = null;
        _bodyButton = null;
        _headshotButton = null;
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
