# CoreWar

CoreWar is a fast-paced, objective-driven first-person shooter prototype set in a
minimalist voxel world. Teams attack enemy mining operations while defending
their own, making positioning, sabotage, and teamwork more important than
simply collecting eliminations.

## Game at a Glance

- Standard match: 4v4, with support planned for 2–4 teams
- Match length: approximately 10–15 minutes
- Teams: Red, Blue, Yellow, and Green
- Visual style: simple voxel geometry on a white grid
- Respawning: unlimited
- Primary objective: be the first team whose drill reaches the victory-point
  target

## Core Gameplay

Each team owns an indestructible mining drill that continuously generates
victory points. Eliminations award points and in-match money, while sabotaging
an enemy drill transfers points from that team to yours. Drills cannot be
destroyed, though their locations may change during a match.

Teams permanently unlock upgrades at 25%, 50%, and 75% of the victory target.
Sabotage cannot push a team below a milestone it has already reached.

Players also compete over power weapons, construct tactical voxel cover, and
deploy class-specific traps. Random events such as meteor showers, fog, low
gravity, or EMP pulses add variety between matches.

## Classes and Progression

Before a match, each player chooses two class cards and selects one of them at
the start or after respawning. Planned specialties include Infantry, Sniper,
Engineer, Support, Assault, Assassin, Heavy, Demolition, Saboteur, and Gunner.

Cards use independent rarity, specialty, and tier systems. The catalog defines
all 30 cards with display names, rarities, weapons, and descriptions (e.g.
Infantry → Ranger → Skirmisher; Gunner → Water Cannon Officer → Vulcan Operator).
Higher tiers provide more specialized playstyles rather than direct power upgrades,
and advanced cards may require progress across one or more specialties.

Persistent player progression includes cards, cosmetics, levels, titles,
emblems, statistics, achievements, and ranked rating. Match-specific progress
does not carry into future games.

## Design Priorities

- Objective play over kill farming
- Readable, minimalist visuals
- Fast decisions and coordinated team strategy
- Multiple valuable roles and playstyles
- Replayability through classes, traps, upgrades, maps, and dynamic events

## Getting Started (Unity)

This repository is a Unity project (built against Unity 6; any Unity 6.x
version should work — if Unity Hub asks, pick your installed 6000.x editor).

1. Open Unity Hub, choose **Add project from disk**, and select this folder.
2. Open the project and let Unity import the assets.
3. Open `Assets/Scenes/MainMenu.unity` and press Play.
4. **Sign in** or **create a profile**, then use the hub to manage your
   collection and start a match. If you signed in within the last hour, the game
   opens straight to the hub — use **Logout** to switch accounts.

Your **light/dark theme**, volume, UI sounds, **look sensitivity**, and **ADS
sensitivity** are saved locally in `persistentDataPath/CoreWar/settings.json`
and restored every launch.

### Main menu flow

| Screen | Purpose |
|--------|---------|
| Sign In / Sign Up | Local username + passcode profiles (stored in `persistentDataPath`, not in git) |
| Hub | Play, Decks, Settings, Logout, Quit — shown automatically when a session is still valid (1 hour of inactivity) |
| Game Modes | Scrollable list of modes (locked modes show a padlock); selecting one starts local matchmaking |
| Decks | Browse all 30 class cards (vertical scroll only); each row is ~1/3 class specialty blurb + ~2/3 three tier cards; set a two-slot loadout; preview stats |
| Match Prep | Arena loads behind card pick; press **READY** to look around and cycle hotbar; top banner counts down; movement unlocks when prep ends |
| Settings | Light/dark theme, UI volume, UI sounds toggle, look sensitivity, ADS sensitivity, account info |

**Menu controls:**

- **ESC** — back (closes modals first, then previous screen)
- **Mouse wheel** — scroll the decks collection (works even when hovering a card)
- Most buttons play procedural hover/click sounds; settings toggles and sliders
  are silent (back arrow still clicks)
