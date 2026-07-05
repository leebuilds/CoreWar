using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Routes between auth, hub, play, and decks screens with back-stack navigation.
/// </summary>
public class MenuNavigator : MonoBehaviour
{
    enum ScreenId
    {
        SignIn,
        SignUp,
        Hub,
        GameModes,
        Decks,
        Settings
    }

    RectTransform _root;
    Image _backdropImage;
    GameObject _screenRoot;
    MenuWindowFrame _window;
    InputField _usernameField;
    InputField _passcodeField;
    InputField _confirmPasscodeField;
    LoadoutSlotBar _decksLoadoutBar;
    CardPreviewPanel _previewPanel;
    GameObject _cardActionOverlay;
    CardDefinition _pendingActionCard;
    ScrollRect _decksScrollRect;

    readonly List<CardTileView> _deckCardTiles = new List<CardTileView>();
    readonly Stack<ScreenId> _backStack = new Stack<ScreenId>();
    ScreenId _currentScreen;
    int _sessionCheckGraceFrames;
    bool _bootstrapped;

    MatchmakingPanel _matchmakingPanel;
    MatchClassSelectPanel _classSelectPanel;
    GameObject _cancelMatchmakingOverlay;
    GameModeButtonFx _activeModeButtonFx;
    string _activeModeId;

    public static MenuNavigator Create()
    {
        RectTransform root;
        MenuUiFactory.CreateCanvas("Menu Canvas", out root);

        MenuSettings.EnsureLoaded();

        var backdropGo = new GameObject("Menu Backdrop");
        backdropGo.transform.SetParent(root, false);
        backdropGo.transform.SetAsFirstSibling();
        var backdropImage = backdropGo.AddComponent<Image>();
        backdropImage.raycastTarget = false;
        backdropImage.color = MenuUiFactory.Background;
        MenuUiFactory.StretchFull(backdropGo.GetComponent<RectTransform>());

        var navigatorGo = new GameObject("Menu Navigator");
        navigatorGo.transform.SetParent(root, false);
        MenuUiFactory.StretchFull(navigatorGo.AddComponent<RectTransform>());
        var navigator = navigatorGo.AddComponent<MenuNavigator>();
        navigator._backdropImage = backdropImage;
        navigator.Bootstrap();
        return navigator;
    }

    void Awake()
    {
        _root = transform as RectTransform;
    }

    void Bootstrap()
    {
        if (_bootstrapped)
        {
            return;
        }

        _bootstrapped = true;
        MenuSettings.EnsureLoaded();
        ApplyMenuBackground();
        ProfileSession.EnsureInitialized();
        ProfileSession.ValidateSessionOrLogout();
        MenuSettings.Changed += HandleSettingsChanged;

        MatchmakingSession.BindRunner(this);
        _matchmakingPanel = MatchmakingPanel.Create(_root, RequestCancelMatchmaking);
        _classSelectPanel = MatchClassSelectPanel.Create(_root);
        _classSelectPanel.Completed += HandlePrepComplete;
        MatchmakingSession.Completed += HandleMatchmakingCompleted;
        MatchmakingSession.Cancelled += HandleMatchmakingCancelled;

        if (ProfileSession.IsSignedIn)
        {
            ShowScreen(ScreenId.Hub, pushHistory: false);
        }
        else
        {
            ShowScreen(ScreenId.SignIn, pushHistory: false);
        }
    }

    void OnDestroy()
    {
        MenuSettings.Changed -= HandleSettingsChanged;
        MatchmakingSession.Completed -= HandleMatchmakingCompleted;
        MatchmakingSession.Cancelled -= HandleMatchmakingCancelled;

        if (_classSelectPanel != null)
        {
            _classSelectPanel.Completed -= HandlePrepComplete;
        }
    }

    void HandleSettingsChanged()
    {
        ApplyMenuBackground();
        if (_screenRoot == null)
        {
            return;
        }

        ShowScreen(_currentScreen, pushHistory: false);
    }

    void ApplyMenuBackground()
    {
        var background = MenuUiFactory.Background;
        if (_backdropImage != null)
        {
            _backdropImage.color = background;
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = background;
        }
    }

