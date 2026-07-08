using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match overlay for choosing one of two loadout cards on respawn.
/// </summary>
public class RespawnClassPicker : MonoBehaviour
{
    GameObject _overlayRoot;
    Action<string> _onCardSelected;
    Action _onBack;
    Action _onBeforeSelect;
    bool _isOpen;
    RectTransform _layer;

    public bool IsOpen => _isOpen;

    public static RespawnClassPicker Create(Transform parent, Action<string> onCardSelected)
    {
        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Respawn Picker", 190);
        var hostRect = GameUICanvas.CreateScreenHost(layer, "Respawn Class Picker");
        var picker = hostRect.gameObject.AddComponent<RespawnClassPicker>();
        picker._layer = layer;
        picker.Build(onCardSelected);
        return picker;
    }

    void Build(Action<string> onCardSelected)
    {
        _onCardSelected = onCardSelected;

        _overlayRoot = new GameObject("Overlay Root");
        _overlayRoot.transform.SetParent(transform, false);
        MenuUiFactory.StretchFull(_overlayRoot.AddComponent<RectTransform>());
        _overlayRoot.SetActive(false);
    }

    public void Show(Action onBack = null, Action onBeforeSelect = null)
    {
        if (_isOpen)
        {
            return;
        }

        _onBack = onBack;
        _onBeforeSelect = onBeforeSelect;

        ClearOverlayChildren();
        _isOpen = true;
        _overlayRoot.SetActive(true);

        MenuUiFactory.EnsureEventSystem();
        GameUICanvas.BringLayerToFront(_layer);
        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);

        MenuUiFactory.CreateFullscreenDim(_overlayRoot.transform, 0.55f);

        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "CHOOSE CLASS", showBack: true,
            "pick a loadout card to respawn", new Vector2(980f, 560f), showHeader: false, GoBack,
            animateFade: false);

        var cardA = CardCatalog.Get(GameSession.LoadoutCardIdA);
        var cardB = CardCatalog.Get(GameSession.LoadoutCardIdB);

        CreatePickerCard(frame.Body, cardA, new Vector2(-220f, 0f), GameSession.LoadoutCardIdA);
        CreatePickerCard(frame.Body, cardB, new Vector2(220f, 0f), GameSession.LoadoutCardIdB);
    }

    public bool TryGoBack()
    {
        if (_onBack == null)
        {
            return false;
        }

        GoBack();
        return true;
    }

    void GoBack()
    {
        var callback = _onBack;
        _onBack = null;
        _onBeforeSelect = null;
        Hide(resumeGameplay: false);
        callback?.Invoke();
    }

    void CreatePickerCard(Transform parent, CardDefinition card, Vector2 position, string cardId)
    {
        if (card == null)
        {
            MenuUiFactory.CreateText(parent, "Missing", "EMPTY", 24, FontStyle.Bold,
                TextAnchor.MiddleCenter, position, new Vector2(280f, 180f), MenuUiFactory.Background);
            return;
        }

        var tile = CardTileView.Create(parent, card, owned: true, () => SelectCard(cardId));
        var rect = tile.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }

    void SelectCard(string cardId)
    {
        var beforeSelect = _onBeforeSelect;
        _onBack = null;
        _onBeforeSelect = null;
        beforeSelect?.Invoke();
        _onCardSelected?.Invoke(cardId);
        Hide(resumeGameplay: true);
    }

    public void Hide()
    {
        Hide(resumeGameplay: true);
    }

    public void Hide(bool resumeGameplay)
    {
        _isOpen = false;
        _onBack = null;
        _onBeforeSelect = null;
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        ClearOverlayChildren();

        if (resumeGameplay && GameSession.IsMatchActive)
        {
            SceneFlow.ApplyGameInputState();
            MatchClockHud.Instance?.SetVisible(true);
        }
        else if (!resumeGameplay)
        {
            MatchClockHud.Instance?.SetVisible(false);
        }
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