- **READY** on the match prep screen locks your spawn class and dismisses the card window; a small top banner counts down while you look around and cycle hotbar (no crosshair or build previews until prep ends)
- During matchmaking, a bottom panel shows live status plus **Settings**; **Cancel matchmaking** or back/ESC warns before stopping search
- **Game modes:** **SHOOTING RANGE** requires loadout slot 1 only; **TEST ONE PLAYER** requires both slots; **TEST TWO PLAYER** is locked until online play (shows padlock)
- Hub **PLAY** is always available to browse modes even when some modes are locked

### Shooting Range (solo)

- Long flat lane (1 voxel = 1 meter) with side walls, backstop, and a **firing-line fence at 0 m**
- **10 m walk zone** behind the fence (Z = −10 to 0); you spawn at the center (~Z = −5); target distances are measured from the fence forward
- **8 tan humanoid dummies** at **10, 50, 100, 200, 300, 400, 500, 600 m**, spread across the lane — **10 m on the right**, **600 m on the left**, others interpolated in between (not a straight row)
- Red **distance sign on each dummy’s chest**, facing the firing line
- **No match prep** — instant spawn with crosshair, movement, gun, and full hotbar; starts with **loadout slot 1** card
- Matchmaking UI still runs but completes immediately (zero delay)
- Damage scales with bullet impact speed per weapon (see **Ballistics** below);
  headshots use higher per-weapon max values; player hits flash the screen red
  (blindness scales log with damage — 50% HP → 0.125 s, 99% HP → 2 s, headshots
  **2×** duration; re-hits while blind extend without flashing); **ding** on dummy
  hit (brighter on headshot); dummies respawn ~3 s after being dropped
- Bullets are visible dark spheres while in flight; destroyed on map hit (no bounce)
- Terrain uses merged panels (floor, walls, backstop, fence) for performance instead of per-voxel cubes
- Pause menu (**Esc**): **Choose Character**, **Dummy Stats** (HP slider + **Moving dummies** ON/OFF), **Reset Map**, Settings, Exit Match — submenus replace the main pause screen until you go back
- **Moving dummies** (toggle in Dummy Stats): patrol side-to-side at **2.2 m/s**, reverse at lane walls; OFF snaps dummies to spawn positions
- Choose Character opens the owned-card picker; **back** returns to pause (does not close pause)
- No stats are saved from range sessions

### In the field scene (standard modes)

- After matchmaking, the arena loads behind the match prep overlay (pick spawn class, then press **READY**)
- Before **READY**, only the card window is interactive
- After **READY**, the card window closes, a top banner shows the countdown and ready status, and you can look around and cycle hotbar (no crosshair or build previews; held tool still visible); **Esc** opens the pause menu as usual
- WASD movement unlocks when the prep countdown finishes
- Mouse to look (first-person camera)
- Space to jump
- Mouse wheel cycles equippable hotbar slots; `1`, `2`, `F`, and `H` select slots directly
- **Q** tap equips the grenade slot; **Q** hold opens the grenade radial wheel
- **E** — class ability (not equippable via scroll)
- **Esc** — pause menu (reloads, cooldowns, and ability timers keep running; player input blocked until pause closes; layered submenus for Settings / Dummy Stats / exit confirm; back restores main pause); **Exit Match** asks for confirmation; respawn is locked during prep
- **Exit Match** — fully ends the match and returns to the main menu
- Respawn picker **back** returns to pause when opened from pause (same for shooting range **Choose Character**)
- Top-right **match clock** counts up from match start (`M:SS`); hidden while pause or respawn overlays are open
- After respawn, pick loadout slot A or B from the class picker overlay; the
  player returns near the original spawn in the closest location with full
  capsule clearance from terrain and builds

While the pause menu, respawn picker, character picker, or other in-match overlays are open,
gameplay input is ignored, the crosshair is hidden, and the cursor stays free
for UI clicks. The cursor remains tied to pause (and its sub-windows) until
pause closes — even if prep finishes while paused. In-match **Settings** uses
the same form as the hub (without the account section) and respects light/dark
theme. Matchmaking and match prep overlays also refresh when the theme changes.

