using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-match owned-card picker for shooting range character swaps.
/// </summary>
public class ShootingRangeCharacterPicker : MonoBehaviour
{
    Canvas _canvas;
    GameObject _overlayRoot;
    Action<string> _onCardSelected;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    public static ShootingRangeCharacterPicker Create(Transform parent, Action<string> onCardSelected)
    {
        var go = new GameObject("Shooting Range Character Picker");
        go.transform.SetParent(parent, false);
        var picker = go.AddComponent<ShootingRangeCharacterPicker>();
        picker.Build(onCardSelected);
        return picker;
    }

    void Build(Action<string> onCardSelected)
    {
        _onCardSelected = onCardSelected;
        MenuUiFactory.EnsureEventSystem();

        var canvasGo = new GameObject("Picker Canvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 210;

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

        SceneFlow.ApplyMenuInputState();
        MatchClockHud.Instance?.SetVisible(false);

        var dim = new GameObject("Dim");
        dim.transform.SetParent(_overlayRoot.transform, false);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        MenuUiFactory.StretchFull(dim.GetComponent<RectTransform>());

        var frame = MenuWindowFrame.CreateScreen(_overlayRoot.transform, "CHOOSE CHARACTER", showBack: true,
            "select an owned card · game visible behind", DecksLayout.WindowSize, showHeader: true, () => Hide());

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

    void SelectCard(string cardId)
    {
        _onCardSelected?.Invoke(cardId);
        Hide();
    }

    public void Hide()
    {
        Hide(resumeGameplay: true);
    }

    public void Hide(bool resumeGameplay)
    {
        _isOpen = false;
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
