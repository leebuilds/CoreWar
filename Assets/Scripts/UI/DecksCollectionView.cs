using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared decks collection scroll area for menu and in-match overlays.
/// </summary>
public static class DecksCollectionView
{
    public static ScrollRect BuildOwnedCollection(Transform parent, Action<CardDefinition> onCardSelected)
    {
        var viewportGo = new GameObject("Collection Viewport");
        viewportGo.transform.SetParent(parent, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(viewportRect);

        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = MenuUiFactory.ScrollViewportFill;
        viewportGo.AddComponent<Mask>().showMaskGraphic = true;

        var contentGo = new GameObject("Collection Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = DecksLayout.RowSpacing;
        layout.padding = new RectOffset((int)DecksLayout.HorizontalPadding, (int)DecksLayout.HorizontalPadding, 4, 4);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateSectionHeader(contentGo.transform, "OWNED CARDS");

        var specialtyKeys = new[]
        {
            "infantry", "sniper", "engineer", "support", "assault",
            "assassin", "heavy", "demolition", "saboteur", "gunner"
        };

        foreach (var specialtyKey in specialtyKeys)
        {
            CreateOwnedSpecialtyRow(contentGo.transform, specialtyKey, onCardSelected);
        }

        var scrollRect = viewportGo.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;
        scrollRect.verticalNormalizedPosition = 1f;
        return scrollRect;
    }

    static void CreateSectionHeader(Transform parent, string title)
    {
        var headerGo = new GameObject("Section Header");
        headerGo.transform.SetParent(parent, false);
        var layout = headerGo.AddComponent<LayoutElement>();
        layout.preferredHeight = 36f;
        layout.minHeight = 36f;
        layout.preferredWidth = DecksLayout.ContentRowWidth;
        layout.minWidth = DecksLayout.ContentRowWidth;

        MenuUiFactory.CreateAnchoredText(headerGo.transform, "Title", title,
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, MenuUiFactory.Ink);
    }

    static void CreateOwnedSpecialtyRow(Transform parent, string specialtyKey, Action<CardDefinition> onCardSelected)
    {
        CardDefinition tier1 = null;
        CardDefinition tier2 = null;
        CardDefinition tier3 = null;
        string specialtyLabel = specialtyKey;

        foreach (var card in CardCatalog.ForSpecialty(specialtyKey))
        {
            specialtyLabel = card.specialtyLabel;
            switch (card.tier)
            {
                case 1: tier1 = card; break;
                case 2: tier2 = card; break;
                case 3: tier3 = card; break;
            }
        }

        bool hasOwned = (tier1 != null && ProfileSession.OwnsCard(tier1.id)) ||
                        (tier2 != null && ProfileSession.OwnsCard(tier2.id)) ||
                        (tier3 != null && ProfileSession.OwnsCard(tier3.id));
        if (!hasOwned)
        {
            return;
        }

        var rowGo = new GameObject($"Row_{specialtyKey}");
        rowGo.transform.SetParent(parent, false);

        var rowLayoutElement = rowGo.AddComponent<LayoutElement>();
        rowLayoutElement.preferredWidth = DecksLayout.ContentRowWidth;
        rowLayoutElement.minWidth = DecksLayout.ContentRowWidth;
        rowLayoutElement.preferredHeight = DecksLayout.RowHeight;
        rowLayoutElement.minHeight = DecksLayout.RowHeight;

        var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = DecksLayout.ColumnSpacing;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        ClassSpecialtyPanel.Create(rowGo.transform, specialtyKey, specialtyLabel);
        CreateOwnedCardCell(rowGo.transform, tier1, onCardSelected);
        CreateOwnedCardCell(rowGo.transform, tier2, onCardSelected);
        CreateOwnedCardCell(rowGo.transform, tier3, onCardSelected);
    }

    static void CreateOwnedCardCell(Transform parent, CardDefinition card, Action<CardDefinition> onCardSelected)
    {
        if (card == null || !ProfileSession.OwnsCard(card.id))
        {
            return;
        }

        var cellGo = new GameObject($"Card Cell {card.id}");
        cellGo.transform.SetParent(parent, false);
        var cellLayout = cellGo.AddComponent<LayoutElement>();
        cellLayout.preferredWidth = DecksLayout.CardSize.x;
        cellLayout.preferredHeight = DecksLayout.CardSize.y;
        cellLayout.minWidth = DecksLayout.CardSize.x;
        cellLayout.minHeight = DecksLayout.CardSize.y;

        cellGo.AddComponent<RectMask2D>();
        CardTileView.Create(cellGo.transform, card, owned: true, () => onCardSelected?.Invoke(card), DecksLayout.CardSize);
    }
}