All in-match HUD and overlays render on a shared **Game UI Canvas** (`GameUICanvas`)
with consistent screen scaling (1920×1080 reference, match width/height 0.5).
Interactive overlays use nested canvas sort orders so buttons receive clicks above the HUD.

### Hotbar

Six on-screen slots in four groups (ability, weapons, grenade, tools) in the
**lower-left** corner at **~200% scale** (crosshair ~**250%**), with procedural
icons, thin black slot borders, and key labels in each slot corner. While
holding a firearm, a small white ammo panel with a thin black border appears
just above the row (never overlapping slots) showing `reserve / mag` (left =
reserve, right = magazine).

Layout: **`E` · `1` `2` · `Q` · `F` `H`**

| Key | Slot | Action |
|-----|------|--------|
| `E` | Ability | Class ability (cooldown overlay; not equippable via scroll) |
| `1` | Primary | Class primary (AR, Sniper, Hunting Rifle, Scoped AR, LMG, …) |
| `2` | Secondary | Pistol (or class secondary) |
| `Q` | Grenade | Tap to equip grenade slot; hold to open grenade wheel |
| `F` | Build | Enables build mode while selected |
| `H` | Hammer | Left click swings and destroys one owned build piece |
| `R` | — | Reload the held firearm (blocked during draw/reload) |

**Grenades**

| Input | Action |
|-------|--------|
| **Q tap** | Equip grenade slot (last selected type) |
| **Q hold** + drag | Radial wheel — frag or flashbang (even sectors per type count) |
| **Left click** | Throw immediately (**5 s** fuse) |
| **Right click** | Prime in hand (**5 s** fuse); left click throws with remaining fuse |

- **0.5 s** draw after equipping grenade slot before prime/throw
- **1.8 s** cooldown after a grenade leaves your hand before another can be pulled out
- **Per-life inventory (all classes):** **2** frag, **1** flashbang — resets on respawn; HUD shows count on **Q** slot
- After throwing your **last** grenade, the grenade slot stays selected for **0.5 s**, then auto-switches to your prior weapon and starts its draw timer
- While **primed** in hand: hotbar, **Q**, and grenade wheel are blocked
- Build and grenade wheels are mutually exclusive; either wheel blocks other input and hides the crosshair

**Frag grenade**

- Thrown at **30 m/s** with **9.81 m/s²** gravity (rigidbody physics, high ground friction, low roll)
- **5 s** fuse; **70** damage at **0 m** → **15** at **8 m** (line of sight)
- Layered **fireball** VFX **0.5 s** (**5 m** diameter); gun-style black blindness (not fiery explosion blindness)

**Flashbang**

- Same throw physics and **5 s** fuse; **no damage**
- **150°** view cone, **30 m** range, line of sight
- White screen: **0–4 s** full white within **15 m**, then **4 s** fade (peak alpha **100%** near → **20%** at max range)
- Second flash while blind keeps the **longer** total duration without restarting the animation
- White blindness is **visual only** — does not block reload, hotbar, or other input

**Blindness overlay order (back → front):** white (flashbang) → black → fire → red.
Black gun/explosion blindness always renders above white and **does** block input.

**Tier 1 kits**

| Card | Primary (`1`) | Secondary (`2`) | Tools |
|------|---------------|-----------------|-------|
| Infantry | AR (full auto ~400 RPM) | Pistol (semi-auto) | Build · Hammer |
| Sniper | Sniper rifle (semi-auto, ADS) | Pistol | Build · Hammer |

**Tier 2 / 3 kits (implemented)**

| Card | Primary (`1`) | Secondary (`2`) | Tools |
|------|---------------|-----------------|-------|
| Hunter (`sniper_2`) | Hunting rifle (semi-auto, iron-sight ADS) | Pistol | Build · Hammer |
| Anti-Material (`sniper_3`) | Anti-material rifle (charged ADS-only explosive round) | Pistol | Build · Hammer |
| Ranger (`infantry_2`) | Scoped AR (full auto, ADS) | Pistol | Build · Hammer |
| Skirmisher (`infantry_3`) | AR (full auto) | Machine pistol (semi-auto) | Build · Hammer |
| Heavy (`heavy_1`) | LMG (full auto) | Pistol | Build · Hammer |
| Cyborg (`heavy_2`) | Laser LMG (overheating beam) | Laser sword | Build · Hammer |
| Kamikaze (`demolition_1`) | SMG (full auto) | C4 charge + remote | Build · Hammer |
| Gunner (`gunner_1`) | Machine gun (full auto, circle reticle) | Pistol | Build · Hammer |

