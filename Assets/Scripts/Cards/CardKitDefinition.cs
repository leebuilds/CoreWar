using UnityEngine;

public enum CardHotbarTool
{
    AssaultRifle,
    Pistol,
    Blueprint,
    Hammer
}

/// <summary>
/// Per-card loadout kit. Hotbar and held visuals resolve from this data.
/// </summary>
public class CardKitDefinition
{
    public CardHotbarTool[] hotbarTools;

    public static CardKitDefinition DefaultInfantryPlaceholder()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.AssaultRifle,
                CardHotbarTool.Pistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public int SlotCount => hotbarTools == null || hotbarTools.Length == 0 ? 4 : hotbarTools.Length;

    public CardHotbarTool GetToolAt(int index)
    {
        if (hotbarTools == null || hotbarTools.Length == 0)
        {
            return (CardHotbarTool)(index % 4);
        }

        return hotbarTools[Mathf.Clamp(index, 0, hotbarTools.Length - 1)];
    }

    public static string DisplayName(CardHotbarTool tool)
    {
        switch (tool)
        {
            case CardHotbarTool.AssaultRifle:
                return "AR";
            case CardHotbarTool.Pistol:
                return "Pistol";
            case CardHotbarTool.Hammer:
                return "Hammer";
            case CardHotbarTool.Blueprint:
                return "Build";
            default:
                return tool.ToString();
        }
    }

    public static string HotbarKeyLabel(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                return "1";
            case 1:
                return "2";
            case 2:
                return "F";
            case 3:
                return "H";
            default:
                return (slotIndex + 1).ToString();
        }
    }

    public static bool IsFirearm(CardHotbarTool tool)
    {
        return tool == CardHotbarTool.AssaultRifle || tool == CardHotbarTool.Pistol;
    }
}
