using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Two-slot loadout strip inside the Decks window header.
/// </summary>
public class LoadoutSlotBar : MonoBehaviour
{
    readonly Image[] _slotImages = new Image[2];
    readonly Text[] _slotLabels = new Text[2];
    readonly Button[] _slotButtons = new Button[2];

    public static LoadoutSlotBar Create(Transform headerParent, UnityAction<int> onSlotClicked)
    {
        var go = new GameObject("Loadout Slot Bar");
        go.transform.SetParent(headerParent, false);

        var rect = go.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(rect);

        var bar = go.AddComponent<LoadoutSlotBar>();
        bar.Build(onSlotClicked);
        bar.Refresh();
        return bar;
    }

    void Build(UnityAction<int> onSlotClicked)
    {
        float pad = MenuUiFactory.ContentPadding;
        float gap = MenuUiFactory.ContentPadding * 0.5f;

        MenuUiFactory.CreateAnchoredText(transform, "Label", "LOADOUT",
            MenuUiFactory.SmallFontSize, FontStyle.Bold, TextAnchor.UpperCenter, MenuUiFactory.MutedInk);
        var labelRect = transform.Find("Label").GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.62f);
        labelRect.anchorMax = new Vector2(1f, 0.98f);
        labelRect.offsetMin = new Vector2(pad, 0f);
        labelRect.offsetMax = new Vector2(-pad, 0f);

        for (int i = 0; i < 2; i++)
        {
            var slotGo = new GameObject($"Slot {i + 1}");
            slotGo.transform.SetParent(transform, false);

            var rect = slotGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(i == 0 ? 0f : 0.5f, 0.08f);
            rect.anchorMax = new Vector2(i == 0 ? 0.5f : 1f, 0.58f);
            rect.offsetMin = new Vector2(i == 0 ? pad : gap * 0.5f, 0f);
            rect.offsetMax = new Vector2(i == 0 ? -gap * 0.5f : -pad, 0f);

            var border = slotGo.AddComponent<Image>();
            border.color = MenuUiFactory.Ink;

            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(slotGo.transform, false);
            var innerRect = innerGo.AddComponent<RectTransform>();
            MenuUiFactory.ApplyInnerBorder(innerRect);
            _slotImages[i] = innerGo.AddComponent<Image>();
            _slotImages[i].color = MenuUiFactory.PanelFill;

            _slotButtons[i] = slotGo.AddComponent<Button>();
            _slotButtons[i].targetGraphic = border;
            int slotIndex = i;
            _slotButtons[i].onClick.AddListener(() => onSlotClicked?.Invoke(slotIndex));

            _slotLabels[i] = MenuUiFactory.CreateAnchoredText(innerGo.transform, "Label", $"SLOT {i + 1}\nEMPTY",
                MenuUiFactory.SmallFontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
    }

    public void Refresh()
    {
        var profile = ProfileSession.ActiveProfile;
        for (int i = 0; i < 2; i++)
        {
            string cardId = profile?.loadoutCardIds != null && profile.loadoutCardIds.Length > i
                ? profile.loadoutCardIds[i]
                : string.Empty;

            var card = CardCatalog.Get(cardId);
            if (card == null)
            {
                _slotImages[i].color = MenuUiFactory.PanelFill;
                _slotLabels[i].text = $"SLOT {i + 1}\nEMPTY";
                continue;
            }

            _slotImages[i].color = MenuUiFactory.PanelFill;
            _slotLabels[i].text = $"{card.displayName.ToUpperInvariant()}\nT{card.tier} {card.specialtyLabel.ToUpperInvariant()}";
        }
    }
}
