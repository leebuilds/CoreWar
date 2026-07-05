using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-match pause overlay (does not freeze gameplay).
/// </summary>
public class GamePauseMenu : MonoBehaviour
{
    RespawnClassPicker _respawnPicker;
    GameObject _overlayRoot;
    GameObject _settingsOverlay;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    /// <summary>
    /// Handles ESC while the pause menu is open. Closes settings first, then the pause overlay.
    /// </summary>
    public bool TryHandleEscape()
    {
        if (!_isOpen)
        {
            return false;
        }

        if (_settingsOverlay != null)
        {
            HideSettingsOverlay();
            return true;
        }

        Hide();
        return true;
    }

    public static GamePauseMenu Create(Transform parent, RespawnClassPicker respawnPicker)
    {
        var go = new GameObject("Game Pause Menu");
        go.transform.SetParent(parent, false);
        var menu = go.AddComponent<GamePauseMenu>();
        menu._respawnPicker = respawnPicker;
        menu.Build();
        return menu;
    }

    void Build()
    {
        var canvasGo = new GameObject("Pause Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _overlayRoot = new GameObject("Pause Overlay Root");
        _overlayRoot.transform.SetParent(canvasGo.transform, false);
        MenuUiFactory.StretchFull(_overlayRoot.AddComponent<RectTransform>());
        _overlayRoot.SetActive(false);
    }

    public void Toggle()
    {
        if (_isOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        if (_isOpen)
        {
            return;
        }

        HideSettingsOverlay();
        ClearOverlayChildren();
        _isOpen = true;
        _overlayRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CreateDim(_overlayRoot.transform, 0.35f);

        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "PAUSE", showBack: true,
            "game continues in the background", new Vector2(480f, 420f), showHeader: false, Hide);

        MenuUiFactory.CreateButton(frame.Body, "Respawn", "RESPAWN",
            new Vector2(0f, 70f), new Vector2(320f, 64f), OpenRespawnPicker);
        MenuUiFactory.CreateButton(frame.Body, "Settings", "SETTINGS",
            new Vector2(0f, -10f), new Vector2(320f, 64f), ShowSettings);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match", "EXIT MATCH",
            new Vector2(0f, -90f), new Vector2(320f, 64f), ExitMatch);
    }

    public void Hide()
    {
        HideSettingsOverlay();
        ClearOverlayChildren();
        _isOpen = false;
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OpenRespawnPicker()
    {
        Hide();
        _respawnPicker?.Show();
    }

    void ShowSettings()
    {
        if (_settingsOverlay != null)
        {
            return;
        }

        _settingsOverlay = MenuUiFactory.CreateModalOverlay(_overlayRoot.transform, 0.25f);
        var frame = MenuWindowFrame.CreateScreen(_settingsOverlay.transform, "SETTINGS", showBack: true,
            "audio · controls · account options coming soon", new Vector2(520f, 360f), showHeader: false,
            HideSettingsOverlay);

        MenuUiFactory.CreateText(frame.Body, "Placeholder", "settings are not available yet",
            22, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(420f, 80f),
            MenuUiFactory.MutedInk);
    }

    void HideSettingsOverlay()
    {
        if (_settingsOverlay != null)
        {
            Destroy(_settingsOverlay);
            _settingsOverlay = null;
        }
    }

    void ExitMatch()
    {
        Hide();
        SceneManager.LoadScene("MainMenu");
    }

    static void CreateDim(Transform parent, float alpha)
    {
        var dim = new GameObject("Dim");
        dim.transform.SetParent(parent, false);
        dim.transform.SetAsFirstSibling();
        var dimImage = dim.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, alpha);
        MenuUiFactory.StretchFull(dim.GetComponent<RectTransform>());
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