**E abilities**

| Card | Ability | Cooldown |
|------|---------|----------|
| Sniper | Cycle scope (Iron → 4× → 10×); **E works whenever ready, even if sniper is not equipped**; while ADS with sniper held uses swap animation | None |
| Infantry | Speed boost — **+15%** move speed, **−20%** reload & pullout, **−15%** recoil for **10 s** | 30 s |
| Hunter | Mark — reveals enemies within **300 m** ahead for **4 s** (red bullseye icon on head, through walls) | 40 s |
| Anti-Material | **Brace** — toggle stabilizer pivot: orbit anchor with A/D, reduced recoil/sway, faster ADS, **6 s** reload while braced | 45 s |
| Ranger | Hold breath — steady aim while holding E (**4 s** max); **−50%** scoped AR recoil while ADS, slower sway | 14 s |
| Skirmisher | Dash — **8 m** over **0.2 s** with full-screen blur | 8 s |
| Heavy | Shield — **120** shield HP, decays **12/s**; blue flash on health bar | 30 s after break |
| Cyborg | Regen boost — **20% HP/s** for **6 s** | 35 s |
| Kamikaze | Explosive vest — hold **E** for **5 s** within **1 m** of a teammate, enemy, or dummy (self if alone); wearer takes **5%** less body-shot damage; on death detonates (**130** at 0 m → **10** at 10 m, **8 m** build break) | 120 s |
| Gunner | Suppression boost — **7 s** max (**3000** RPM, **+20%** crosshair, wider spread, stronger suppression on hit); ends when mag hits **0** | 30 s |

**Ammo (per weapon, separate pools)**

| Weapon | Start (reserve / mag) | Reload |
|--------|------------------------|--------|
| Pistol | 150 / 12 | 1.2 s full mag |
| AR | 200 / 30 | 1.5 s full mag |
| Sniper | 40 / 5 | 1.5 s start + 0.8 s per round (interruptible after first round) |
| Hunting rifle | 48 / 1 | 2.1 s per round, manual only, locked once started |
| Anti-material rifle | 40 / 1 | **8 s** unbraced · **6 s** braced (single round) |
| Machine pistol | 150 / 18 | 1.2 s full mag |
| LMG | 200 / 55 | 4.5 s full mag |
| SMG | 150 / 25 | 1.275 s full mag |
| Machine gun (Gunner) | 1400 / 280 | 10 s full mag |
| C4 (Kamikaze) | **0 / 1** charge | — (no reload; **50 s** recharge when empty) |

When a weapon runs completely dry (magazine and reserve both **0** for that weapon),
reserve refills to its starting maximum after **30 s** without loading the magazine —
press **R** to load rounds into the gun. Pistol-family and rifle-family weapons share
reserve pools but recharge independently per weapon when that weapon's pool is empty.
C4 recharges after **50 s** when spent and **automatically** restores the ready charge (no reload key).

Reload blocks shooting, hotbar swap, and E ability (sniper and hunting rifle
fully locked through reload). Gun dips during reload; sniper per-round reload adds
a quick bob. Ammo resets on match start, respawn, and character reset.

**Weapon movement slows:** heavier weapons reduce move speed while held, ADS, or
firing (e.g. LMG **70%** held / **30%** firing; hunting rifle **85%** held /
**45%** ADS or firing).

**AR / Pistol / Sniper / Hunting rifle / Anti-material / Scoped AR / Machine pistol / LMG / Machine gun / Cyborg laser:** fire
visible bullets (or laser beam) along the **crosshair / red dot** with muzzle flash,
per-weapon gunshot audio, and recoil kick. The AR, scoped AR, LMG, and machine gun hold left
click for automatic fire; pistol, machine pistol, sniper, hunting rifle, and anti-material are
semi-auto per click (anti-material requires **1 s charge** while fully ADS). Each firearm has a draw animation before you can shoot,
reload, or ADS. Sniper and hunting rifle **right click** enters ADS (sniper
**5×** scopes; hunting rifle **6.5×** iron sights with peripheral blur). Anti-material
**right click** — **12×** zoom, **no hipfire**. Ranger
scoped AR **right click** — **1.8×** zoom.

