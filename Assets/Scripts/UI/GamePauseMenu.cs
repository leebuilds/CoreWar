using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match pause overlay (does not freeze gameplay).
/// </summary>
public class GamePauseMenu : MonoBehaviour
{
    static GamePauseMenu _instance;

    RespawnClassPicker _respawnPicker;
    ShootingRangeCharacterPicker _characterPicker;
    ShootingRangeDummyStatsPanel _dummyStatsPanel;
    PlayerDamageDebugPanel _damageDebugPanel;
    ThirdPersonController _player;
    GameObject _overlayRoot;
    GameObject _settingsOverlay;
    GameObject _exitConfirmOverlay;
    bool _isOpen;
    bool _settingsSubscribed;

    public bool IsOpen => _isOpen;
    public static bool IsAnyOpen => _instance != null && _instance._isOpen;

    public bool TryHandleEscape()
    {
        if (_dummyStatsPanel != null && _dummyStatsPanel.IsOpen)
        {
            _dummyStatsPanel.Hide();
            return true;
        }

        if (_damageDebugPanel != null && _damageDebugPanel.IsOpen)
        {
            _damageDebugPanel.Hide();
            return true;
        }

        if (!_isOpen)
        {
            return false;
        }

        if (_exitConfirmOverlay != null)
        {
            HideExitMatchConfirm();
            return true;
        }

        if (_settingsOverlay != null)
        {
            HideSettingsOverlay();
            return true;
        }

        Hide();
        return true;
    }

    public static GamePauseMenu Create(
        Transform parent,
        RespawnClassPicker respawnPicker,
        ShootingRangeCharacterPicker characterPicker = null,
        ThirdPersonController player = null)
    {
        var go = new GameObject("Game Pause Menu");
        go.transform.SetParent(parent, false);
        var menu = go.AddComponent<GamePauseMenu>();
        _instance = menu;
        menu._respawnPicker = respawnPicker;
        menu._characterPicker = characterPicker;
        menu._player = player;
        menu.Build();
        return menu;
    }

