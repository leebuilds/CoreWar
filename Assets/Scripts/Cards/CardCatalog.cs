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

    /// <summary>
    /// Cards unlocked for a brand-new account.
    /// </summary>
    public static string[] DefaultOwnedCardIds()
    {
        return new[] { "infantry_1", "sniper_1", "sniper_2", "sniper_3", "infantry_2", "infantry_3", "heavy_1", "heavy_2", "demolition_1" };
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
        foreach (var entry in AllEntries)
        {
            Register(CreateCard(entry));
        }
    }

    static CardKitDefinition ResolveKit(CardEntry entry)
    {
        if (entry.SpecialtyKey == "infantry" && entry.Tier == 1)
        {
            return CardKitDefinition.Tier1Infantry();
        }

        if (entry.SpecialtyKey == "infantry" && entry.Tier == 2)
        {
            return CardKitDefinition.Tier2Ranger();
        }

        if (entry.SpecialtyKey == "infantry" && entry.Tier == 3)
        {
            return CardKitDefinition.Tier3Skirmisher();
        }

        if (entry.SpecialtyKey == "sniper" && entry.Tier == 1)
        {
            return CardKitDefinition.Tier1Sniper();
        }

        if (entry.SpecialtyKey == "sniper" && entry.Tier == 2)
        {
            return CardKitDefinition.Tier2Hunter();
        }

        if (entry.SpecialtyKey == "heavy" && entry.Tier == 2)
        {
            return CardKitDefinition.Tier2Cyborg();
        }

        if (entry.SpecialtyKey == "sniper" && entry.Tier == 3)
        {
            return CardKitDefinition.Tier3AntiMaterial();
        }

        if (entry.SpecialtyKey == "demolition" && entry.Tier == 1)
        {
            return CardKitDefinition.Tier1Kamikaze();
        }

        return CardKitDefinition.FromWeaponNames(entry.PrimaryWeapon, entry.SecondaryWeapon);
    }

    static CardDefinition CreateCard(CardEntry entry)
    {
        return new CardDefinition
        {
            id = $"{entry.SpecialtyKey}_{entry.Tier}",
            specialty = entry.SpecialtyKey,
            specialtyLabel = entry.SpecialtyLabel,
            tier = entry.Tier,
            displayName = entry.DisplayName,
            rarity = entry.Rarity,
            kit = ResolveKit(entry),
            preview = new CardPreviewStats
            {
                description = entry.Description,
                moveSpeed = entry.MoveSpeed,
                health = entry.Health,
                trapLimit = entry.TrapLimit,
                primaryWeapon = entry.PrimaryWeapon,
                secondaryWeapon = entry.SecondaryWeapon,
                passiveAbility = entry.PassiveAbility,
                sabotageNote = entry.SabotageNote,
                buildModifier = entry.BuildModifier,
                hotbarSummary = entry.HotbarSummary
            }
        };
    }

    static void Register(CardDefinition card)
    {
        All.Add(card);
        ById[card.id] = card;
    }

    struct CardEntry
    {
        public string SpecialtyKey;
        public string SpecialtyLabel;
        public int Tier;
        public string DisplayName;
        public CardRarity Rarity;
        public string Description;
        public float MoveSpeed;
        public int Health;
        public int TrapLimit;
        public string PrimaryWeapon;
        public string SecondaryWeapon;
        public string PassiveAbility;
        public string SabotageNote;
        public string BuildModifier;
        public string HotbarSummary;
    }

    static readonly CardEntry[] AllEntries =
    {
        Entry("infantry", "Infantry", 1, "Infantry", CardRarity.Common,
            "The baseline soldier. Designed to be simple, reliable, and effective at medium range. Infantry has no flashy mechanics, making it ideal for learning the game's fundamentals. Its strength comes from flexibility rather than specialization.",
            8f, 100, 5,
            "Standard Assault Rifle", "Service Pistol",
            "10 s boost: +15% speed, −20% reload & pullout, −15% recoil.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("infantry", "Infantry", 2, "Ranger", CardRarity.Rare,
            "Ranger extends Infantry into a precision rifleman. Their scoped assault rifle enters 1.8× ADS on right click, with 50% more recoil than a standard AR. Hold breath cuts scoped AR recoil in half.",
            8.2f, 110, 5,
            "Scoped Assault Rifle", "Service Pistol",
            "Hold breath up to 5 s (−50% scoped AR recoil); 5 s cooldown.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("infantry", "Infantry", 3, "Skirmisher", CardRarity.Epic,
            "Skirmishers are extremely mobile infantry specialists. They carry an assault rifle and machine pistol for flexible range, and a rapid forward dash lets them burst across open ground to reposition or close distance.",
            9.2f, 105, 5,
            "Standard Assault Rifle", "Machine Pistol",
            "Dash forward 8 m every 15 s (screen blurs for 4 s while dashing).",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("sniper", "Sniper", 1, "Sniper", CardRarity.Common,
            "A traditional sniper built around accuracy and positioning. Excels at long-range eliminations but has few special mechanics.",
            7.6f, 90, 4,
            "Standard Sniper Rifle", "Service Pistol",
            "Long-range precision with minimal special mechanics.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("sniper", "Sniper", 2, "Hunter", CardRarity.Rare,
            "Hunter trades raw sniper damage for battlefield awareness. Their single-shot hunting rifle uses iron-sight 5× ADS, and their mark ability reveals enemies ahead through walls.",
            7.8f, 95, 7,
            "Hunting Rifle", "Service Pistol",
            "Mark enemies within 300 m ahead for 4 s; 40 s cooldown.",
            "Standard sabotage tool.",
            "Stronger defensive traps than other snipers.",
            "Gun · Hammer · Blueprint"),

        Entry("sniper", "Sniper", 3, "Anti-Material", CardRarity.Legendary,
            "This is no longer a traditional sniper. Its enormous rifle pierces multiple objects and structures before exploding when it finally stops. It performs poorly against individual players because of its slow reload and cumbersome handling, but it is devastating during drill assaults and can force defenders out of cover.",
            6.8f, 100, 4,
            "Anti-Material Rifle", "Service Pistol",
            "Sticky explosive rounds; 12× ADS only; 5 s reload. Brace (E): stabilizer pivot.",
            "Devastating against drills and fortifications.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("engineer", "Engineer", 1, "Trapper", CardRarity.Rare,
            "The ultimate defensive specialist. Trapper has the largest selection of traps in the game and can deploy more active traps than any other card. Their objective is to shape enemy movement before combat even begins.",
            7.8f, 100, 12,
            "SMG", "Shotgun",
            "Largest trap selection and highest active trap limit.",
            "Standard sabotage tool.",
            "Build speed bonus.",
            "Gun · Hammer · Blueprint"),

        Entry("engineer", "Engineer", 2, "Mechanic", CardRarity.Epic,
            "Mechanic specializes in transportation and machinery. Their signature creation is a transport plane capable of quickly delivering teammates into enemy territory. Future mobility devices such as vehicles or moving platforms naturally belong to this card.",
            8f, 105, 6,
            "Carbine", "None",
            "Transport plane delivers teammates behind enemy lines.",
            "Standard sabotage tool.",
            "Build speed bonus.",
            "Gun · Hammer · Blueprint"),

        Entry("engineer", "Engineer", 3, "Architect", CardRarity.Legendary,
            "Architect fundamentally changes how construction works. Instead of placing individual blocks, they build complete structures instantly using limited-use golden blueprints. They also unlock reinforced materials that resist sniper fire and demolition explosives.",
            7.6f, 100, 6,
            "PDW", "Golden Blueprint",
            "Instant full-structure builds via limited golden blueprints.",
            "Standard sabotage tool.",
            "Reinforced materials resist sniper and demolition damage.",
            "Gun · Hammer · Blueprint"),

        Entry("support", "Support", 1, "Medic", CardRarity.Uncommon,
            "Medic focuses entirely on keeping teammates alive. They can continuously heal nearby allies and revive fallen teammates if timed correctly. Their combat ability is intentionally modest so they rely on teammates for protection.",
            8f, 95, 4,
            "SMG", "Pistol",
            "Continuous nearby healing and timed revives.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("support", "Support", 2, "Quartermaster", CardRarity.Epic,
            "Quartermaster supplies the team rather than healing it. They replenish teammates' ammunition and traps while providing mobility through deployable ziplines. Their logistical support keeps offensive pushes alive far longer than normal.",
            8f, 100, 5,
            "Carbine", "None",
            "Resupplies ammo and traps; deploys team ziplines.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("support", "Support", 3, "Captain", CardRarity.Legendary,
            "Captain leads the battlefield through rally points. Rally points grant nearby teammates healing, resupply, and movement speed bonuses, allowing an organized team to establish powerful forward operating positions.",
            8.2f, 110, 5,
            "Battle Rifle", "Sidearm",
            "Rally points heal, resupply, and boost nearby allies.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("assault", "Assault", 1, "Riot Trooper", CardRarity.Uncommon,
            "Built for breaking defensive positions. Riot Troopers trade weapon quality for survivability while advancing. Their portable shield protects them and nearby teammates from incoming fire and explosives during objective pushes.",
            7.6f, 120, 4,
            "Assault Rifle", "Pistol",
            "Carryable riot shield and baton protect the push.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint · Shield"),

        Entry("assault", "Assault", 2, "Lazerman", CardRarity.Epic,
            "The Lazerman replaces conventional bullets with a versatile laser system. One firing mode ricochets off walls, rewarding creative geometry and positioning. The second mode emits a wide beam that briefly stuns nearby enemies, making them excellent crowd-control specialists.",
            8f, 95, 4,
            "Laser Cannon", "None",
            "Ricochet laser mode and wide stun beam mode.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("assault", "Assault", 3, "Granny with a Shotgun", CardRarity.SuperSoldier,
            "One of the strangest cards in the game. Granny is incredibly fast but extremely fragile. She thrives on sudden flanks and point-blank ambushes capable of instantly changing the outcome of a fight before disappearing again.",
            9.5f, 70, 4,
            "Double-Barrel Shotgun", "Frying Pan",
            "Extreme speed with fragile health; excels at ambushes.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("assassin", "Assassin", 1, "Hitman", CardRarity.Uncommon,
            "The classic assassin. High movement speed and a lethal melee strike allow Hitman to quickly eliminate isolated enemies before escaping.",
            9f, 85, 4,
            "Silenced Pistol", "Instant-Kill Knife",
            "High speed with a lethal melee finisher.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("assassin", "Assassin", 2, "Secret Agent", CardRarity.Epic,
            "Secret Agent introduces stealth mechanics through temporary invisibility while also detecting nearby traps. They excel at infiltrating defended positions without being noticed.",
            9.2f, 90, 4,
            "Silenced Pistol", "Combat Knife",
            "Temporary invisibility and nearby trap detection.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("assassin", "Assassin", 3, "Koroshiya", CardRarity.SuperSoldier,
            "Koroshiya pushes melee combat to its limit. Short-range teleportation enables surprise attacks before delivering sweeping katana strikes or devastating charged slashes capable of instantly eliminating opponents.",
            9.4f, 90, 4,
            "Katana", "None",
            "Short-range teleport into sweeping and charged katana attacks.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("heavy", "Heavy", 1, "Heavy", CardRarity.Rare,
            "The classic tank. Heavy carries an LMG and service pistol with enormous ammunition reserves, 140 HP, and a rechargeable shield that absorbs fire until it breaks.",
            6.5f, 140, 4,
            "LMG", "Service Pistol",
            "120 HP shield (decays 12 HP/s); 30 s recharge.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("heavy", "Heavy", 2, "Cyborg", CardRarity.Epic,
            "Cyborg replaces ammunition with an overheating laser weapon. While less durable than Heavy, they regenerate health rapidly after avoiding damage, allowing them to repeatedly return to combat.",
            7f, 130, 4,
            "Laser LMG", "Laser Sword",
            "Overheating arm laser; +15% max HP and 20% HP/s regen for 6 s; 35 s cooldown.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("heavy", "Heavy", 3, "Frankenstein", CardRarity.Legendary,
            "Frankenstein abandons firearms entirely. They slowly lumber across the battlefield with immense health. Once reduced below half health, they become dramatically faster and unlock a rechargeable lunging attack capable of devastating nearby enemies.",
            6f, 180, 4,
            "Electrified Fists", "None",
            "Below half health: faster movement and rechargeable lunge attack.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("demolition", "Demolition", 1, "Kamikaze", CardRarity.Rare,
            "Kamikaze combines close-range SMG gunplay with remote C4 charges and explosive vests. They are particularly effective at breaching enemy defenses without sacrificing combat capability.",
            8f, 105, 5,
            "SMG", "C4",
            "Hold E near a teammate, enemy, or dummy for 5 s to strap on an explosive vest (self if alone). Vest wearer takes 5% less body-shot damage; on death it detonates for 130 damage at point-blank.",
            "Improved demolition efficiency.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint · Charges"),

        Entry("demolition", "Demolition", 2, "Bazooka Trooper", CardRarity.Epic,
            "Bazooka Trooper dominates medium-range sieges. Their rockets tear apart enemy buildings and force defenders from entrenched positions but leave them vulnerable in close combat.",
            7.4f, 100, 5,
            "Rocket Launcher", "Machine Pistol",
            "Rockets shred structures and displace entrenched defenders.",
            "Improved demolition efficiency.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint · Rockets"),

        Entry("demolition", "Demolition", 3, "Missile Operator", CardRarity.Legendary,
            "Missile Operator often remains behind friendly lines while remotely piloting guided missiles toward enemy fortifications. Although the missiles are difficult to control and can be destroyed mid-flight, skilled players can devastate entire defensive positions.",
            7.6f, 95, 5,
            "Pistol", "None",
            "Remotely pilot guided missiles at enemy fortifications.",
            "Improved demolition efficiency.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint · Missiles"),

        Entry("saboteur", "Saboteur", 1, "Saboteur", CardRarity.Rare,
            "Saboteur is the fastest drill infiltrator. They detect nearby traps and sabotage enemy drills significantly faster than other classes, making them the premier objective-focused attacker.",
            9f, 95, 5,
            "SMG", "Pistol",
            "Fastest drill sabotage; detects nearby traps.",
            "Fastest drill sabotage in the game.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("saboteur", "Saboteur", 2, "Hacker", CardRarity.Epic,
            "Hacker expands infiltration through electronic warfare. They detect traps from much farther away and can temporarily disable certain enemy traps and automated devices without triggering them.",
            8.6f, 95, 5,
            "Burst Rifle", "None",
            "Long-range trap detection; disables traps without triggering them.",
            "Improved sabotage efficiency.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("saboteur", "Saboteur", 3, "Drone Pilot", CardRarity.Legendary,
            "Drone Pilot controls a remotely piloted reconnaissance drone capable of spotting traps and even certain enemies through walls. Skilled players can secretly position the drone near enemy objectives before an attack, although its loud motor makes careless placement easy to discover.",
            8.4f, 95, 5,
            "SMG", "Pistol",
            "Recon drone spots traps and some enemies through walls.",
            "Improved sabotage efficiency.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint · Drone"),

        Entry("gunner", "Gunner", 1, "Gunner", CardRarity.Epic,
            "The Gunner's weapon is inaccurate but appears to have almost limitless ammunition. Continuous fire suppresses enemies by reducing their movement speed and weapon accuracy, making pushes extremely difficult.",
            7.2f, 115, 4,
            "Suppression Machine Gun", "None",
            "Sustained fire suppresses enemy movement and accuracy.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("gunner", "Gunner", 2, "Water Cannon Officer", CardRarity.Legendary,
            "Rather than dealing direct damage, Water Cannon Officer specializes in battlefield control. Powerful water blasts shove enemies out of position, blind them, and interrupt defensive formations, making them ideal for coordinated assaults.",
            7.8f, 110, 4,
            "High-Pressure Water Cannon", "Pistol",
            "Water blasts shove, blind, and disrupt defensive formations.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint"),

        Entry("gunner", "Gunner", 3, "Vulcan Operator", CardRarity.SuperSoldier,
            "Vulcan Operator is the pinnacle of suppression. Their rotary cannon uses an overheating system instead of ammunition and fires incendiary rounds capable of damaging both enemies and structures. While less accurate than conventional weapons, sustained fire can completely lock down choke points and prevent enemy advances.",
            6.8f, 120, 4,
            "Rotary Vulcan Cannon", "None",
            "Overheating incendiary rotary cannon locks down choke points.",
            "Standard sabotage tool.",
            "Standard build costs.",
            "Gun · Hammer · Blueprint")
    };

    static CardEntry Entry(string specialtyKey, string specialtyLabel, int tier, string displayName,
        CardRarity rarity, string description, float moveSpeed, int health, int trapLimit,
        string primaryWeapon, string secondaryWeapon, string passiveAbility, string sabotageNote,
        string buildModifier, string hotbarSummary)
    {
        return new CardEntry
        {
            SpecialtyKey = specialtyKey,
            SpecialtyLabel = specialtyLabel,
            Tier = tier,
            DisplayName = displayName,
            Rarity = rarity,
            Description = description,
            MoveSpeed = moveSpeed,
            Health = health,
            TrapLimit = trapLimit,
            PrimaryWeapon = primaryWeapon,
            SecondaryWeapon = secondaryWeapon,
            PassiveAbility = passiveAbility,
            SabotageNote = sabotageNote,
            BuildModifier = buildModifier,
            HotbarSummary = hotbarSummary
        };
    }
}
