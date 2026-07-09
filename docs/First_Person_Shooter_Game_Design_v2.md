# Game Design Document (Working Draft)

# Core Concept

A fast-paced third-person shooter with simple, readable visuals.

- White grid ground.
- Minimal voxel world.
- 2–4 teams:
  - Red
  - Blue
  - Yellow
  - Green
- Teams are always added in that order (e.g. 3 teams = Red, Blue, Yellow).

The game is designed around **attacking enemy mining operations while defending your own**, rather than simply earning kills.

---

# Win Condition

Each team owns a mining drill.

The drill constantly mines resources and generates team points.

The first team to reach the required number of points wins.

Final standings are:

1. First
2. Second
3. Third
4. Fourth (if applicable)

---

# Point System

## Passive Mining

- The drill continuously earns points.
- Remaining near your team's drill allows you to receive the points it generates.

## Combat

When you eliminate an enemy player:

- Their team loses points.
- Your team gains those points **only if you are within range of your own drill**.
- If you are away from your drill, the enemy still loses points, but your team does not receive them.

This encourages players to balance offense with defense.

## Sabotage

Players can infiltrate enemy territory and sabotage the enemy drill.

Sabotage:

- Removes points faster than player eliminations.
- Is much riskier.
- Rewards coordinated attacks.

---

# Player Progression

Every player has an account.

Progress persists between matches.

Examples include:

- Unlocking cards
- Cosmetics
- Statistics
- Achievements

No match progress carries into future games.

---

# Card / Class System

Each player builds a personal deck of cards.

Before each match:

- Choose **two** class cards.

Examples:

- Infantry
- Sniper
- Heavy
- Medic
- Engineer

When:

- the match begins
- or the player respawns

they choose one of those two classes.

This allows players to adapt during the match while keeping meaningful loadout decisions.


## Card Progression

Cards are defined by **three core attributes**:

1. **Rarity**
2. **Specialty**
3. **Tier**

These are independent concepts that work together to create progression.

### Specialty

A specialty represents the general combat role of a card.

Examples:

- Sniper
- Infantry
- Heavy
- Medic
- Engineer
- Hunter

Future specialties can be added over time.

### Tier

Tiers represent mastery within a specialty.

Examples:

- Tier 1 Sniper
- Tier 2 Sniper
- Tier 3 Sniper

A higher tier does **not** simply mean stronger. Instead, it represents more specialized and advanced versions of that combat role.

Example progression:

- Common Sniper → Tier 1 Sniper
- Rare Heavy Sniper → Tier 2 Sniper
- Super Rare Hunter → Tier 2 Sniper

Players must unlock prerequisite cards before later-tier cards become available.

### Rarity

Rarity determines how difficult a card is to obtain.

Example rarities:

- Common
- Uncommon
- Rare
- Epic
- Legendary

Higher rarity does not necessarily correspond to a higher tier. Both systems exist independently.

### Prerequisites

Some advanced cards require ownership of earlier cards.

Examples:

- A Tier 2 Sniper may require several Tier 1 Sniper cards.
- A rare Heavy Sniper may require previous Sniper progression.
- Extremely rare cards may require progression in multiple specialties simultaneously.

Example:

A Legendary card could require:

- Tier 3 Sniper
- Tier 2 Heavy
- Tier 2 Engineer

before it becomes unlockable.

This creates a progression tree where players gradually unlock increasingly specialized classes instead of immediately obtaining the strongest cards.


---

# Traps (Major Gameplay Pillar)

Traps are intended to be one of the defining mechanics of the game.

Players can deploy traps to:

- Protect their drill.
- Secure important routes.
- Defend sabotage locations.
- Slow enemy pushes.
- Create ambushes.

Potential trap ideas:

- Land mines
- Spike traps
- Laser tripwires
- Explosive barrels
- Hidden voxel pitfalls
- Sticky slowing fields
- Alarm sensors
- Smoke emitters
- Decoy beacons
- Remote explosives

Different classes and future cards can specialize in trap placement or detection.

---

# Team Upgrades

As a team reaches mining milestones, everyone receives upgrades.

Upgrades occur **3–4 times throughout the match at evenly spaced percentages of the victory objective**.

Example milestones:

- 25%
- 50%
- 75%
- (Optional final milestone near 90%)

Possible upgrades:

- Faster mining
- Faster respawn
- Stronger drill defenses
- Increased movement speed
- Better ammunition capacity

These upgrades keep matches escalating naturally.

---

# Building System

Players can construct simple voxel structures.

Examples:

- Walls
- Cover
- Small barricades
- Short ramps

Building is intended for tactical positioning rather than large-scale fort building.

---

# Dynamic Events

Random events occasionally affect every team.

Possible events:

- Meteor shower
- Thick fog
- Low gravity
- Resource surge
- EMP pulse
- Sandstorm
- Earthquake that changes terrain

These events help each match feel unique.

---

# Power Weapons

Special weapons spawn at contested areas of the map.

Examples:

- Rocket launcher
- Railgun
- Minigun
- Grenade launcher
- Energy weapon

Teams must decide whether fighting over these weapons is worth leaving objectives.

---

# Maps

Initial maps should remain relatively simple.

Focus on:

- Clear sightlines
- Multiple attack routes
- Small amounts of verticality
- Readable layouts
- Balanced drill placement

More advanced map design can be explored later.

---

# Match Awards