    void Build()
    {
        MenuUiFactory.EnsureEventSystem();

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

        if (GameSession.IsShootingRange)
        {
            _dummyStatsPanel = ShootingRangeDummyStatsPanel.Create(transform, null);
        }

        _damageDebugPanel = PlayerDamageDebugPanel.Create(transform, _player, () => Hide());
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

        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);
        BuildPauseContents();
    }

    void BuildPauseContents()
    {
        CreateDim(_overlayRoot.transform, 0.35f);

        if (GameSession.IsShootingRange)
        {
            BuildShootingRangePauseContents();
            return;
        }

        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "PAUSE", showBack: true,
            PauseFooterText(), new Vector2(480f, 500f), showHeader: false, Hide);

        bool respawnLocked = GameSession.IsInPrepPhase;
        var respawnButton = MenuUiFactory.CreateButton(frame.Body, "Respawn", "RESPAWN",
            new Vector2(0f, 110f), MenuUiFactory.StandardButtonSize, OpenRespawnPicker, enabled: !respawnLocked);
        if (respawnLocked)
        {
            MenuUiFactory.AddButtonLockIcon(respawnButton.transform);
        }

        MenuUiFactory.CreateButton(frame.Body, "Test Damage", "TEST DAMAGE",
            new Vector2(0f, 30f), MenuUiFactory.StandardButtonSize, OpenDamageDebug);
        MenuUiFactory.CreateButton(frame.Body, "Settings", "SETTINGS",
            new Vector2(0f, -50f), MenuUiFactory.StandardButtonSize, ShowSettings);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match", "EXIT MATCH",
            new Vector2(0f, -130f), MenuUiFactory.StandardButtonSize, RequestExitMatch);
    }

    void BuildShootingRangePauseContents()
    {
        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "PAUSE", showBack: true,
            PauseFooterText(), new Vector2(480f, 640f), showHeader: false, Hide);

        MenuUiFactory.CreateButton(frame.Body, "Choose Character", "CHOOSE CHARACTER",
            new Vector2(0f, 170f), MenuUiFactory.StandardButtonSize, OpenCharacterPicker);
        MenuUiFactory.CreateButton(frame.Body, "Dummy Stats", "DUMMY STATS",
            new Vector2(0f, 90f), MenuUiFactory.StandardButtonSize, OpenDummyStats);
        MenuUiFactory.CreateButton(frame.Body, "Test Damage", "TEST DAMAGE",
            new Vector2(0f, 10f), MenuUiFactory.StandardButtonSize, OpenDamageDebug);
        MenuUiFactory.CreateButton(frame.Body, "Reset Map", "RESET MAP",
            new Vector2(0f, -70f), MenuUiFactory.StandardButtonSize, ResetMap);
        MenuUiFactory.CreateButton(frame.Body, "Settings", "SETTINGS",
            new Vector2(0f, -150f), MenuUiFactory.StandardButtonSize, ShowSettings);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match", "EXIT MATCH",
            new Vector2(0f, -230f), MenuUiFactory.StandardButtonSize, RequestExitMatch);
    }

    static string PauseFooterText()
    {
        return GameSession.IsInPrepPhase
            ? "respawn locked until match starts"
            : "game continues in the background";
    }

    public void Hide()
    {
        Hide(resumeGameplay: true);
    }

    public void Hide(bool resumeGameplay)
    {
        HideExitMatchConfirm();
        HideSettingsOverlay();
        ClearOverlayChildren();
        _isOpen = false;
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        if (resumeGameplay && GameSession.IsMatchActive)
        {
            if (GameSession.IsInPrepPhase)
            {
                if (GameSession.IsPrepReady)
                {
                    SceneFlow.ApplyGameInputState();
                }
                else
                {
                    SceneFlow.ApplyMenuInputState();
                }

                MatchClockHud.Instance?.SetVisible(false);
            }
            else
            {
                SceneFlow.ApplyGameInputState();
                MatchClockHud.Instance?.SetVisible(true);
            }
        }
        else if (!resumeGameplay)
        {
            MatchClockHud.Instance?.SetVisible(false);
        }
    }

    void OpenRespawnPicker()
    {
        if (GameSession.IsInPrepPhase)
        {
            return;
        }

        Hide();
        _respawnPicker?.Show();
    }

    void OpenCharacterPicker()
    {
        Hide();
        _characterPicker?.Show();
    }

    void OpenDummyStats()
    {
        _dummyStatsPanel?.Show();
    }

    void OpenDamageDebug()
    {
        _damageDebugPanel?.Show();
    }

    void ResetMap()
    {
        ShootingRangeSession.ResetMap();
    }

    void ShowSettings()
    {
        if (_settingsOverlay != null)
        {
            return;
        }

        EnsureSettingsSubscription();
        _settingsOverlay = MenuUiFactory.CreateModalOverlay(_overlayRoot.transform, 0.25f);
        var frame = MenuWindowFrame.CreateScreen(_settingsOverlay.transform, "SETTINGS", showBack: true,
            "appearance · audio · controls", new Vector2(580f, 680f), showHeader: false,
            HideSettingsOverlay);

        MenuSettingsPanel.Build(frame.Body, showAccountSection: false);
    }

    void EnsureSettingsSubscription()
    {
        if (_settingsSubscribed)
        {
            return;
        }

        MenuSettings.Changed += HandleSettingsChanged;
        _settingsSubscribed = true;
    }

    void HandleSettingsChanged()
    {
        if (!_isOpen)
        {
            return;
        }

        bool reopenSettings = _settingsOverlay != null;
        HideSettingsOverlay();
        HideExitMatchConfirm();
        ClearOverlayChildren();
        BuildPauseContents();

        if (reopenSettings)
        {
            ShowSettings();
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        if (_settingsSubscribed)
        {
            MenuSettings.Changed -= HandleSettingsChanged;
        }
    }

    void HideSettingsOverlay()
    {
        if (_settingsOverlay != null)
        {
            Destroy(_settingsOverlay);
            _settingsOverlay = null;
        }
    }

    void RequestExitMatch()
    {
        ShowExitMatchConfirm();
    }

    void ShowExitMatchConfirm()
    {
        if (_exitConfirmOverlay != null)
        {
            return;
        }

        var frame = MenuWindowFrame.CreateModal(_overlayRoot.transform, "EXIT MATCH?", showBack: false,
            "you will return to the hub", new Vector2(480f, 320f), HideExitMatchConfirm);
        _exitConfirmOverlay = frame.transform.parent.gameObject;

        MenuUiFactory.CreateButton(frame.Body, "Stay Button", "STAY",
            new Vector2(0f, 40f), MenuUiFactory.StandardButtonSize, HideExitMatchConfirm);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match Button", "EXIT MATCH",
            new Vector2(0f, -40f), MenuUiFactory.StandardButtonSize, () =>
            {
                HideExitMatchConfirm();
                SceneFlow.EnterMainMenu();
            });
    }

    void HideExitMatchConfirm()
    {
        if (_exitConfirmOverlay != null)
        {
            Destroy(_exitConfirmOverlay);
            _exitConfirmOverlay = null;
        }
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
