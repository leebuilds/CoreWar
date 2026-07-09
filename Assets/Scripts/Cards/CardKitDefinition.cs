using System.Collections.Generic;
using UnityEngine;

public enum CardHotbarTool
{
    AssaultRifle,
    SniperRifle,
    HuntingRifle,
    Pistol,
    Blueprint,
    Hammer,
    Smg,
    MachinePistol,
    LightMachineGun,
    ScopedAssaultRifle,
    CyborgLaser,
    LaserSword,
    AntiMaterialRifle
}

/// <summary>
/// Per-card loadout kit. Hotbar and held visuals resolve from this data.
/// </summary>
public class CardKitDefinition
{
    public CardHotbarTool[] hotbarTools;

    public static CardKitDefinition DefaultInfantryPlaceholder()
    {
        return Tier1Infantry();
    }

    public static CardKitDefinition Tier1Infantry()
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

    public static CardKitDefinition Tier1Sniper()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.SniperRifle,
                CardHotbarTool.Pistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition Tier2Ranger()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.ScopedAssaultRifle,
                CardHotbarTool.Pistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition Tier3Skirmisher()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.AssaultRifle,
                CardHotbarTool.MachinePistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition Tier2Hunter()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.HuntingRifle,
                CardHotbarTool.Pistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition Tier2Cyborg()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.CyborgLaser,
                CardHotbarTool.LaserSword,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition Tier3AntiMaterial()
    {
        return new CardKitDefinition
        {
            hotbarTools = new[]
            {
                CardHotbarTool.AntiMaterialRifle,
                CardHotbarTool.Pistol,
                CardHotbarTool.Blueprint,
                CardHotbarTool.Hammer
            }
        };
    }

    public static CardKitDefinition FromWeaponNames(string primaryWeapon, string secondaryWeapon)
    {
        var tools = new List<CardHotbarTool> { ResolvePrimaryTool(primaryWeapon) };

        CardHotbarTool? secondary = ResolveSecondaryTool(secondaryWeapon);
        if (secondary.HasValue)
        {
            tools.Add(secondary.Value);
        }

        tools.Add(CardHotbarTool.Blueprint);
        tools.Add(CardHotbarTool.Hammer);
        return new CardKitDefinition { hotbarTools = tools.ToArray() };
    }

    static CardHotbarTool ResolvePrimaryTool(string primaryWeapon)
    {
        if (string.IsNullOrEmpty(primaryWeapon))
        {
            return CardHotbarTool.AssaultRifle;
        }

        string weapon = primaryWeapon.ToLowerInvariant();
        if (weapon.Contains("scoped") && weapon.Contains("rifle"))
        {
            return CardHotbarTool.ScopedAssaultRifle;
        }

        if (weapon.Contains("machine pistol"))
        {
            return CardHotbarTool.MachinePistol;
        }

        if (weapon.Contains("smg"))
        {
            return CardHotbarTool.Smg;
        }

        if (weapon.Contains("laser lmg") || weapon.Contains("cyborg laser") || weapon.Contains("laser cannon"))
        {
            return CardHotbarTool.CyborgLaser;
        }

        if (weapon.Contains("laser sword") || weapon.Contains("katana"))
        {
            return CardHotbarTool.LaserSword;
        }

        if (weapon.Contains("lmg") || weapon.Contains("machine gun"))
        {
            return CardHotbarTool.LightMachineGun;
        }

        if (weapon.Contains("anti-material"))
        {
            return CardHotbarTool.AntiMaterialRifle;
        }

        if (weapon.Contains("hunting rifle"))
        {
            return CardHotbarTool.HuntingRifle;
        }

        if (weapon.Contains("sniper"))
        {
            return CardHotbarTool.SniperRifle;
        }

        return CardHotbarTool.AssaultRifle;
    }

    static CardHotbarTool? ResolveSecondaryTool(string secondaryWeapon)
    {
        if (string.IsNullOrEmpty(secondaryWeapon))
        {
            return null;
        }

        string weapon = secondaryWeapon.ToLowerInvariant();
        if (weapon == "none")
        {
            return null;
        }

        if (weapon.Contains("machine pistol"))
        {
            return CardHotbarTool.MachinePistol;
        }

        if (weapon.Contains("smg"))
        {
            return CardHotbarTool.Smg;
        }

        if (weapon.Contains("laser sword"))
        {
            return CardHotbarTool.LaserSword;
        }

        if (weapon.Contains("pistol") || weapon.Contains("sidearm"))
        {
            return CardHotbarTool.Pistol;
        }

        return null;
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
            case CardHotbarTool.ScopedAssaultRifle:
                return "Scoped AR";
            case CardHotbarTool.SniperRifle:
                return "Sniper";
            case CardHotbarTool.HuntingRifle:
                return "Hunting Rifle";
            case CardHotbarTool.Pistol:
                return "Pistol";
            case CardHotbarTool.Smg:
                return "SMG";
            case CardHotbarTool.MachinePistol:
                return "M.Pistol";
            case CardHotbarTool.LightMachineGun:
                return "LMG";
            case CardHotbarTool.CyborgLaser:
                return "Laser";
            case CardHotbarTool.AntiMaterialRifle:
                return "A-M Rifle";
            case CardHotbarTool.LaserSword:
                return "Sword";
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
        return tool == CardHotbarTool.AssaultRifle ||
            tool == CardHotbarTool.ScopedAssaultRifle ||
            tool == CardHotbarTool.SniperRifle ||
            tool == CardHotbarTool.HuntingRifle ||
            tool == CardHotbarTool.Pistol ||
            tool == CardHotbarTool.Smg ||
            tool == CardHotbarTool.MachinePistol ||
            tool == CardHotbarTool.LightMachineGun ||
            tool == CardHotbarTool.CyborgLaser ||
            tool == CardHotbarTool.AntiMaterialRifle;
    }

    public static bool UsesOverheatMeter(CardHotbarTool tool)
    {
        return tool == CardHotbarTool.CyborgLaser;
    }
}