**Machine gun (Gunner):** circle reticle (**24 px** radius); bullets land anywhere in the circle with
center-biased spread. Hits apply **suppression** — **1.5 s** movement slow (**80%** speed) and
screen flick; Gunner E boost increases suppression (**65%** speed, **2.5×** flick). Gunner E also
raises RPM to **3000**, widens the reticle **+20%**, and uses edge-biased spread until the mag is empty.

**ADS scope sway:** precision weapons drift smoothly while scoped (semi-random phases of
stillness, wide arcs, and occasional fast spurts). Per-weapon amplitude and speed differ;
Ranger hold breath slows sway further. Bullets always fire where the reticle points, including
during sway.

Bullets use real-world gravity,
per-weapon air drag, spawn from a clamped point near the player when up against
walls, and **cannot damage the shooter**.

**Hammer:** destroys the first player-built object hit within **1.5 voxels** of
any part of the player's body (measured from the capsule surface).

**Kamikaze C4:** secondary slot pulls out C4 in **0.8 s** (held model hidden when out of
charges). Left click throws one charge at **10 m/s** with **8 m/s²** downward fall
acceleration; it sticks flat to surfaces or players. Throwing blocks reload, slot
switching, and other weapon actions for **1 s** but you can still move and look.
Keeping the secondary selected after the charge has armed (**2 s** attached) pulls a
remote in **0.5 s**; pressing it queues a **1 s** detonation. The blast breaks player builds within **8 m** and
deals player/dummy damage linearly from **130 at 0 m** to **5 at 10 m**. **Any explosion**
(including frag grenades, C4, anti-material, and vest chain blasts) detonates equipped
explosive vests within the blast damage radius. Shot damage treats C4 as an entity:
only **body-shot** damage counts, and **30** accumulated damage detonates the charge
(headshots ignored) — bullets, grenades, fiery explosions, and laser sword hits all contribute.
Uses the same fiery explosion blindness as anti-material rounds (orange fireball, black
follow-up, input block). C4 detonates when damaged, attaches to players/dummies who touch it
while it is falling, and drops if the attached player or dummy is eliminated before detonation.
The thrower cannot stick a charge to themselves for **1 s** after throwing.

**Build:** full build-mode toolset (see below).

New accounts unlock **Infantry**, **Sniper**, **Hunter**, **Anti-Material**, **Ranger**,
**Skirmisher**, **Heavy**, **Cyborg**, and **Gunner** by default; other cards show LOCK in Decks
until earned. Higher-tier and other specialty kits are planned.

### Build mode (blueprint slot)

- Left click places the selected build piece
- Right click and move the mouse to choose a build piece (radial selector)
- Ctrl + left-drag places a wall/window rectangle or horizontal ceiling rectangle
- `X` rotates wall/window/door orientation
- Pressing `X` without moving the mouse keeps the targeted voxel fixed while
  cycling orientations
- `Z` toggles wall/window/door orientation lock

Build pieces include walls, windows, ceilings, doors, trap doors, and ladders.
Placement previews snap toward nearby valid visible positions on the near side of
geometry, require one complete visible half-face (top, bottom, left, or right)
for every piece, avoid occupied slots, and show red when the full requested
shape cannot be placed. Ctrl-drag still draws outlines over occupied cells but
skips them for validation and placement.

Your robot gets a random jersey number (1–99) each match, with pen-and-ink
shading on the torn team jersey and the number on the back.

Gun bullets use raycast-integrated flight with gravity and air drag. They pass
through anything the player builds (losing speed based on how much material they
cross), then stop and are destroyed when they hit map floors or walls — no
bounce. Sniper rounds above **500 m/s** can penetrate players/dummies with speed
and accuracy penalties per hit.

