using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match pause overlay (does not freeze gameplay).
/// </summary>
public class GamePauseMenu : MonoBehaviour
{
    enum PauseSubMenu
    {
        None,
        Settings,
        ExitConfirm,
        DummyStats
    }

    static GamePauseMenu _instance;

    RespawnClassPicker _respawnPicker;
    ShootingRangeCharacterPicker _characterPicker;
    ThirdPersonController _player;
    RectTransform _layer;
    GameObject _overlayRoot;
    GameObject _pauseMainContent;
    GameObject _activeSubMenu;
    PauseSubMenu _activeSubMenuKind = PauseSubMenu.None;
    bool _isOpen;
    bool _settingsSubscribed;

    public bool IsOpen => _isOpen;
    public static bool IsAnyOpen => _instance != null && _instance._isOpen;

    public bool TryHandleEscape()
    {
        if (!_isOpen)
        {
            return false;
        }

        if (_activeSubMenuKind != PauseSubMenu.None)
        {
            CloseSubMenu();
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
        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Pause Menu", 200);
        var hostRect = GameUICanvas.CreateScreenHost(layer, "Game Pause Menu");
        var go = hostRect.gameObject;
        var menu = go.AddComponent<GamePauseMenu>();
        _instance = menu;
        menu._layer = layer;
        menu._respawnPicker = respawnPicker;
        menu._characterPicker = characterPicker;
        menu._player = player;
        menu.Build();
        return menu;
    }

    void Build()
    {
        _overlayRoot = new GameObject("Pause Overlay Root");
        _overlayRoot.transform.SetParent(transform, false);
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

        ClearOverlayContent();
        _isOpen = true;
        _overlayRoot.SetActive(true);

        MenuUiFactory.EnsureEventSystem();
        GameUICanvas.BringLayerToFront(_layer);
        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);
        BuildPauseMainContent();
    }

    void BuildPauseMainContent()
    {
        _pauseMainContent = CreateSubmenuRoot("Pause Main");
        MenuUiFactory.CreateFullscreenDim(_pauseMainContent.transform, 0.35f);

        if (GameSession.IsShootingRange)
        {
            BuildShootingRangePauseContents(_pauseMainContent.transform);
            return;
        }

        BuildStandardPauseContents(_pauseMainContent.transform);
    }

