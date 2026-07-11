using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Post-matchmaking class select with READY button and 10-second prep countdown.
/// </summary>
public class MatchClassSelectPanel : MonoBehaviour
{
    const float PrepDuration = 10f;

    public event Action<int> Completed;
    public event Action<int> ReadyPressed;

    GameObject _visualRoot;
    GameObject _root;
    GameObject _readyBanner;
    Text _prepTimerText;
    Text _readyBannerText;
    Button _readyButton;
    CardTileView _leftTile;
    CardTileView _rightTile;
    Button _editDecksLink;
    int _spawnSlotIndex;
    UnityAction _onEditDecks;
    bool _isReady;
    bool _completed;
    Coroutine _prepRoutine;
    float _remainingSeconds;

    public bool IsOpen => gameObject.activeSelf;

    public static MatchClassSelectPanel Create(Transform parent)
    {
        var host = new GameObject("Match Class Select Host");
        host.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(host.AddComponent<RectTransform>());

        var panel = host.AddComponent<MatchClassSelectPanel>();
        panel.Build();
        host.SetActive(false);
        return panel;
    }

    void Build()
    {
        _visualRoot = new GameObject("Theme Visuals");
        _visualRoot.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(_visualRoot.AddComponent<RectTransform>());

        BuildReadyBanner();

        _root = new GameObject("Panel Root");
        _root.transform.SetParent(_visualRoot.transform, false);
        var rootRect = _root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(980f, 560f);
        rootRect.anchoredPosition = Vector2.zero;

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(_root.transform, false);
        MenuUiFactory.StretchFull(borderGo.AddComponent<RectTransform>());
        borderGo.AddComponent<Image>().color = MenuUiFactory.Ink;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_root.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fillGo.AddComponent<Image>().color = MenuUiFactory.PanelFill;

        MenuUiFactory.BuildMilitaryTitleBar(fillGo.transform, 0f, "MATCH PREP", showBack: false, null, out _);

        var prepBar = new GameObject("Prep Timer Bar");
        prepBar.transform.SetParent(fillGo.transform, false);
        var prepRect = prepBar.AddComponent<RectTransform>();
        prepRect.anchorMin = new Vector2(0.08f, 1f);
        prepRect.anchorMax = new Vector2(0.92f, 1f);
        prepRect.pivot = new Vector2(0.5f, 1f);
        prepRect.sizeDelta = new Vector2(0f, 44f);
        prepRect.anchoredPosition = new Vector2(0f, -(MenuUiFactory.TitleBarHeight + 8f));

        var prepBorder = prepBar.AddComponent<Image>();
        prepBorder.color = MenuUiFactory.Ink;

        var prepInner = new GameObject("Inner");
        prepInner.transform.SetParent(prepBar.transform, false);
        var prepInnerRect = prepInner.AddComponent<RectTransform>();
        prepInnerRect.anchorMin = Vector2.zero;
        prepInnerRect.anchorMax = Vector2.one;
        prepInnerRect.offsetMin = new Vector2(2f, 2f);
        prepInnerRect.offsetMax = new Vector2(-2f, -2f);
        prepInner.AddComponent<Image>().color = MenuUiFactory.PanelFill;

        _prepTimerText = MenuUiFactory.CreateAnchoredText(prepInner.transform, "Prep Timer", "starting in 10",
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, MenuUiFactory.Ink);

        var profile = ProfileSession.ActiveProfile;
        var leftCard = CardCatalog.Get(profile.loadoutCardIds[0]);
        var rightCard = CardCatalog.Get(profile.loadoutCardIds[1]);

        _spawnSlotIndex = 0;
        _leftTile = CreateCardTile(fillGo.transform, leftCard, 0, new Vector2(-300f, -20f));
        _rightTile = CreateCardTile(fillGo.transform, rightCard, 1, new Vector2(300f, -20f));
        RefreshSpawnSelection();

        MenuUiFactory.CreateText(fillGo.transform, "Spawn Hint", "tap a card — the highlighted one is your spawn class",
            MenuUiFactory.HintFontSize, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0f, -190f), new Vector2(520f, 40f), MenuUiFactory.MutedInk);

        _readyButton = MenuUiFactory.CreateButton(fillGo.transform, "Ready Button", "READY",
            new Vector2(0f, -40f), MenuUiFactory.PrimaryButtonSize, OnReadyClicked,
            enabled: ProfileSession.HasCompleteLoadout);