### Ballistics and damage

| Weapon | Muzzle speed | Max body | Max headshot |
|--------|--------------|----------|--------------|
| Pistol | 325 m/s | 13 | 30 |
| AR | 850 m/s | 17 | 22 |
| Sniper | 950 m/s | 80 | 100 |
| Hunting rifle | 950 m/s | 65 | 160 |
| Anti-material rifle | 1300 m/s | 90 | 102 |
| Machine pistol | 400 m/s | 20 | 40 |
| LMG | 800 m/s | 30 | 60 |
| Machine gun | 2000 m/s | 7 | 9 |
| Cyborg laser | Hitscan beam | 10 | 15 |

**Explosions (anti-material, C4, future):** shared fiery blindness — **2×** damage-based
duration, orange/red fireball overlay inside **4 m**, black follow-up, input blocked while
blind; **7 s** max total blindness.

**Anti-material explosion:** **10 m** radius, **10–100** exponential damage; **4.5 m** build
destruction; **2 s** stick fuse.

**Damage formula:** `maxDamage × Lerp(0.5, 1.0, impactSpeed / muzzleSpeed)` —
50% of max at 0 m/s, 100% at muzzle velocity.

**Air drag** (exponential per 100 m): pistol ~**25%** loss, AR ~**5%**, sniper
and hunting rifle ~**4%**, anti-material ~**3%**, machine pistol ~**50%**, LMG ~**5.5%**,
machine gun ~**24%**.

**Players:** hits below **30 m/s** apply no damage and destroy the bullet; at
**≥ 30 m/s** velocity-scaled damage applies and the bullet is destroyed (sniper
can penetrate above **500 m/s**).

**Shooting range dummies:** velocity-scaled damage at all impact speeds.

Bullets are visible dark spheres while airborne (no global cap; 20 s failsafe
destroy). Holding fire during match prep does not trigger a shot when gametime
starts — release the mouse first.

## Runtime architecture

The menu UI and the voxel field are generated from code at runtime:

**Menu & profiles**

- `Assets/Scripts/SceneFlow.cs` — scene transitions, cursor/input resets, EventSystem cleanup
- `Assets/Scripts/MainMenuController.cs` — menu scene bootstrap (camera, audio listener, navigator)
- `Assets/Scripts/UI/MenuNavigator.cs` — sign-in, hub, game modes, matchmaking flow, decks, settings routing; theme backdrop
- `Assets/Scripts/Matchmaking/` — `GameModeDefinition`, `MatchmakingSession`, `IMatchmakingBackend`, local sim backend (per-mode playability and instant matchmaking for shooting range)
- `Assets/Scripts/UI/GameModeButtonFx.cs` — bullet holes + smoke on selected game mode button
- `Assets/Scripts/UI/MatchmakingPanel.cs` — bottom matchmaking status panel (feed, timer, count, cancel)
- `Assets/Scripts/UI/MatchClassSelectPanel.cs` — in-arena spawn picker, READY, 10s prep countdown
- `Assets/Scripts/UI/MatchPrepController.cs` — boots prep overlay after matchmaking loads the game scene
- `Assets/Scripts/UI/GameUICanvas.cs` — shared in-game canvas bootstrap, layers, interaction layers, screen hosts
- `Assets/Scripts/UI/GameplayHud.cs` — crosshair, scaled hotbar, ammo panel, build selector, health bar, shield flash, ability icons
- `Assets/Scripts/UI/MatchClockHud.cs` — in-match elapsed time HUD (top-right)
- `Assets/Scripts/UI/MenuWindowFrame.cs` — shared window chrome (military title bar, header, footer)
- `Assets/Scripts/UI/MenuUiFactory.cs` — buttons, inputs, sliders, light/dark styling tokens, `WhiteSprite`, `EnsureEventSystem`
- `Assets/Scripts/UI/MenuSettings.cs` — persistent client settings (`settings.json`)
- `Assets/Scripts/UI/MenuSettingsPanel.cs` — shared settings form (hub + pause menu)
- `Assets/Scripts/UI/MenuUiSounds.cs` — procedural hover, click, gunshot sounds
- `Assets/Scripts/UI/GamePauseMenu.cs` — in-match pause overlay with layered submenus (range: Choose Character, Dummy Stats; all modes: Settings, Exit Match)
- `Assets/Scripts/UI/RespawnClassPicker.cs` — respawn class selection
- `Assets/Scripts/UI/ShootingRangeCharacterPicker.cs` — owned-card collection overlay for range character swaps
- `Assets/Scripts/UI/ShootingRangeDummyStatsPanel.cs` — logarithmic dummy HP slider + moving dummies toggle
- `Assets/Scripts/UI/PlayerBulletHitFlash.cs` — full-screen blindness (white flashbang, black gun hits, fire, red fade); `BlocksGameplayInput` vs visual-only white
- `Assets/Scripts/UI/PlayerDamageDebugPanel.cs` — pause-menu test damage slider (debug blindness tuning)
- `Assets/Scripts/UI/DecksCollectionView.cs` — shared owned-card scroll builder
- `Assets/Scripts/UI/CardTileView.cs` — collection card tiles (compact + deck-row sizing), spawn selection visuals
- `Assets/Scripts/UI/DecksLayout.cs` — decks window and row width constants
- `Assets/Scripts/Profile/` — local profile repository, 1-hour session restore, passcode hashing
- `Assets/Scripts/Cards/ClassSpecialtyDescriptions.cs` — class role blurbs for the decks collection
- `Assets/Scripts/UI/ClassSpecialtyPanel.cs` — specialty column in each decks row (name, symbol slot, role text)
- `Assets/Scripts/Cards/` — 30-card catalog (names, rarities, preview stats), rarity colors, placeholder kits