    void Update()
    {
        if (!SceneFlow.IsMainMenuActive)
        {
            return;
        }

        if (_currentScreen != ScreenId.SignIn && _currentScreen != ScreenId.SignUp)
        {
            if (_sessionCheckGraceFrames > 0)
            {
                _sessionCheckGraceFrames--;
            }
            else
            {
                ProfileSession.ValidateSessionOrLogout();
                if (!ProfileSession.IsSignedIn)
                {
                    _backStack.Clear();
                    ShowScreen(ScreenId.SignIn, pushHistory: false);
                    return;
                }
            }
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        HandleBackNavigation();
    }

    public void HandleBackNavigation()
    {
        if (_cancelMatchmakingOverlay != null)
        {
            HideCancelMatchmakingModal();
            return;
        }

        if (IsInMatchFlow())
        {
            ShowCancelMatchmakingModal(ConfirmCancelMatchFlow);
            return;
        }

        if (_previewPanel != null && _previewPanel.IsOpen)
        {
            _previewPanel.Hide();
            return;
        }

        if (_cardActionOverlay != null)
        {
            HideCardActionPanel();
            return;
        }

        GoBack();
    }

    void ShowScreen(ScreenId screen, bool pushHistory = true)
    {
        if (pushHistory && _screenRoot != null)
        {
            _backStack.Push(_currentScreen);
        }

        _currentScreen = screen;
        if (screen != ScreenId.SignIn && screen != ScreenId.SignUp)
        {
            ProfileSession.TouchActivity();
            _sessionCheckGraceFrames = 2;
        }

        DestroyScreen();
        BuildScreen(screen);
    }

    void GoBack()
    {
        _previewPanel?.Hide();
        HideCardActionPanel();

        if (_backStack.Count == 0)
        {
            if (_currentScreen == ScreenId.SignUp)
            {
                ShowScreen(ScreenId.SignIn, pushHistory: false);
            }

            return;
        }

        ShowScreen(_backStack.Pop(), pushHistory: false);
    }

    void DestroyScreen()
    {
        if (_screenRoot != null)
        {
            Destroy(_screenRoot);
            _screenRoot = null;
        }

        _window = null;
        _usernameField = null;
        _passcodeField = null;
        _confirmPasscodeField = null;
        _decksLoadoutBar = null;
        _previewPanel = null;
        _cardActionOverlay = null;
        _decksScrollRect = null;
        _deckCardTiles.Clear();
    }

    void BuildScreen(ScreenId screen)
    {
        _screenRoot = new GameObject($"Screen_{screen}");
        _screenRoot.transform.SetParent(_root, false);
        MenuUiFactory.StretchFull(_screenRoot.AddComponent<RectTransform>());

        switch (screen)
        {
            case ScreenId.SignIn: BuildSignIn(); break;
            case ScreenId.SignUp: BuildSignUp(); break;
            case ScreenId.Hub: BuildHub(); break;
            case ScreenId.GameModes: BuildGameModes(); break;
            case ScreenId.Decks: BuildDecks(); break;
            case ScreenId.Settings: BuildSettings(); break;
        }
    }

    void BuildSignIn()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "SIGN IN", showBack: false,
            string.Empty, new Vector2(560f, 520f), showHeader: false, null);

        _usernameField = MenuUiFactory.CreateInputField(_window.Body, "Username", "username",
            new Vector2(0f, 90f), MenuUiFactory.StandardInputSize);
        _passcodeField = MenuUiFactory.CreateInputField(_window.Body, "Passcode", "passcode",
            new Vector2(0f, 20f), MenuUiFactory.StandardInputSize, password: true);

