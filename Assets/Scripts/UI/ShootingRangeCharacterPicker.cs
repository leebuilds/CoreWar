using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match owned-card picker for shooting range character swaps.
/// </summary>
public class ShootingRangeCharacterPicker : MonoBehaviour
{
    GameObject _overlayRoot;
    Action<string> _onCardSelected;
    Action _onBack;
    Action _onBeforeSelect;
    bool _isOpen;
    RectTransform _layer;

    public bool IsOpen => _isOpen;

    public static ShootingRangeCharacterPicker Create(Transform parent, Action<string> onCardSelected)
    {
        GameUICanvas.EnsureExists();
        var layer = GameUICanvas.CreateInteractionLayer("Character Picker", 190);
        var hostRect = GameUICanvas.CreateScreenHost(layer, "Shooting Range Character Picker");
        var picker = hostRect.gameObject.AddComponent<ShootingRangeCharacterPicker>();
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

        MenuUiFactory.CreateFullscreenDim(_overlayRoot.transform, 0.45f);

        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "CHOOSE CHARACTER", showBack: true,
            "select an owned card · game visible behind", DecksLayout.WindowSize, showHeader: true, GoBack,
            animateFade: false);

        var bodyRect = frame.Body;
        var bodyLayout = bodyRect.gameObject.AddComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1f;
        bodyLayout.flexibleWidth = 1f;

        DecksCollectionView.BuildOwnedCollection(bodyRect, card =>
        {
            if (card == null)
            {
                return;
            }

            SelectCard(card.id);
        });
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
