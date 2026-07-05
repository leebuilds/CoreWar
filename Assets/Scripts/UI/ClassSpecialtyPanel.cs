using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Left column in the decks collection: class name, future symbol slot, and role blurb.
/// </summary>
public static class ClassSpecialtyPanel
{
    public static RectTransform Create(Transform parent, string specialtyKey, string specialtyLabel)
    {
        var panelGo = new GameObject($"Specialty_{specialtyKey}");
        panelGo.transform.SetParent(parent, false);
        panelGo.AddComponent<RectTransform>();

        var layout = panelGo.AddComponent<LayoutElement>();
        layout.preferredWidth = DecksLayout.SpecialtyWidth;
        layout.preferredHeight = DecksLayout.SpecialtySize.y;
        layout.minWidth = DecksLayout.SpecialtyWidth;
        layout.flexibleWidth = 0f;

        CreateLayer(panelGo.transform, "Border", MenuUiFactory.Ink, 0f);

        var fillGo = CreateLayer(panelGo.transform, "Fill", MenuUiFactory.PanelFill, 0f,
            MenuUiFactory.WindowBorderWidth);

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(fillGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(contentRect);
        contentRect.offsetMin = new Vector2(12f, 10f);
        contentRect.offsetMax = new Vector2(-12f, -10f);

        var column = contentGo.AddComponent<VerticalLayoutGroup>();
        column.spacing = 5f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        CreateTitleBlock(contentGo.transform, specialtyLabel);
        CreateSymbolPlaceholder(contentGo.transform);
        CreateRoleBlock(contentGo.transform, specialtyKey);

        return panelGo.GetComponent<RectTransform>();
    }

    static void CreateTitleBlock(Transform parent, string specialtyLabel)
    {
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);
        titleGo.AddComponent<LayoutElement>().preferredHeight = 28f;

        var title = MenuUiFactory.CreateAnchoredText(titleGo.transform, "Label", specialtyLabel.ToUpperInvariant(),
            MenuUiFactory.BodyFontSize, FontStyle.Bold, TextAnchor.UpperLeft, MenuUiFactory.Ink);
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 16;
        title.resizeTextMaxSize = MenuUiFactory.BodyFontSize;
    }

    static void CreateSymbolPlaceholder(Transform parent)
    {
        var symbolRow = new GameObject("Symbol Row");
        symbolRow.transform.SetParent(parent, false);
        var rowLayout = symbolRow.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 36f;

        var symbolGo = new GameObject("Symbol Placeholder");
        symbolGo.transform.SetParent(symbolRow.transform, false);
        var symbolRect = symbolGo.AddComponent<RectTransform>();
        symbolRect.anchorMin = new Vector2(0f, 0.5f);
        symbolRect.anchorMax = new Vector2(0f, 0.5f);
        symbolRect.pivot = new Vector2(0f, 0.5f);
        symbolRect.sizeDelta = new Vector2(34f, 34f);
        symbolRect.anchoredPosition = Vector2.zero;

        var border = symbolGo.AddComponent<Image>();
        border.color = MenuUiFactory.Ink;

        var innerGo = new GameObject("Inner");
        innerGo.transform.SetParent(symbolGo.transform, false);
        var innerRect = innerGo.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);
        innerGo.AddComponent<Image>().color = MenuUiFactory.ScrollViewportFill;
    }

    static void CreateRoleBlock(Transform parent, string specialtyKey)
    {
        var roleGo = new GameObject("Role");
        roleGo.transform.SetParent(parent, false);
        var roleLayout = roleGo.AddComponent<LayoutElement>();
        roleLayout.preferredHeight = 102f;
        roleLayout.flexibleHeight = 1f;

        string role = ClassSpecialtyDescriptions.GetRole(specialtyKey);
        var roleText = MenuUiFactory.CreateAnchoredText(roleGo.transform, "Text", role,
            MenuUiFactory.SectionFontSize, FontStyle.Normal, TextAnchor.UpperLeft, MenuUiFactory.MutedInk);
        roleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        roleText.verticalOverflow = VerticalWrapMode.Truncate;
        roleText.lineSpacing = 1f;
        roleText.resizeTextForBestFit = true;
        roleText.resizeTextMinSize = 12;
        roleText.resizeTextMaxSize = MenuUiFactory.SectionFontSize;
    }

    static GameObject CreateLayer(Transform parent, string name, Color color, float outerExpand, float innerInset = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        MenuUiFactory.StretchFull(rect);

        if (outerExpand > 0f)
        {
            rect.offsetMin = new Vector2(-outerExpand, -outerExpand);
            rect.offsetMax = new Vector2(outerExpand, outerExpand);
        }

        if (innerInset > 0f)
        {
            rect.offsetMin = new Vector2(innerInset, innerInset);
            rect.offsetMax = new Vector2(-innerInset, -innerInset);
        }

        go.AddComponent<Image>().color = color;
        return go;
    }
}