**Gameplay**

- `Assets/Scripts/GameSession.cs` — team, loadout, game mode, match clock, and active card into the game scene
- `Assets/Scripts/VoxelFieldBuilder.cs` — flat grid of white voxels (32×32 standard; 48×680 shooting range), grippy floor + slippery wall physics materials, lighting
- `Assets/Scripts/ShootingRange/` — merged terrain + firing-line fence, spread dummies (optional patrol movement), hit zones, session state (bullets, reset)
- `Assets/Scripts/VoxelMaterialUtility.cs` — solid-color materials for range props and hit flashes
- `Assets/Scripts/ThirdPersonController.cs` — first-person controller, hotbar, ammo/reload, all class weapons/build/hammer/grenades, sniper/hunting rifle/anti-material/scoped AR ADS, scope sway, E abilities, movement slows, pause
- `Assets/Scripts/GrenadeType.cs` — frag and flashbang enum
- `Assets/Scripts/ThrownGrenadeProjectile.cs` — shared grenade rigidbody throw physics
- `Assets/Scripts/FragGrenadeProjectile.cs` / `FlashbangGrenadeProjectile.cs` — type-specific detonation
- `Assets/Scripts/GrenadeBlastUtility.cs` — frag damage, LOS, gun-style blindness
- `Assets/Scripts/FlashbangBlindUtility.cs` — flashbang cone, range, white-blind duration/alpha
- `Assets/Scripts/FragGrenadeSmokeEffect.cs` / `FlashbangBurstEffect.cs` — grenade VFX (frag fireball)
- `Assets/Scripts/MachineGunSuppressionUtility.cs` — machine gun suppression timer and move slow
- `Assets/Scripts/ExplosiveVestState.cs` — Kamikaze vest equip, damage reduction, explosion detonation
- `Assets/Scripts/C4ChargeProjectile.cs` — thrown C4, stick, remote detonation, damage threshold
- `Assets/Scripts/ExplosionBlastUtility.cs` — shared explosion damage, fiery blindness, build break, VFX
- `Assets/Scripts/AntiMaterialProjectile.cs` — sticky explosive anti-material round
- `Assets/Scripts/AntiMaterialExplosionEffect.cs` — detonation fireball VFX
- `Assets/Scripts/HunterMarkSystem.cs` — Hunter mark target scan and apply/clear
- `Assets/Scripts/HunterMarkOverlay.cs` — screen-space Hunter mark icons on `GameUICanvas`
- `Assets/Scripts/HunterMarkOutlineDrawer.cs` — procedural bullseye + teardrop mark sprite
- `Assets/Scripts/FullScreenBlur.shader` — Ranger hold breath / Skirmisher dash blur
- `Assets/Scripts/WeaponAmmo.cs` — per-weapon reserve + magazine pools and reload timing defaults
- `Assets/Scripts/UI/HotbarIconDrawer.cs` — procedural hotbar slot icons
- `Assets/Scripts/ProjectileBullet.cs` — raycast bullet flight, air drag, build penetration, sniper player penetration, owner ignore (no self-damage)
- `Assets/Scripts/ProjectileDamage.cs` — per-weapon velocity damage, air drag constants, player hit threshold
- `Assets/Scripts/SniperScopePostEffect.cs` + `Assets/Scripts/SniperScopePost.shader` — sniper ADS blur overlay (iron sights: blur only; magnified: blur + vignette)
- `Assets/Scripts/PlayerHealth.cs` — local player HP, shield, blindness triggers (no death flow yet)
- `Assets/Scripts/CapsuleRobotVisual.cs` + `Assets/Scripts/JerseyInkUtility.cs` — capsule robot with jersey
- `Assets/Scripts/VoxelLightingWorld.cs` — voxel occupancy, build rules, hammer removal
- `Assets/Scripts/PenInkShadowEffect.cs` + `Assets/Scripts/PenInkShadowPost.shader` — pen-and-ink shadows