        MenuUiFactory.CreateButton(_window.Body, "Sign In Button", "SIGN IN",
            new Vector2(0f, -60f), MenuUiFactory.StandardButtonSize, AttemptSignIn);
        MenuUiFactory.CreateTextLink(_window.Body, "Create Account Link", "create account",
            new Vector2(0f, -130f), MenuUiFactory.TextLinkSize, () => ShowScreen(ScreenId.SignUp));
        MenuUiFactory.CreateButton(_window.Body, "Quit Button", "QUIT",
            new Vector2(0f, -190f), MenuUiFactory.StandardButtonSize, MenuUiFactory.QuitApplication);
    }

    void BuildSignUp()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "CREATE ACCOUNT", showBack: true,
            string.Empty, new Vector2(560f, 580f), showHeader: false, GoBack);

        _usernameField = MenuUiFactory.CreateInputField(_window.Body, "Username", "unique username",
            new Vector2(0f, 120f), MenuUiFactory.StandardInputSize);
        _passcodeField = MenuUiFactory.CreateInputField(_window.Body, "Passcode", "passcode",
            new Vector2(0f, 50f), MenuUiFactory.StandardInputSize, password: true);
        _confirmPasscodeField = MenuUiFactory.CreateInputField(_window.Body, "Confirm Passcode", "confirm passcode",
            new Vector2(0f, -20f), MenuUiFactory.StandardInputSize, password: true);

        MenuUiFactory.CreateButton(_window.Body, "Create Button", "CREATE",
            new Vector2(0f, -100f), MenuUiFactory.StandardButtonSize, AttemptSignUp);
    }

    void BuildHub()
    {
        var username = ProfileSession.ActiveProfile?.username ?? "player";
        bool canPlay = ProfileSession.HasCompleteLoadout;
        string footer = canPlay
            ? $"welcome, {username} · loadout ready"
            : $"welcome, {username} · choose two cards in decks";

        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "COREWAR", showBack: false,
            footer, new Vector2(480f, 640f), showHeader: false, null);

        MenuUiFactory.CreateText(_window.Body, "Welcome", $"welcome, {username}",
            MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 170f), new Vector2(420f, 40f));

        MenuUiFactory.CreateButton(_window.Body, "Play Button", "PLAY",
            new Vector2(0f, 70f), MenuUiFactory.StandardButtonSize, () => ShowScreen(ScreenId.GameModes), enabled: canPlay);
        MenuUiFactory.CreateButton(_window.Body, "Decks Button", "DECKS",
            new Vector2(0f, -10f), MenuUiFactory.StandardButtonSize, () => ShowScreen(ScreenId.Decks));
        MenuUiFactory.CreateButton(_window.Body, "Settings Button", "SETTINGS",
            new Vector2(0f, -90f), MenuUiFactory.StandardButtonSize, () => ShowScreen(ScreenId.Settings));
        MenuUiFactory.CreateButton(_window.Body, "Logout Button", "LOGOUT",
            new Vector2(0f, -170f), MenuUiFactory.StandardButtonSize, Logout);
        MenuUiFactory.CreateButton(_window.Body, "Quit Button", "QUIT",
            new Vector2(0f, -250f), MenuUiFactory.StandardButtonSize, MenuUiFactory.QuitApplication);
    }

    void BuildSettings()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "SETTINGS", showBack: true,
            "appearance · audio · controls", new Vector2(580f, 680f), showHeader: false, GoBack);

        MenuSettingsPanel.Build(_window.Body, showAccountSection: true);
    }

    void BuildGameModes()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "GAME MODES", showBack: true,
            "select a mode to search for a match", new Vector2(560f, 640f), showHeader: false, GoBackFromGameModes);

        var viewportGo = new GameObject("Modes Viewport");
        viewportGo.transform.SetParent(_window.Body, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(viewportRect);

        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = MenuUiFactory.ScrollViewportFill;
        viewportGo.AddComponent<Mask>().showMaskGraphic = true;

        var contentGo = new GameObject("Modes Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < GameModeDefinition.Catalog.Count; i++)
        {
            CreateGameModeRow(contentGo.transform, GameModeDefinition.Catalog[i]);
        }

        var scrollRect = viewportGo.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void CreateGameModeRow(Transform parent, GameModeDefinition mode)
    {
        var rowGo = new GameObject($"Mode_{mode.id}");
        rowGo.transform.SetParent(parent, false);
        var rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 64f);

        var rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 64f;
        rowLayout.minHeight = 64f;

        GameModeButtonFx fx = null;
        var button = MenuUiFactory.CreateBodyButton(rowGo.transform, $"Mode Button {mode.displayName}", mode.displayName,
            Vector2.zero, new Vector2(480f, MenuUiFactory.CompactControlHeight),
            () => OnGameModeSelected(mode, fx), enabled: ProfileSession.HasCompleteLoadout);
        fx = GameModeButtonFx.Attach(button);

        var buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
    }

    void GoBackFromGameModes()
    {
        if (IsInMatchFlow())
        {
            ShowCancelMatchmakingModal(ConfirmCancelMatchFlow);
            return;
        }

        GoBack();
    }

    bool IsInMatchFlow()
    {
        return MatchmakingSession.IsActive || (_classSelectPanel != null && _classSelectPanel.IsOpen);
    }

    void OnGameModeSelected(GameModeDefinition mode, GameModeButtonFx fx)
    {
        if (mode == null || !ProfileSession.HasCompleteLoadout)
        {
            return;
        }

        if (_activeModeId == mode.id && IsInMatchFlow())
        {
            return;
        }

        if (IsInMatchFlow())
        {
            ShowCancelMatchmakingModal(() =>
            {
                ConfirmCancelMatchFlow();
                BeginMatchmaking(mode, fx);
            });
            return;
        }

        BeginMatchmaking(mode, fx);
    }

    void BeginMatchmaking(GameModeDefinition mode, GameModeButtonFx fx)
    {
        ProfileSession.TouchActivity();
        _activeModeId = mode.id;
        _activeModeButtonFx = fx;
        fx?.PlayBurst();
        MatchmakingSession.Start(mode);
        _matchmakingPanel.Show();
    }

    void RequestCancelMatchmaking()
    {
        if (MatchmakingSession.IsActive)
        {
            MatchmakingSession.Cancel();
            return;
        }

        if (_classSelectPanel != null && _classSelectPanel.IsOpen)
        {
            ConfirmCancelMatchFlow();
        }
    }

    void HandleMatchmakingCompleted()
    {
        _matchmakingPanel.Hide();
        _classSelectPanel.Show(() =>
        {
            ShowCancelMatchmakingModal(() =>
            {
                ConfirmCancelMatchFlow();
                ShowScreen(ScreenId.Decks);
            });
        });
    }

    void HandleMatchmakingCancelled()
    {
        CleanupMatchFlow();
    }

    void ConfirmCancelMatchFlow()
    {
        if (MatchmakingSession.IsActive)
        {
            MatchmakingSession.Cancel();
            return;
        }

        CleanupMatchFlow();
    }

    void CleanupMatchFlow()
    {
        StopActiveModeFx();
        _matchmakingPanel?.Hide();
        _classSelectPanel?.Hide();
        _activeModeId = null;
        _activeModeButtonFx = null;
        MatchmakingSession.Reset();
    }

    void StopActiveModeFx()
    {
        _activeModeButtonFx?.StopFx();
        _activeModeButtonFx = null;
    }

    void HandlePrepComplete(int spawnSlotIndex)
    {
        if (!ProfileSession.HasCompleteLoadout)
        {
            CleanupMatchFlow();
            return;
        }

        StopActiveModeFx();
        _classSelectPanel.Hide();
        _matchmakingPanel.Hide();

        var profile = ProfileSession.ActiveProfile;
        var mode = GameModeDefinition.Get(_activeModeId);
        ProfileSession.TouchActivity();
        GameSession.BeginMatch(
            GameSession.Team.Red,
            profile.loadoutCardIds[0],
            profile.loadoutCardIds[1],
            profile.loadoutCardIds[spawnSlotIndex],
            _activeModeId,
            mode?.requiredPlayers ?? 1);
        MenuUiSounds.PlayGunshot();
        SceneFlow.EnterGameFromPrep();
        MatchmakingSession.Reset();
        _activeModeId = null;
    }

    void ShowCancelMatchmakingModal(Action onConfirmCancel)
    {
        if (_cancelMatchmakingOverlay != null)
        {
            return;
        }

        var frame = MenuWindowFrame.CreateModal(_root, "CANCEL MATCHMAKING?", showBack: false,
            "leaving will stop search", new Vector2(480f, 320f), HideCancelMatchmakingModal);
        _cancelMatchmakingOverlay = frame.transform.parent.gameObject;

        MenuUiFactory.CreateButton(frame.Body, "Stay Button", "STAY",
            new Vector2(0f, 40f), MenuUiFactory.StandardButtonSize, HideCancelMatchmakingModal);
        MenuUiFactory.CreateButton(frame.Body, "Cancel Search Button", "CANCEL SEARCH",
            new Vector2(0f, -40f), MenuUiFactory.StandardButtonSize, () =>
            {
                HideCancelMatchmakingModal();
                onConfirmCancel?.Invoke();
            });
    }

    void HideCancelMatchmakingModal()
    {
        if (_cancelMatchmakingOverlay != null)
        {
            Destroy(_cancelMatchmakingOverlay);
            _cancelMatchmakingOverlay = null;
        }
    }

    void BuildDecks()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "DECKS", showBack: true,
            "pick two cards for your loadout", new Vector2(1320f, 820f), showHeader: true, GoBack);

        _decksLoadoutBar = LoadoutSlotBar.Create(_window.Header, ClearLoadoutSlot);
        _previewPanel = CardPreviewPanel.Create(_screenRoot.transform);
        BuildDecksScrollArea();
        StartCoroutine(ScrollDecksToTopNextFrame());
    }

    IEnumerator ScrollDecksToTopNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_decksScrollRect != null)
        {
            _decksScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    void BuildDecksScrollArea()
    {
        var viewportGo = new GameObject("Decks Viewport");
        viewportGo.transform.SetParent(_window.Body, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(viewportRect);

        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = MenuUiFactory.ScrollViewportFill;
        viewportGo.AddComponent<Mask>().showMaskGraphic = true;

        var contentGo = new GameObject("Decks Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var specialtyKeys = new[]
        {
            "infantry", "sniper", "engineer", "support", "assault",
            "assassin", "heavy", "demolition", "saboteur", "gunner"
        };

        foreach (var specialtyKey in specialtyKeys)
        {
            CreateSpecialtyRow(contentGo.transform, specialtyKey);
        }

        _decksScrollRect = viewportGo.AddComponent<ScrollRect>();
        _decksScrollRect.content = contentRect;
        _decksScrollRect.viewport = viewportRect;
        _decksScrollRect.horizontal = false;
        _decksScrollRect.vertical = true;
        _decksScrollRect.movementType = ScrollRect.MovementType.Clamped;
        _decksScrollRect.scrollSensitivity = 40f;
        _decksScrollRect.verticalNormalizedPosition = 1f;

        RefreshDeckCardLoadoutStates();
    }

    void CreateSpecialtyRow(Transform parent, string specialtyKey)
    {
        CardDefinition tier1 = null;
        CardDefinition tier2 = null;
        CardDefinition tier3 = null;
        string specialtyLabel = specialtyKey;

        foreach (var card in CardCatalog.ForSpecialty(specialtyKey))
        {
            specialtyLabel = card.specialtyLabel;
            switch (card.tier)
            {
                case 1: tier1 = card; break;
                case 2: tier2 = card; break;
                case 3: tier3 = card; break;
            }
        }

        var rowGo = new GameObject($"Row_{specialtyKey}");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 210f);

        var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        CreateRowLabel(rowGo.transform, specialtyLabel);
        CreateRowCardCell(rowGo.transform, tier1);
        CreateRowArrow(rowGo.transform);
        CreateRowCardCell(rowGo.transform, tier2);
        CreateRowArrow(rowGo.transform);
        CreateRowCardCell(rowGo.transform, tier3);
    }

    static void CreateRowLabel(Transform parent, string specialtyLabel)
    {
        var labelGo = new GameObject("Specialty Label");
        labelGo.transform.SetParent(parent, false);
        labelGo.AddComponent<RectTransform>().sizeDelta = new Vector2(120f, 180f);
        MenuUiFactory.CreateAnchoredText(labelGo.transform, "Text", specialtyLabel.ToUpperInvariant(),
            MenuUiFactory.SmallFontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    void CreateRowCardCell(Transform parent, CardDefinition card)
    {
        if (card == null)
        {
            return;
        }

        bool owned = ProfileSession.OwnsCard(card.id);
        var tile = CardTileView.Create(parent, card, owned, () => OpenCardAction(card));
        _deckCardTiles.Add(tile);
        tile.SetInLoadout(IsCardInLoadout(card.id));
    }

    static void CreateRowArrow(Transform parent)
    {
        var arrowGo = new GameObject("Tier Arrow");
        arrowGo.transform.SetParent(parent, false);
        arrowGo.AddComponent<RectTransform>().sizeDelta = new Vector2(36f, 180f);
        MenuUiFactory.CreateAnchoredText(arrowGo.transform, "Arrow", "→", 34, FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    void OpenCardAction(CardDefinition card)
    {
        if (card == null || !ProfileSession.OwnsCard(card.id))
        {
            return;
        }

        ProfileSession.TouchActivity();
        _pendingActionCard = card;
        HideCardActionPanel();

        var frame = MenuWindowFrame.CreateModal(_screenRoot.transform, card.displayName, showBack: true,
            $"{CardRarityColors.Label(card.rarity)} · {card.specialtyLabel.ToUpperInvariant()} · TIER {card.tier}",
            new Vector2(520f, 460f), HideCardActionPanel);
        _cardActionOverlay = frame.transform.parent.gameObject;

        var buttonPanel = new GameObject("Action Buttons");
        buttonPanel.transform.SetParent(frame.Body, false);
        var panelRect = buttonPanel.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(panelRect);

        MenuUiFactory.CreateButton(buttonPanel.transform, "Preview", "PREVIEW",
            new Vector2(0f, 90f), MenuUiFactory.StandardButtonSize, OpenPreviewFromAction);
        MenuUiFactory.CreateButton(buttonPanel.transform, "Select Slot 1", "SELECT SLOT 1",
            new Vector2(0f, 10f), MenuUiFactory.StandardButtonSize, () => SelectPendingCard(0));
        MenuUiFactory.CreateButton(buttonPanel.transform, "Select Slot 2", "SELECT SLOT 2",
            new Vector2(0f, -70f), MenuUiFactory.StandardButtonSize, () => SelectPendingCard(1));
    }

    void HideCardActionPanel()
    {
        if (_cardActionOverlay != null)
        {
            Destroy(_cardActionOverlay);
            _cardActionOverlay = null;
        }
    }

    void OpenPreviewFromAction()
    {
        if (_pendingActionCard == null || _previewPanel == null)
        {
            return;
        }

        var card = _pendingActionCard;
        HideCardActionPanel();
        _previewPanel.Show(card, () => _previewPanel.Hide(),
            slotIndex =>
            {
                ProfileSession.SetLoadoutSlot(slotIndex, card.id);
                RefreshDecksUi();
                _previewPanel.Hide();
            });
    }

    void SelectPendingCard(int slotIndex)
    {
        if (_pendingActionCard == null)
        {
            return;
        }

        ProfileSession.SetLoadoutSlot(slotIndex, _pendingActionCard.id);
        RefreshDecksUi();
        HideCardActionPanel();
    }

    void ClearLoadoutSlot(int slotIndex)
    {
        ProfileSession.ClearLoadoutSlot(slotIndex);
        RefreshDecksUi();
    }

    void RefreshDecksUi()
    {
        _decksLoadoutBar?.Refresh();
        RefreshDeckCardLoadoutStates();
    }

    void RefreshDeckCardLoadoutStates()
    {
        foreach (var tile in _deckCardTiles)
        {
            if (tile == null || tile.Card == null)
            {
                continue;
            }

            tile.SetInLoadout(IsCardInLoadout(tile.Card.id));
        }
    }

    bool IsCardInLoadout(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return false;
        }

        var profile = ProfileSession.ActiveProfile;
        if (profile?.loadoutCardIds == null)
        {
            return false;
        }

        for (int i = 0; i < profile.loadoutCardIds.Length; i++)
        {
            if (profile.loadoutCardIds[i] == cardId)
            {
                return true;
            }
        }

        return false;
    }

    void AttemptSignIn()
    {
        SetError(string.Empty);
        if (!ProfileSession.Repository.TrySignIn(_usernameField.text, _passcodeField.text,
                out var profile, out var error))
        {
            SetError(error);
            return;
        }

        ProfileSession.SignIn(profile);
        if (!ProfileSession.IsSignedIn)
        {
            SetError("Could not start session. Try again.");
            return;
        }

        _backStack.Clear();
        ShowScreen(ScreenId.Hub, pushHistory: false);
    }

    void AttemptSignUp()
    {
        SetError(string.Empty);
        if (_passcodeField.text != _confirmPasscodeField.text)
        {
            SetError("Passcodes do not match.");
            return;
        }

        if (!ProfileSession.Repository.TryCreateProfile(_usernameField.text, _passcodeField.text,
                out var profile, out var error))
        {
            SetError(error);
            return;
        }

        ProfileSession.SignIn(profile);
        if (!ProfileSession.IsSignedIn)
        {
            SetError("Could not start session. Try again.");
            return;
        }

        _backStack.Clear();
        ShowScreen(ScreenId.Hub, pushHistory: false);
    }

    void Logout()
    {
        CleanupMatchFlow();
        ProfileSession.Logout();
        _backStack.Clear();
        ShowScreen(ScreenId.SignIn, pushHistory: false);
    }

    void SetError(string message)
    {
        if (_window != null)
        {
            _window.SetFooterText(message, isError: !string.IsNullOrEmpty(message));
        }
    }
}