    void BuildStandardPauseContents(Transform parent)
    {
        var frame = MenuWindowFrame.CreateScreen(parent, "PAUSE", showBack: true,
            PauseFooterText(), new Vector2(480f, 420f), showHeader: false, Hide, animateFade: false);

        bool respawnLocked = GameSession.IsInPrepPhase;
        var respawnButton = MenuUiFactory.CreateButton(frame.Body, "Respawn", "RESPAWN",
            new Vector2(0f, 70f), MenuUiFactory.StandardButtonSize, OpenRespawnPicker, enabled: !respawnLocked);
        if (respawnLocked)
        {
            MenuUiFactory.AddButtonLockIcon(respawnButton.transform);
        }

        MenuUiFactory.CreateButton(frame.Body, "Settings", "SETTINGS",
            new Vector2(0f, -10f), MenuUiFactory.StandardButtonSize, ShowSettings);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match", "EXIT MATCH",
            new Vector2(0f, -90f), MenuUiFactory.StandardButtonSize, ShowExitMatchConfirm);
    }

    void BuildShootingRangePauseContents(Transform parent)
    {
        var frame = MenuWindowFrame.CreateScreen(parent, "PAUSE", showBack: true,
            PauseFooterText(), new Vector2(480f, 560f), showHeader: false, Hide, animateFade: false);

        MenuUiFactory.CreateButton(frame.Body, "Choose Character", "CHOOSE CHARACTER",
            new Vector2(0f, 130f), MenuUiFactory.StandardButtonSize, OpenCharacterPicker);
        MenuUiFactory.CreateButton(frame.Body, "Dummy Stats", "DUMMY STATS",
            new Vector2(0f, 50f), MenuUiFactory.StandardButtonSize, ShowDummyStats);
        MenuUiFactory.CreateButton(frame.Body, "Reset Map", "RESET MAP",
            new Vector2(0f, -30f), MenuUiFactory.StandardButtonSize, ResetMap);
        MenuUiFactory.CreateButton(frame.Body, "Settings", "SETTINGS",
            new Vector2(0f, -110f), MenuUiFactory.StandardButtonSize, ShowSettings);
        MenuUiFactory.CreateButton(frame.Body, "Exit Match", "EXIT MATCH",
            new Vector2(0f, -190f), MenuUiFactory.StandardButtonSize, ShowExitMatchConfirm);
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
        ClearOverlayContent();
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

    void OpenSubMenu(PauseSubMenu kind, Action<GameObject> build)
    {
        if (_pauseMainContent != null)
        {
            _pauseMainContent.SetActive(false);
        }

        ClearActiveSubMenu();
        _activeSubMenuKind = kind;
        _activeSubMenu = CreateSubmenuRoot(kind.ToString());
        build(_activeSubMenu);
    }

    void CloseSubMenu()
    {
        ClearActiveSubMenu();
        if (_pauseMainContent != null)
        {
            _pauseMainContent.SetActive(true);
        }
    }

    GameObject CreateSubmenuRoot(string name)
    {
        var submenu = new GameObject(name);
        submenu.transform.SetParent(_overlayRoot.transform, false);
        MenuUiFactory.StretchFull(submenu.AddComponent<RectTransform>());
        return submenu;
    }

    void OpenRespawnPicker()
    {
        if (GameSession.IsInPrepPhase)
        {
            return;
        }

        HideOverlayForChildPicker();
        _respawnPicker?.Show(
            onBack: RestoreOverlayFromChildPicker,
            onBeforeSelect: () => Hide(resumeGameplay: true));
    }

    void OpenCharacterPicker()
    {
        HideOverlayForChildPicker();
        _characterPicker?.Show(
            onBack: RestoreOverlayFromChildPicker,
            onBeforeSelect: () => Hide(resumeGameplay: true));
    }

    void HideOverlayForChildPicker()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    void RestoreOverlayFromChildPicker()
    {
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(true);
        }

        MenuUiFactory.EnsureEventSystem();
        GameUICanvas.BringLayerToFront(_layer);
        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);
    }

    void ShowDummyStats()
    {
        OpenSubMenu(PauseSubMenu.DummyStats, submenu =>
        {
            ShootingRangeDummyStatsPanel.BuildInto(submenu.transform, CloseSubMenu);
        });
    }

    void ResetMap()
    {
        ShootingRangeSession.ResetMap();
    }

    void ShowSettings()
    {
        EnsureSettingsSubscription();
        OpenSubMenu(PauseSubMenu.Settings, submenu =>
        {
            MenuUiFactory.CreateFullscreenDim(submenu.transform, 0.35f);
            var frame = MenuWindowFrame.CreateScreen(submenu.transform, "SETTINGS", showBack: true,
                "appearance · audio · controls", new Vector2(580f, 680f), showHeader: false,
                CloseSubMenu, animateFade: false);
            MenuSettingsPanel.Build(frame.Body, showAccountSection: false);
        });
    }

    void ShowExitMatchConfirm()
    {
        OpenSubMenu(PauseSubMenu.ExitConfirm, submenu =>
        {
            MenuUiFactory.CreateFullscreenDim(submenu.transform, 0.35f);
            var frame = MenuWindowFrame.CreateScreen(submenu.transform, "EXIT MATCH?", showBack: false,
                "you will return to the hub", new Vector2(480f, 320f), showHeader: false,
                CloseSubMenu, animateFade: false);

            MenuUiFactory.CreateButton(frame.Body, "Stay Button", "STAY",
                new Vector2(0f, 40f), MenuUiFactory.StandardButtonSize, CloseSubMenu);
            MenuUiFactory.CreateButton(frame.Body, "Exit Match Button", "EXIT MATCH",
                new Vector2(0f, -40f), MenuUiFactory.StandardButtonSize, () =>
                {
                    SceneFlow.EnterMainMenu();
                });
        });
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

        bool reopenSettings = _activeSubMenuKind == PauseSubMenu.Settings;
        ClearOverlayContent();
        BuildPauseMainContent();

        if (reopenSettings)
        {
            ShowSettings();
        }
        else if (_pauseMainContent != null)
        {
            _pauseMainContent.SetActive(true);
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

    void ClearActiveSubMenu()
    {
        if (_activeSubMenu != null)
        {
            Destroy(_activeSubMenu);
            _activeSubMenu = null;
        }

        _activeSubMenuKind = PauseSubMenu.None;
    }

    void ClearOverlayContent()
    {
        ClearActiveSubMenu();

        if (_pauseMainContent != null)
        {
            Destroy(_pauseMainContent);
            _pauseMainContent = null;
        }

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