Local profile, session, and settings JSON are written to
`Application.persistentDataPath/CoreWar/` and are excluded from git via
`.gitignore` (`profiles/`, `settings.json`, `session.json`).

## Documentation

- [Full game design document](docs/First_Person_Shooter_Game_Design_v2.md)
- [Gunner, universal grenades, C4/vest explosions, and Ranger fix session recap](docs/chats/2026-07-10-gunner-grenades-c4-ranger-session.md)
- [Grenades, flashbangs, and blindness layering session recap](docs/chats/2026-07-09-grenades-flashbang-and-blindness-session.md)
- [Anti-Material sniper, scope sway, Cyborg, and Infantry ability session recap](docs/chats/2026-07-09-anti-material-sway-and-infantry-session.md)
- [Hunter, Ranger, Skirmisher, Heavy, and shooting range polish session recap](docs/chats/2026-07-09-hunter-ranger-skirmisher-heavy-session.md)
- [Unified game UI, HUD polish, and combat tuning session recap](docs/chats/2026-07-08-unified-game-ui-hud-and-combat-tuning-session.md)
- [Ammo, reload, ballistics rewrite, and hotbar icons session recap](docs/chats/2026-07-07-ammo-reload-ballistics-and-hotbar-icons-session.md)
- [Sniper unlocks, ballistics, and abilities session recap](docs/chats/2026-07-07-sniper-unlocks-ballistics-and-abilities-session.md)
- [Ballistics, hotbar, and player damage session recap](docs/chats/2026-07-07-ballistics-hotbar-and-player-damage-session.md)
- [Shooting range solo mode and layout polish session recap](docs/chats/2026-07-05-shooting-range-mode-session.md)
- [Decks collection layout and card catalog session recap](docs/chats/2026-07-05-decks-layout-and-card-catalog-session.md)
- [In-arena prep, pause polish, and overlay theming session recap](docs/chats/2026-07-05-in-arena-prep-pause-flow-session.md)
- [Matchmaking, pre-match flow, and menu polish session recap](docs/chats/2026-07-05-matchmaking-prep-flow-session.md)
- [Settings, theme, session, and menu polish session recap](docs/chats/2026-07-05-settings-theme-menu-polish-session.md)
- [Profile, decks, loadout, and menu UI session recap](docs/chats/2026-07-04-profile-decks-loadout-menu-session.md)
- [Hotbar tools and combat session recap](docs/chats/2026-07-04-hotbar-tools-and-combat-session.md)
- [FPS build mode session recap](docs/chats/2026-07-03-fps-build-mode-session.md)
