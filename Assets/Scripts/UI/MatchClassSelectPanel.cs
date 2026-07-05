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

    GameObject _root;
    Text _prepTimerText;
    Button _readyButton;
    CardTileView _leftTile;
    CardTileView _rightTile;
    int _spawnSlotIndex;
    UnityAction _onEditDecks;
    bool _isReady;
    bool _completed;
    bool _prepRunning;
    Coroutine _prepRoutine;

    public bool IsOpen => _root != null && _root.activeSelf;

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
        var blocker = new GameObject("Input Blocker");
        blocker.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(blocker.AddComponent<RectTransform>());
        var blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.25f);
        blockerImage.raycastTarget = true;

        _root = new GameObject("Panel Root");
        _root.transform.SetParent(transform, false);
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

        MenuUiFactory.CreateTextLink(fillGo.transform, "Edit Link", "edit in decks",
            new Vector2(0f, -110f), MenuUiFactory.TextLinkSize, () => _onEditDecks?.Invoke());
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
        _onEditDecks = onEditDecks;
        _isReady = false;
        _completed = false;
        _prepRunning = false;
        _spawnSlotIndex = 0;
        RefreshSpawnSelection();

        if (_readyButton != null)
        {
            _readyButton.interactable = ProfileSession.HasCompleteLoadout;
        }

        if (_prepRoutine != null)
        {
            StopCoroutine(_prepRoutine);
        }

        gameObject.SetActive(true);
        _root.SetActive(true);
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

        _prepRunning = false;
        _completed = false;
        if (_root != null)
        {
            _root.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    IEnumerator PrepCountdown()
    {
        _prepRunning = true;
        float remaining = PrepDuration;

        while (remaining > 0f && !_completed)
        {
            if (_prepTimerText != null)
            {
                int seconds = Mathf.CeilToInt(remaining);
                _prepTimerText.text = _isReady
                    ? $"ready · starting in {seconds}"
                    : $"starting in {seconds}";
            }

            yield return null;
            remaining -= Time.unscaledDeltaTime;
        }

        if (_prepTimerText != null)
        {
            _prepTimerText.text = "starting match";
        }

        _prepRunning = false;
        CompleteSelection();
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

        if (_prepRoutine != null)
        {
            StopCoroutine(_prepRoutine);
            _prepRoutine = null;
        }

        if (_prepTimerText != null)
        {
            _prepTimerText.text = "starting match";
        }

        CompleteSelection();
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
