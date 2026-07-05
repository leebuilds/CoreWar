using UnityEngine;

/// <summary>
/// Rarity palette for card tiles and banners.
/// </summary>
public static class CardRarityColors
{
    public static Color BannerBackground(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:
                return new Color(0.52f, 0.88f, 0.58f);
            case CardRarity.Rare:
                return new Color(0.48f, 0.68f, 0.98f);
            case CardRarity.Epic:
                return new Color(0.72f, 0.52f, 0.96f);
            case CardRarity.Legendary:
                return new Color(0.98f, 0.82f, 0.28f);
            case CardRarity.SuperSoldier:
                return new Color(0.96f, 0.42f, 0.42f);
            default:
                return new Color(0.78f, 0.78f, 0.78f);
        }
    }

    public static int BannerFontSize(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return 12;
        }

        if (label.Length <= 6)
        {
            return 18;
        }

        if (label.Length <= 8)
        {
            return 15;
        }

        if (label.Length <= 11)
        {
            return 13;
        }

        return 11;
    }

    public static Color Fill(CardRarity rarity)
    {
        return BannerBackground(rarity);
    }

    public static Color Background(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:
                return new Color(0.82f, 0.9f, 0.84f);
            case CardRarity.Rare:
                return new Color(0.82f, 0.88f, 0.96f);
            case CardRarity.Epic:
                return new Color(0.88f, 0.82f, 0.96f);
            case CardRarity.Legendary:
                return new Color(0.96f, 0.9f, 0.72f);
            case CardRarity.SuperSoldier:
                return new Color(0.96f, 0.82f, 0.82f);
            default:
                return new Color(0.9f, 0.9f, 0.9f);
        }
    }

    public static Color BannerInk(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Legendary:
            case CardRarity.SuperSoldier:
                return MenuUiFactory.Ink;
            default:
                return Color.white;
        }
    }

    public static string Label(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:
                return "UNCOMMON";
            case CardRarity.Rare:
                return "RARE";
            case CardRarity.Epic:
                return "EPIC";
            case CardRarity.Legendary:
                return "LEGENDARY";
            case CardRarity.SuperSoldier:
                return "SUPER SOLDIER";
            default:
                return "COMMON";
        }
    }
}
