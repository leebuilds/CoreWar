using UnityEngine;

/// <summary>
/// Rarity palette for card tiles and banners — muted, darker military-adjacent tones.
/// </summary>
public static class CardRarityColors
{
    public static Color BannerBackground(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Uncommon:
                return new Color(0.28f, 0.56f, 0.34f);
            case CardRarity.Rare:
                return new Color(0.26f, 0.42f, 0.68f);
            case CardRarity.Epic:
                return new Color(0.40f, 0.30f, 0.60f);
            case CardRarity.Legendary:
                return new Color(0.62f, 0.50f, 0.16f);
            case CardRarity.SuperSoldier:
                return new Color(0.60f, 0.24f, 0.24f);
            default:
                return new Color(0.58f, 0.58f, 0.58f);
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
        switch (rarity)
        {
            case CardRarity.Uncommon:
                return new Color(0.34f, 0.50f, 0.38f);
            case CardRarity.Rare:
                return new Color(0.32f, 0.44f, 0.58f);
            case CardRarity.Epic:
                return new Color(0.44f, 0.36f, 0.54f);
            case CardRarity.Legendary:
                return new Color(0.56f, 0.48f, 0.22f);
            case CardRarity.SuperSoldier:
                return new Color(0.54f, 0.34f, 0.34f);
            default:
                return new Color(0.68f, 0.68f, 0.68f);
        }
    }

    public static Color Background(CardRarity rarity)
    {
        return Fill(rarity);
    }

    public static Color BannerInk(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Legendary:
            case CardRarity.SuperSoldier:
                return MenuUiFactory.MilitaryTitleInk;
            default:
                return new Color(0.92f, 0.94f, 0.88f);
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
