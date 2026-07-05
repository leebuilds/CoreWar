using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Renders a single collection card tile with rarity styling and optional lock overlay.
/// </summary>
public class CardTileView : MonoBehaviour
{
    const float BorderThickness = MenuUiFactory.WindowBorderWidth;

    Image _fillImage;
    Color _baseFill;
    GameObject _loadoutOutline;
    GameObject _spawnSelectionFrame;
    GameObject _spawnSelectedLabel;
    GameObject _dimOverlay;
    GameObject _lockOverlay;
    Button _button;
    CardDefinition _card;
    bool _locked;

    public CardDefinition Card => _card;
    public bool Locked => _locked;

    public static CardTileView Create(Transform parent, CardDefinition card, bool owned, UnityAction onClick)
    {
        var go = new GameObject($"Card_{card.id}");
        go.transform.SetParent(parent, false);

        var view = go.AddComponent<CardTileView>();
        view.Build(card, owned, onClick);
        MenuUiFactory.ApplyCardHover(view);
        return view;
    }

    void Build(CardDefinition card, bool owned, UnityAction onClick)
    {
        _card = card;
        _locked = !owned;
        _baseFill = CardRarityColors.Fill(card.rarity);

        var rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 180f);

        _loadoutOutline = CreateLayer("Loadout Outline", MenuUiFactory.LoadoutOutline, outerExpand: 5f);
        _loadoutOutline.SetActive(false);

        _dimOverlay = CreateLayer("Dim Overlay", new Color(0.12f, 0.12f, 0.12f, 0.45f), innerInset: BorderThickness);
        _dimOverlay.SetActive(false);

        CreateLayer("Border", MenuUiFactory.Ink, outerExpand: 0f);

        var fillGo = CreateLayer("Fill", _baseFill, innerInset: BorderThickness);
        _fillImage = fillGo.GetComponent<Image>();
        _fillImage.raycastTarget = true;

        _button = gameObject.AddComponent<Button>();
        _button.targetGraphic = _fillImage;
        _button.interactable = owned && onClick != null;
        if (onClick != null)
        {
            _button.onClick.AddListener(onClick);
            MenuUiSounds.WireButton(_button);
        }

        var titleText = MenuUiFactory.CreateAnchoredText(transform, "Title", card.displayName.ToUpperInvariant(),
            26, FontStyle.Bold, TextAnchor.UpperCenter, MenuUiFactory.Ink);
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.08f, 0.56f);
        titleRect.anchorMax = new Vector2(0.92f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        _spawnSelectedLabel = MenuUiFactory.CreateAnchoredText(transform, "Selected Label", "selected",
            MenuUiFactory.SmallFontSize, FontStyle.Bold, TextAnchor.MiddleCenter, MenuUiFactory.Ink).gameObject;
        var selectedRect = _spawnSelectedLabel.GetComponent<RectTransform>();
        selectedRect.anchorMin = new Vector2(0.10f, 0.40f);
        selectedRect.anchorMax = new Vector2(0.90f, 0.56f);
        selectedRect.offsetMin = Vector2.zero;
        selectedRect.offsetMax = Vector2.zero;
        _spawnSelectedLabel.SetActive(false);

        var tierText = MenuUiFactory.CreateAnchoredText(transform, "Meta",
            $"{CardRarityColors.Label(card.rarity)}\n{card.specialtyLabel.ToUpperInvariant()} · TIER {card.tier}",
            MenuUiFactory.SmallFontSize, FontStyle.Bold, TextAnchor.LowerCenter, MenuUiFactory.Ink);
        var tierRect = tierText.GetComponent<RectTransform>();
        tierRect.anchorMin = new Vector2(0.08f, 0.08f);
        tierRect.anchorMax = new Vector2(0.92f, 0.40f);
        tierRect.offsetMin = Vector2.zero;
        tierRect.offsetMax = Vector2.zero;

        if (_locked)
        {
            _lockOverlay = new GameObject("Lock Overlay");
            _lockOverlay.transform.SetParent(transform, false);
            var lockRect = _lockOverlay.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = new Vector2(BorderThickness, BorderThickness);
            lockRect.offsetMax = new Vector2(-BorderThickness, -BorderThickness);

            var lockBg = _lockOverlay.AddComponent<Image>();
            lockBg.color = new Color(0.15f, 0.15f, 0.15f, 0.55f);
            lockBg.raycastTarget = false;

            MenuUiFactory.CreateAnchoredText(_lockOverlay.transform, "Lock", "LOCK", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        }

        _spawnSelectionFrame = MenuUiFactory.CreateCornerBracketFrame(transform, MenuUiFactory.Ink);
        _spawnSelectionFrame.SetActive(false);

        gameObject.AddComponent<ScrollWheelForwarder>();
    }

    GameObject CreateLayer(string name, Color color, float outerExpand = 0f, float innerInset = 0f)
    {
        var layerGo = new GameObject(name);
        layerGo.transform.SetParent(transform, false);

        var layerRect = layerGo.AddComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;

        if (outerExpand > 0f)
        {
            layerRect.offsetMin = new Vector2(-outerExpand, -outerExpand);
            layerRect.offsetMax = new Vector2(outerExpand, outerExpand);
        }
        else if (innerInset > 0f)
        {
            layerRect.offsetMin = new Vector2(innerInset, innerInset);
            layerRect.offsetMax = new Vector2(-innerInset, -innerInset);
        }
        else
        {
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;
        }

        var layerImage = layerGo.AddComponent<Image>();
        layerImage.color = color;
        layerImage.raycastTarget = false;
        return layerGo;
    }

    public void SetInLoadout(bool inLoadout)
    {
        if (_loadoutOutline != null)
        {
            _loadoutOutline.SetActive(inLoadout);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_fillImage == null)
        {
            return;
        }

        _fillImage.color = highlighted
            ? Color.Lerp(_baseFill, MenuUiFactory.Ink, 0.1f)
            : _baseFill;
    }

    public void SetSpawnSelected(bool selected)
    {
        if (_spawnSelectionFrame != null)
        {
            _spawnSelectionFrame.SetActive(selected);
        }

        if (_spawnSelectedLabel != null)
        {
            _spawnSelectedLabel.SetActive(selected);
        }
    }

    public void SetSpawnDimmed(bool dimmed)
    {
        if (_dimOverlay != null)
        {
            _dimOverlay.SetActive(dimmed);
        }
    }
}
