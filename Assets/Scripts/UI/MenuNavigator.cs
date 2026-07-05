using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        Play,
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
    float _decksScrollPosition = 1f;
    ScrollRect _decksScrollRect;

    readonly List<CardTileView> _deckCardTiles = new List<CardTileView>();
    readonly Stack<ScreenId> _backStack = new Stack<ScreenId>();
    ScreenId _currentScreen;
    int _playSpawnSlotIndex;
    CardTileView _playLeftTile;
    CardTileView _playRightTile;
    int _sessionCheckGraceFrames;
    bool _bootstrapped;

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
            case ScreenId.Play: BuildPlay(); break;
            case ScreenId.Decks: BuildDecks(); break;
            case ScreenId.Settings: BuildSettings(); break;
        }
    }

    void BuildSignIn()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "SIGN IN", showBack: false,
            string.Empty, new Vector2(560f, 520f), showHeader: false, null);

        _usernameField = MenuUiFactory.CreateInputField(_window.Body, "Username", "username",
            new Vector2(0f, 90f), new Vector2(420f, 52f));
        _passcodeField = MenuUiFactory.CreateInputField(_window.Body, "Passcode", "passcode",
            new Vector2(0f, 20f), new Vector2(420f, 52f), password: true);

        MenuUiFactory.CreateButton(_window.Body, "Sign In Button", "SIGN IN",
            new Vector2(0f, -60f), new Vector2(320f, 64f), AttemptSignIn);
        MenuUiFactory.CreateTextLink(_window.Body, "Create Account Link", "create account",
            new Vector2(0f, -130f), new Vector2(320f, 44f), () => ShowScreen(ScreenId.SignUp));
        MenuUiFactory.CreateButton(_window.Body, "Quit Button", "QUIT",
            new Vector2(0f, -190f), new Vector2(320f, 64f), MenuUiFactory.QuitApplication);
    }

    void BuildSignUp()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "CREATE ACCOUNT", showBack: true,
            string.Empty, new Vector2(560f, 580f), showHeader: false, GoBack);

        _usernameField = MenuUiFactory.CreateInputField(_window.Body, "Username", "unique username",
            new Vector2(0f, 120f), new Vector2(420f, 52f));
        _passcodeField = MenuUiFactory.CreateInputField(_window.Body, "Passcode", "passcode",
            new Vector2(0f, 50f), new Vector2(420f, 52f), password: true);
        _confirmPasscodeField = MenuUiFactory.CreateInputField(_window.Body, "Confirm Passcode", "confirm passcode",
            new Vector2(0f, -20f), new Vector2(420f, 52f), password: true);

        MenuUiFactory.CreateButton(_window.Body, "Create Button", "CREATE",
            new Vector2(0f, -100f), new Vector2(320f, 64f), AttemptSignUp);
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
            new Vector2(0f, 70f), new Vector2(320f, 64f), () => ShowScreen(ScreenId.Play), enabled: canPlay);
        MenuUiFactory.CreateButton(_window.Body, "Decks Button", "DECKS",
            new Vector2(0f, -10f), new Vector2(320f, 64f), () => ShowScreen(ScreenId.Decks));
        MenuUiFactory.CreateButton(_window.Body, "Settings Button", "SETTINGS",
            new Vector2(0f, -90f), new Vector2(320f, 64f), () => ShowScreen(ScreenId.Settings));
        MenuUiFactory.CreateButton(_window.Body, "Logout Button", "LOGOUT",
            new Vector2(0f, -170f), new Vector2(320f, 64f), Logout);
        MenuUiFactory.CreateButton(_window.Body, "Quit Button", "QUIT",
            new Vector2(0f, -250f), new Vector2(320f, 64f), MenuUiFactory.QuitApplication);
    }

    void BuildSettings()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "SETTINGS", showBack: true,
            "appearance · audio · controls", new Vector2(580f, 680f), showHeader: false, GoBack);

        MenuSettingsPanel.Build(_window.Body, showAccountSection: true);
    }

    void BuildPlay()
    {
        _window = MenuWindowFrame.CreateScreen(_screenRoot.transform, "PLAY", showBack: true,
            "team red · confirm loadout · quick play only (for now)", new Vector2(980f, 560f), showHeader: false, GoBack);

        var profile = ProfileSession.ActiveProfile;
        var leftCard = CardCatalog.Get(profile.loadoutCardIds[0]);
        var rightCard = CardCatalog.Get(profile.loadoutCardIds[1]);

        _playSpawnSlotIndex = 0;
        _playLeftTile = CreatePlayCardTile(_window.Body, leftCard, 0, new Vector2(-300f, 20f));
        _playRightTile = CreatePlayCardTile(_window.Body, rightCard, 1, new Vector2(300f, 20f));
        RefreshPlaySpawnSelection();

        MenuUiFactory.CreateText(_window.Body, "Spawn Hint", "tap a card — the highlighted one is your spawn class",
            MenuUiFactory.HintFontSize, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -150f), new Vector2(520f, 40f),
            MenuUiFactory.MutedInk);

        bool canPlay = ProfileSession.HasCompleteLoadout;
        MenuUiFactory.CreateButton(_window.Body, "Start Button", "PLAY",
            new Vector2(0f, 20f), new Vector2(320f, 80f), StartMatch, enabled: canPlay);
        MenuUiFactory.CreateTextLink(_window.Body, "Edit Link", "edit in decks",
            new Vector2(0f, -80f), new Vector2(320f, 44f), () => ShowScreen(ScreenId.Decks));
    }

    void BuildDecks()
    {
        _decksScrollPosition = 1f;
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
            new Vector2(0f, 90f), new Vector2(320f, 60f), OpenPreviewFromAction);
        MenuUiFactory.CreateButton(buttonPanel.transform, "Select Slot 1", "SELECT SLOT 1",
            new Vector2(0f, 10f), new Vector2(320f, 60f), () => SelectPendingCard(0));
        MenuUiFactory.CreateButton(buttonPanel.transform, "Select Slot 2", "SELECT SLOT 2",
            new Vector2(0f, -70f), new Vector2(320f, 60f), () => SelectPendingCard(1));
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
        if (_decksScrollRect != null)
        {
            _decksScrollPosition = _decksScrollRect.verticalNormalizedPosition;
        }
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

    CardTileView CreatePlayCardTile(Transform parent, CardDefinition card, int slotIndex, Vector2 position)
    {
        if (card == null)
        {
            MenuUiFactory.CreateText(parent, "Missing Card", "EMPTY",
                24, FontStyle.Bold, TextAnchor.MiddleCenter, position, new Vector2(280f, 180f));
            return null;
        }

        var tile = CardTileView.Create(parent, card, owned: true, () => SelectPlaySpawnSlot(slotIndex));
        var rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        return tile;
    }

    void SelectPlaySpawnSlot(int slotIndex)
    {
        _playSpawnSlotIndex = Mathf.Clamp(slotIndex, 0, 1);
        RefreshPlaySpawnSelection();
    }

    void RefreshPlaySpawnSelection()
    {
        bool leftSelected = _playSpawnSlotIndex == 0;
        bool rightSelected = _playSpawnSlotIndex == 1;
        _playLeftTile?.SetSpawnSelected(leftSelected);
        _playRightTile?.SetSpawnSelected(rightSelected);
        _playLeftTile?.SetSpawnDimmed(!leftSelected);
        _playRightTile?.SetSpawnDimmed(!rightSelected);

        if (_window != null)
        {
            var profile = ProfileSession.ActiveProfile;
            if (profile?.loadoutCardIds != null && profile.loadoutCardIds.Length > 1)
            {
                var card = CardCatalog.Get(profile.loadoutCardIds[_playSpawnSlotIndex]);
                var label = card != null ? card.displayName.ToUpperInvariant() : "UNKNOWN";
                _window.SetFooterText($"spawning as {label} · team red · quick play only (for now)");
            }
        }
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
        ProfileSession.Logout();
        _backStack.Clear();
        ShowScreen(ScreenId.SignIn, pushHistory: false);
    }

    void StartMatch()
    {
        if (!ProfileSession.HasCompleteLoadout)
        {
            return;
        }

        var profile = ProfileSession.ActiveProfile;
        ProfileSession.TouchActivity();
        GameSession.BeginMatch(
            GameSession.Team.Red,
            profile.loadoutCardIds[0],
            profile.loadoutCardIds[1],
            profile.loadoutCardIds[_playSpawnSlotIndex]);
        MenuUiSounds.PlayGunshot();
        SceneManager.LoadScene("Game");
    }

    void SetError(string message)
    {
        if (_window != null)
        {
            _window.SetFooterText(message, isError: !string.IsNullOrEmpty(message));
        }
    }
}