        _editDecksLink = MenuUiFactory.CreateTextLink(fillGo.transform, "Edit Link", "edit in decks",
            new Vector2(0f, -110f), MenuUiFactory.TextLinkSize, () => _onEditDecks?.Invoke());
    }

    void BuildReadyBanner()
    {
        _readyBanner = new GameObject("Ready Banner");
        _readyBanner.transform.SetParent(_visualRoot.transform, false);
        var bannerRect = _readyBanner.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.sizeDelta = new Vector2(360f, 44f);
        bannerRect.anchoredPosition = new Vector2(0f, -20f);

        var borderGo = new GameObject("Border");
        borderGo.transform.SetParent(_readyBanner.transform, false);
        MenuUiFactory.StretchFull(borderGo.AddComponent<RectTransform>());
        borderGo.AddComponent<Image>().color = MenuUiFactory.Ink;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_readyBanner.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        fillGo.AddComponent<Image>().color = MenuUiFactory.PanelFill;

        _readyBannerText = MenuUiFactory.CreateAnchoredText(fillGo.transform, "Ready Banner Text", "ready · 0:10",
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, MenuUiFactory.Ink);
        _readyBanner.SetActive(false);
    }

    void OnEnable()
    {
        MenuSettings.Changed += HandleThemeChanged;
    }

    void OnDisable()
    {
        MenuSettings.Changed -= HandleThemeChanged;
    }

    void HandleThemeChanged()
    {
        int selectedSlot = _spawnSlotIndex;
        bool showMainWindow = _root != null && _root.activeSelf;
        bool showReadyBanner = _readyBanner != null && _readyBanner.activeSelf;

        if (_visualRoot != null)
        {
            Destroy(_visualRoot);
        }

        Build();
        _spawnSlotIndex = selectedSlot;
        RefreshSpawnSelection();

        if (_readyButton != null)
        {
            _readyButton.interactable = !_isReady && ProfileSession.HasCompleteLoadout;
        }

        if (_editDecksLink != null)
        {
            _editDecksLink.gameObject.SetActive(_onEditDecks != null && !_isReady);
        }

        _root?.SetActive(showMainWindow);
        _readyBanner?.SetActive(showReadyBanner);
        UpdateTimerLabels();
    }

    CardTileView CreateCardTile(Transform parent, CardDefinition card, int slotIndex, Vector2 position)
    {
        if (card == null)
        {
            return null;
        }

        var tile = CardTileView.Create(parent, card, owned: true, () => SelectSpawnSlot(slotIndex));
        var rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        return tile;
    }

    void SelectSpawnSlot(int slotIndex)
    {
        if (_isReady)
        {
            return;
        }

        _spawnSlotIndex = Mathf.Clamp(slotIndex, 0, 1);
        RefreshSpawnSelection();
    }

    void RefreshSpawnSelection()
    {
        bool leftSelected = _spawnSlotIndex == 0;
        bool rightSelected = _spawnSlotIndex == 1;
        _leftTile?.SetSpawnSelected(leftSelected);
        _rightTile?.SetSpawnSelected(rightSelected);
        _leftTile?.SetSpawnDimmed(!leftSelected);
        _rightTile?.SetSpawnDimmed(!rightSelected);
    }

    public void Show(UnityAction onEditDecks = null)
    {
        gameObject.SetActive(true);
        _onEditDecks = onEditDecks;
        _isReady = false;
        _completed = false;
        _spawnSlotIndex = 0;
        RefreshSpawnSelection();

        if (_readyButton != null)
        {
            _readyButton.interactable = ProfileSession.HasCompleteLoadout;
        }

        if (_editDecksLink != null)
        {
            _editDecksLink.gameObject.SetActive(_onEditDecks != null);
        }

        if (_prepRoutine != null)
        {
            StopCoroutine(_prepRoutine);
        }

        if (_root != null)
        {
            _root.SetActive(true);
        }

        if (_readyBanner != null)
        {
            _readyBanner.SetActive(false);
        }

        transform.SetAsLastSibling();
        _prepRoutine = StartCoroutine(PrepCountdown());
    }

    public void Hide()
    {
        if (_prepRoutine != null)
        {
            StopCoroutine(_prepRoutine);
            _prepRoutine = null;
        }

        _completed = false;

        if (_readyBanner != null)
        {
            _readyBanner.SetActive(false);
        }

        if (_root != null)
        {
            _root.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    IEnumerator PrepCountdown()
    {
        _remainingSeconds = PrepDuration;

        while (_remainingSeconds > 0f && !_completed)
        {
            UpdateTimerLabels();
            yield return null;
            _remainingSeconds -= Time.unscaledDeltaTime;
        }

        if (_readyBannerText != null)
        {
            _readyBannerText.text = "starting match";
        }

        if (_prepTimerText != null)
        {
            _prepTimerText.text = "starting match";
        }

        CompleteSelection();
    }

    void UpdateTimerLabels()
    {
        int seconds = Mathf.CeilToInt(_remainingSeconds);
        string timerLabel = FormatPrepSeconds(seconds);

        if (_prepTimerText != null)
        {
            _prepTimerText.text = _isReady
                ? $"ready · {timerLabel}"
                : $"starting in {seconds}";
        }

        if (_readyBannerText != null && _isReady)
        {
            _readyBannerText.text = $"ready · {timerLabel}";
        }
    }

    static string FormatPrepSeconds(int seconds)
    {
        return $"0:{Mathf.Max(0, seconds):00}";
    }

    void OnReadyClicked()
    {
        if (_isReady || _completed || !ProfileSession.HasCompleteLoadout)
        {
            return;
        }

        _isReady = true;
        if (_readyButton != null)
        {
            _readyButton.interactable = false;
        }

        if (_root != null)
        {
            _root.SetActive(false);
        }

        if (_readyBanner != null)
        {
            _readyBanner.SetActive(true);
            UpdateTimerLabels();
        }

        var profile = ProfileSession.ActiveProfile;
        var activeCardId = profile.loadoutCardIds[_spawnSlotIndex];
        GameSession.MarkPrepReady(activeCardId);
        ReadyPressed?.Invoke(_spawnSlotIndex);
    }

    void CompleteSelection()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        Completed?.Invoke(_spawnSlotIndex);
    }
}
