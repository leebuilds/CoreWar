using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Full-screen card preview with placeholder stat sheet.
/// </summary>
public class CardPreviewPanel : MonoBehaviour
{
    GameObject _root;
    GameObject _modalOverlay;

    public bool IsOpen => _root != null && _root.activeSelf;

    public static CardPreviewPanel Create(Transform parent)
    {
        var go = new GameObject("Card Preview Panel");
        go.transform.SetParent(parent, false);
        MenuUiFactory.StretchFull(go.AddComponent<RectTransform>());

        var panel = go.AddComponent<CardPreviewPanel>();
        panel._root = go;
        go.SetActive(false);
        return panel;
    }

    public void Show(CardDefinition card, UnityAction onBack, UnityAction<int> onSelectSlot)
    {
        ClearChildren();
        _root.SetActive(true);

        var frame = MenuWindowFrame.CreateModal(_root.transform, card.displayName, showBack: true,
            $"{CardRarityColors.Label(card.rarity)} · TIER {card.tier}",
            new Vector2(920f, 720f), onBack);
        _modalOverlay = frame.transform.parent.gameObject;

        string body = BuildBody(card);
        var bodyText = MenuUiFactory.CreateAnchoredText(frame.Body, "Body", body,
            MenuUiFactory.BodyFontSize, FontStyle.Normal, TextAnchor.UpperLeft, MenuUiFactory.Ink);
        var bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.04f, 0.14f);
        bodyRect.anchorMax = new Vector2(0.96f, 0.96f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        var buttonRow = new GameObject("Button Row");
        buttonRow.transform.SetParent(frame.Body, false);
        var rowRect = buttonRow.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.04f, 0.02f);
        rowRect.anchorMax = new Vector2(0.96f, 0.12f);
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        MenuUiFactory.CreateButton(buttonRow.transform, "Select Slot 1", "SELECT SLOT 1",
            new Vector2(-230f, 0f), new Vector2(220f, 52f), () => onSelectSlot?.Invoke(0));
        MenuUiFactory.CreateButton(buttonRow.transform, "Select Slot 2", "SELECT SLOT 2",
            new Vector2(230f, 0f), new Vector2(220f, 52f), () => onSelectSlot?.Invoke(1));

        _root.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        _root.SetActive(false);
        ClearChildren();
    }

    void ClearChildren()
    {
        if (_modalOverlay != null)
        {
            Destroy(_modalOverlay);
            _modalOverlay = null;
        }

        for (int i = _root.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(_root.transform.GetChild(i).gameObject);
        }
    }

    static string BuildBody(CardDefinition card)
    {
        var preview = card.preview;
        if (preview == null)
        {
            return "No preview data.";
        }

        return
            $"{card.specialtyLabel.ToUpperInvariant()}\n\n" +
            $"{preview.description}\n\n" +
            $"Move speed: {preview.moveSpeed:0.#}\n" +
            $"Health: {preview.health}\n" +
            $"Trap limit: {preview.trapLimit}\n" +
            $"Primary: {preview.primaryWeapon}\n" +
            $"Secondary: {preview.secondaryWeapon}\n" +
            $"Hotbar: {preview.hotbarSummary}\n" +
            $"Passive: {preview.passiveAbility}\n" +
            $"Sabotage: {preview.sabotageNote}\n" +
            $"Build: {preview.buildModifier}";
    }
}