Players earn awards at the end of every match.

Examples:

- MVP
- Best Defender
- Master Saboteur
- Most Eliminations
- Most Points Stolen
- Best Accuracy
- Trap Master
- Objective Defender
- Clutch Player
- Survivor

This rewards multiple playstyles rather than only winning.

---

# Design Goals

The game should emphasize:

- Objective play over kill farming.
- Team strategy.
- Fast decision making.
- Multiple ways to contribute.
- Readable, minimalist visuals.
- High replayability through different class combinations, traps, upgrades, and dynamic events.




# Additional Core Design Decisions

## Match Size

- Standard format: **4v4**.

## Match Length

- Target duration: **10–15 minutes**.

## Respawning

- Players respawn indefinitely.
- There is no ticket or lives system.

## Drill Rules

- Each team has **one drill**.
- Drills **cannot be destroyed**, only sabotaged.
- Drill locations begin in balanced positions but may be relocated during the match.

## Revised Point System

The drill is the primary source of victory points.

### Mining
- Your drill continuously generates victory points for your team regardless of player location.
- Players do not need to remain near the drill for it to mine.

### Eliminations
- Eliminating enemy players grants victory points to your team.
- Eliminations also award in-match money.
- Enemy teams do **not** lose victory points when one of their players is eliminated.

### Sabotage
- Sabotaging an enemy drill is the **only** method of changing victory points between teams.
- Successful sabotage:
  - Removes victory points from the enemy.
  - Adds those same points to your team.
- Team upgrade milestones at **25%, 50%, and 75%** are permanent.
- If a team has already reached a milestone, sabotage cannot reduce them below that checkpoint, although the sabotaging team still receives the full point reward.
- This makes objective play the primary path to victory.

## Team Upgrades

Team milestone upgrades remain unlocked several times throughout the match.

They unlock when a team reaches 25%, 50%, and 75% of the total victory points required to win. Once unlocked, these milestones cannot be lost.

## Player Economy

Players earn money through:

- Eliminations
- Time survived / played
- Completing map objectives
- Being the leading team

Money is spent on:

- Building materials
- (Future expansion)

## Building

Building costs money.

Most specialties build equally, although builder-oriented specialties can construct faster, cheaper, or with stronger structures.

Structures:
- Have health.
- Can only be destroyed by explosive damage.
- Bullets may penetrate certain building materials.

## Traps

Every specialty has access to its own trap set.

Trap rules:

- Maximum of **5 active traps per player**.
- Trap limit belongs to the individual player, not the team.
- Players may dismantle only their own traps.
- Enemy players cannot see friendly trap indicators.
- Enemy players may destroy or trigger traps.

## Card Progression Rules

- Approximately 10 specialties are planned.
- Each specialty has between **2 and 4 tiers**.
- Different specialties do not need the same number of tiers.
- Higher-tier cards are alternative playstyles rather than direct upgrades.
- Cards are permanently unlocked.
- Duplicate cards do not exist.

## Player Progression

Players gain experience.

Experience increases player level.

Rewards are earned through:

- Level-ups
- Daily rewards
- Quests

## Persistent Profile

The following are permanently stored:

- Cards
- Cosmetics
- Player Level
- Titles
- Emblems
- Statistics
- Ranked Rating

## Maps

- Maps are currently handcrafted.
- Drill spawn locations begin in balanced positions.


## Sabotage Mechanics

Every player carries a standard sabotage tool.

- The tool is used at melee range against enemy drills.
- Successfully using it transfers victory points from the enemy team to your own.
- Future specialties may gain ranged sabotage capabilities or improved sabotage efficiency.


# Draft Card Progression Sketch

> This section is an early design sketch and is expected to change.

## Design Philosophy
- Higher tiers represent increasingly unique and specialized operators rather than direct upgrades.
- Higher rarities generally indicate more unusual mechanics and playstyles.
- The setting is intentionally "anything goes."

## Rarity
| Rarity | Abbreviation |
|---|---|
| Common | C |
| Uncommon | UC |
| Rare | R |
| Epic | E |
| Legendary | L |
| Super Soldier | SS |

## Specialty Sketch
- Infantry: Infantry → Marksman → Scout (or Commando / Ranger)
- Sniper: Sniper → Hunter → Heavy Sniper (or Deadeye)
- Engineer: Engineer → Trapper → Advanced Builder (Architect / Fortifier / Constructor / Master Engineer)
- Support: Medic → Wizard → Captain (or Field Commander)
- Assault: Riot Trooper → Water Cannon Officer → Granny with a Shotgun
- Assassin: Mafia → Secret Agent (or Operative) → Koroshiya (or Phantom / Ghost)
- Heavy: Heavy → Cyborg → Frankenstein
- Demolition: Explosion Specialist (or Demolition Expert / Demolitionist / Sapper) → Bazooka → Missile Operator
- Saboteur: Saboteur → Hacker → Drone Pilot (or Drone Commander)
- Gunner: Gunner → Lazerman (or Laser Gunner) → Machine Gunner (placement TBD)

# Remaining Design Topics
- Finalize each specialty.
- Build the complete card prerequisite tree.
- Design weapons, traps, gadgets, and passives.
- Expand building materials and structures.
- Balance mining, kills, sabotage, and money.
- Design maps and additional game modes.
- Flesh out progression, ranked play, cosmetics, UI, and audio.
