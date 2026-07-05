using UnityEngine;

public enum CardHotbarTool
{
    Gun,
    Hammer,
    Blueprint
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
                CardHotbarTool.Gun,
                CardHotbarTool.Hammer,
                CardHotbarTool.Blueprint
            }
        };
    }

    public int SlotCount => hotbarTools == null || hotbarTools.Length == 0 ? 3 : hotbarTools.Length;

    public CardHotbarTool GetToolAt(int index)
    {
        if (hotbarTools == null || hotbarTools.Length == 0)
        {
            return (CardHotbarTool)(index % 3);
        }

        return hotbarTools[Mathf.Clamp(index, 0, hotbarTools.Length - 1)];
    }

    public static string DisplayName(CardHotbarTool tool)
    {
        switch (tool)
        {
            case CardHotbarTool.Gun:
                return "Gun";
            case CardHotbarTool.Hammer:
                return "Hammer";
            case CardHotbarTool.Blueprint:
                return "Blueprint";
            default:
                return tool.ToString();
        }
    }
}
