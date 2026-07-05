using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// In-match overlay for choosing one of two loadout cards on respawn.
/// </summary>
public class RespawnClassPicker : MonoBehaviour
{
    Canvas _canvas;
    GameObject _overlayRoot;
    Action<string> _onCardSelected;
    bool _isOpen;

    public bool IsOpen => _isOpen;

    public static RespawnClassPicker Create(Transform parent, Action<string> onCardSelected)
    {
        var go = new GameObject("Respawn Class Picker");
        go.transform.SetParent(parent, false);
        var picker = go.AddComponent<RespawnClassPicker>();
        picker.Build(onCardSelected);
        return picker;
    }

    void Build(Action<string> onCardSelected)
    {
        _onCardSelected = onCardSelected;

        var canvasGo = new GameObject("Picker Canvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var dim = new GameObject("Dim");
        dim.transform.SetParent(_overlayRoot.transform, false);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        MenuUiFactory.StretchFull(dim.GetComponent<RectTransform>());

        MenuUiFactory.CreateText(_overlayRoot.transform, "Title", "CHOOSE CLASS",
            52, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 280f), new Vector2(900f, 80f), MenuUiFactory.Background);

        var cardA = CardCatalog.Get(GameSession.LoadoutCardIdA);
        var cardB = CardCatalog.Get(GameSession.LoadoutCardIdB);

        CreatePickerCard(_overlayRoot.transform, cardA, new Vector2(-420f, 0f), GameSession.LoadoutCardIdA);
        CreatePickerCard(_overlayRoot.transform, cardB, new Vector2(420f, 0f), GameSession.LoadoutCardIdB);
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
        _onCardSelected?.Invoke(cardId);
        Hide();
    }

    public void Hide()
    {
        _isOpen = false;
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }

        ClearOverlayChildren();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
