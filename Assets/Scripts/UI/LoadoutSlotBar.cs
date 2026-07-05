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
        MenuUiFactory.CreateAnchoredText(transform, "Label", "LOADOUT",
            16, FontStyle.Bold, TextAnchor.UpperCenter, MenuUiFactory.MutedInk);
        var labelRect = transform.Find("Label").GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.04f, 0.62f);
        labelRect.anchorMax = new Vector2(0.96f, 0.98f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        for (int i = 0; i < 2; i++)
        {
            var slotGo = new GameObject($"Slot {i + 1}");
            slotGo.transform.SetParent(transform, false);

            var rect = slotGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(i == 0 ? 0.04f : 0.52f, 0.08f);
            rect.anchorMax = new Vector2(i == 0 ? 0.48f : 0.96f, 0.58f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var border = slotGo.AddComponent<Image>();
            border.color = MenuUiFactory.Ink;

            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(slotGo.transform, false);
            var innerRect = innerGo.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2f, 2f);
            innerRect.offsetMax = new Vector2(-2f, -2f);
            _slotImages[i] = innerGo.AddComponent<Image>();
            _slotImages[i].color = Color.white;

            _slotButtons[i] = slotGo.AddComponent<Button>();
            _slotButtons[i].targetGraphic = border;
            int slotIndex = i;
            _slotButtons[i].onClick.AddListener(() => onSlotClicked?.Invoke(slotIndex));

            _slotLabels[i] = MenuUiFactory.CreateAnchoredText(innerGo.transform, "Label", $"SLOT {i + 1}\nEMPTY",
                15, FontStyle.Bold, TextAnchor.MiddleCenter);
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
                _slotImages[i].color = Color.white;
                _slotLabels[i].text = $"SLOT {i + 1}\nEMPTY";
                continue;
            }

            _slotImages[i].color = Color.white;
            _slotLabels[i].text = $"{card.displayName.ToUpperInvariant()}\nT{card.tier} {card.specialtyLabel.ToUpperInvariant()}";
        }
    }
}
