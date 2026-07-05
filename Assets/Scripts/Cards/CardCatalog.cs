using System.Collections.Generic;

/// <summary>
/// Static catalog of all class cards. Server sync can mirror this table later.
/// </summary>
public static class CardCatalog
{
    static readonly Dictionary<string, CardDefinition> ById = new Dictionary<string, CardDefinition>();
    static readonly List<CardDefinition> All = new List<CardDefinition>();
    static bool _initialized;

    public static IReadOnlyList<CardDefinition> AllCards
    {
        get
        {
            EnsureInitialized();
            return All;
        }
    }

    public static string[] AllCardIds()
    {
        EnsureInitialized();
        var ids = new string[All.Count];
        for (int i = 0; i < All.Count; i++)
        {
            ids[i] = All[i].id;
        }

        return ids;
    }

    public static CardDefinition Get(string cardId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(cardId))
        {
            return null;
        }

        ById.TryGetValue(cardId, out var card);
        return card;
    }

    public static IEnumerable<CardDefinition> ForSpecialty(string specialtyKey)
    {
        EnsureInitialized();
        foreach (var card in All)
        {
            if (card.specialty == specialtyKey)
            {
                yield return card;
            }
        }
    }

    static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        BuildCatalog();
    }

    static void BuildCatalog()
    {
        var rows = new[]
        {
            new SpecialtyRow("infantry", "Infantry", new[] { "Infantry", "Marksman", "Scout" }),
            new SpecialtyRow("sniper", "Sniper", new[] { "Sniper", "Hunter", "Heavy Sniper" }),
            new SpecialtyRow("engineer", "Engineer", new[] { "Engineer", "Trapper", "Advanced Builder" }),
            new SpecialtyRow("support", "Support", new[] { "Medic", "Wizard", "Captain" }),
            new SpecialtyRow("assault", "Assault", new[] { "Riot Trooper", "Water Cannon Officer", "Granny with a Shotgun" }),
            new SpecialtyRow("assassin", "Assassin", new[] { "Mafia", "Secret Agent", "Koroshiya" }),
            new SpecialtyRow("heavy", "Heavy", new[] { "Heavy", "Cyborg", "Frankenstein" }),
            new SpecialtyRow("demolition", "Demolition", new[] { "Explosion Specialist", "Bazooka", "Missile Operator" }),
            new SpecialtyRow("saboteur", "Saboteur", new[] { "Saboteur", "Hacker", "Drone Pilot" }),
            new SpecialtyRow("gunner", "Gunner", new[] { "Gunner", "Lazerman", "Machine Gunner" })
        };

        var rarities = new[]
        {
            CardRarity.Common,
            CardRarity.Uncommon,
            CardRarity.Rare,
            CardRarity.Epic,
            CardRarity.Legendary,
            CardRarity.SuperSoldier
        };

        int rarityIndex = 0;
        foreach (var row in rows)
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                var rarity = rarities[rarityIndex % rarities.Length];
                rarityIndex++;
                Register(CreateCard(row.key, row.label, tier, row.names[tier - 1], rarity));
            }
        }
    }

    static CardDefinition CreateCard(string specialtyKey, string specialtyLabel, int tier, string displayName, CardRarity rarity)
    {
        float tierScale = 1f + (tier - 1) * 0.05f;
        return new CardDefinition
        {
            id = $"{specialtyKey}_{tier}",
            specialty = specialtyKey,
            specialtyLabel = specialtyLabel,
            tier = tier,
            displayName = displayName,
            rarity = rarity,
            kit = CardKitDefinition.DefaultInfantryPlaceholder(),
            preview = new CardPreviewStats
            {
                description = $"{displayName} is a Tier {tier} {specialtyLabel} operator. Placeholder kit uses gun, hammer, and blueprint until class-specific weapons ship.",
                moveSpeed = 8f * tierScale,
                health = 100 + (tier - 1) * 15,
                trapLimit = 5,
                primaryWeapon = tier == 1 ? "Standard Rifle" : tier == 2 ? "Enhanced Rifle" : "Specialist Rifle",
                secondaryWeapon = "Sidearm (placeholder)",
                passiveAbility = $"{specialtyLabel} Tier {tier} passive — future unique modifier.",
                sabotageNote = tier >= 2 ? "Improved sabotage efficiency (placeholder)." : "Standard sabotage tool.",
                buildModifier = specialtyKey == "engineer"
                    ? "Build speed bonus (placeholder)."
                    : "Standard build costs.",
                hotbarSummary = "Gun · Hammer · Blueprint"
            }
        };
    }

    static void Register(CardDefinition card)
    {
        All.Add(card);
        ById[card.id] = card;
    }

    struct SpecialtyRow
    {
        public string key;
        public string label;
        public string[] names;

        public SpecialtyRow(string key, string label, string[] names)
        {
            this.key = key;
            this.label = label;
            this.names = names;
        }
    }
}
