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

Cards use independent rarity, specialty, and tier systems. Higher tiers provide
more specialized playstyles rather than direct power upgrades, and advanced
cards may require progress across one or more specialties.

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

Your **light/dark theme**, volume, UI sounds, and mouse sensitivity are saved
locally in `persistentDataPath/CoreWar/settings.json` and restored every launch.

### Main menu flow

| Screen | Purpose |
|--------|---------|
| Sign In / Sign Up | Local username + passcode profiles (stored in `persistentDataPath`, not in git) |
| Hub | Play, Decks, Settings, Logout, Quit — shown automatically when a session is still valid (1 hour of inactivity) |
| Game Modes | Scrollable list of modes; selecting one starts local matchmaking (test one/two player) |
| Decks | Browse all 30 class cards (scroll starts at top); set a two-slot loadout; preview stats |
| Match Prep | After matchmaking, pick spawn class, **READY**, or wait for 10s prep countdown |
| Settings | Light/dark theme, UI volume, UI sounds toggle, mouse sensitivity, account info |

**Menu controls:**

- **ESC** — back (closes modals first, then previous screen)
- **Mouse wheel** — scroll the decks collection (works even when hovering a card)
- Most buttons play procedural hover/click sounds; settings toggles and sliders
  are silent (back arrow still clicks)
- **READY** on the match prep screen also plays a gunshot when the arena loads
- During matchmaking, a bottom panel shows live status; **Cancel matchmaking** or back/ESC warns before stopping search

### In the field scene

- WASD to move (camera-relative)
- Mouse to look (first-person camera)
- Space to jump
- Mouse wheel cycles the hotbar; `1`, `2`, and `3` select slots directly
- **Esc** — pause menu (game does not freeze; opens Respawn / Settings / Exit Match)
- **Exit Match** — fully ends the match and returns to the main menu
- Top-right **match clock** counts up from match start (`M:SS`); hidden while pause or respawn overlays are open
- After respawn, pick loadout slot A or B from the class picker overlay

While the pause menu or respawn picker is open, gameplay input is ignored, the
crosshair is hidden, and the cursor is free for UI clicks. In-match **Settings**
uses the same form as the hub (without the account section).

### Hotbar

| Slot | Tool | Action |
|------|------|--------|
| 1 | Gun | Left click fires a semi-auto placeholder gun |
| 2 | Hammer | Left click swings and destroys one owned build piece |
| 3 | Blueprint | Enables build mode while selected |

**Gun:** fires a fast visible bullet along the current crosshair aim, then
applies a brief recoil kick. Bullets have light gravity, muzzle flash, and spawn
from a clamped point near the player when up against walls. Recoil kicks mostly
upward with a small horizontal component; the crosshair settles away from the
shot line after each kick (stronger randomness = less predictable aim).

**Hammer:** destroys the first player-built object hit within **1.5 voxels** of
any part of the player's body (measured from the capsule surface).

**Blueprint:** full build-mode toolset (see below).

All 30 class cards currently share the same placeholder kit (gun, hammer,
blueprint). Per-class kits are planned.

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
geometry, require line of sight to each piece center, avoid occupied slots, and
show red when the full requested shape cannot be placed. Ctrl-drag still draws
outlines over occupied cells but skips them for validation and placement.

Your robot gets a random jersey number (1–99) each match, with pen-and-ink
shading on the torn team jersey and the number on the back.

Gun bullets are visual prototype projectiles. They pass through player-built
panels, lose a little velocity, leave temporary bullet-hole marks, and disappear
after landing or timing out.

## Runtime architecture

The menu UI and the voxel field are generated from code at runtime:

**Menu & profiles**

- `Assets/Scripts/SceneFlow.cs` — scene transitions, cursor/input resets, EventSystem cleanup
- `Assets/Scripts/MainMenuController.cs` — menu scene bootstrap (camera, audio listener, navigator)
- `Assets/Scripts/UI/MenuNavigator.cs` — sign-in, hub, game modes, matchmaking flow, decks, settings routing; theme backdrop
- `Assets/Scripts/Matchmaking/` — `GameModeDefinition`, `MatchmakingSession`, `IMatchmakingBackend`, local sim backend
- `Assets/Scripts/UI/GameModeButtonFx.cs` — bullet holes + smoke on selected game mode button
- `Assets/Scripts/UI/MatchmakingPanel.cs` — bottom matchmaking status panel (feed, timer, count, cancel)
- `Assets/Scripts/UI/MatchClassSelectPanel.cs` — post-matchmaking spawn picker, READY, 10s prep countdown
- `Assets/Scripts/UI/MatchClockHud.cs` — in-match elapsed time HUD (top-right)
- `Assets/Scripts/UI/MenuWindowFrame.cs` — shared window chrome (military title bar, header, footer)
- `Assets/Scripts/UI/MenuUiFactory.cs` — buttons, inputs, sliders, light/dark styling tokens
- `Assets/Scripts/UI/MenuSettings.cs` — persistent client settings (`settings.json`)
- `Assets/Scripts/UI/MenuSettingsPanel.cs` — shared settings form (hub + pause menu)
- `Assets/Scripts/UI/MenuUiSounds.cs` — procedural hover, click, gunshot sounds
- `Assets/Scripts/UI/GamePauseMenu.cs` — in-match pause overlay
- `Assets/Scripts/UI/RespawnClassPicker.cs` — respawn class selection
- `Assets/Scripts/UI/CardTileView.cs` — collection card tiles, spawn selection visuals
- `Assets/Scripts/Profile/` — local profile repository, 1-hour session restore, passcode hashing
- `Assets/Scripts/Cards/` — 30-card catalog, rarity colors, placeholder kits

**Gameplay**

- `Assets/Scripts/GameSession.cs` — team, loadout, game mode, match clock, and active card into the game scene
- `Assets/Scripts/VoxelFieldBuilder.cs` — flat 32×32 grid of white voxels with lighting
- `Assets/Scripts/ThirdPersonController.cs` — first-person controller, hotbar, tools, pause
- `Assets/Scripts/ProjectileBullet.cs` — visual bullet flight and bullet holes
- `Assets/Scripts/CapsuleRobotVisual.cs` + `Assets/Scripts/JerseyInkUtility.cs` — capsule robot with jersey
- `Assets/Scripts/VoxelLightingWorld.cs` — voxel occupancy, build rules, hammer removal
- `Assets/Scripts/PenInkShadowEffect.cs` + `Assets/Scripts/PenInkShadowPost.shader` — pen-and-ink shadows

Local profile, session, and settings JSON are written to
`Application.persistentDataPath/CoreWar/` and are excluded from git via
`.gitignore` (`profiles/`, `settings.json`, `session.json`).

## Documentation

- [Full game design document](docs/Third_Person_Shooter_Game_Design_v2.md)
- [Matchmaking, pre-match flow, and menu polish session recap](docs/chats/2026-07-05-matchmaking-prep-flow-session.md)
- [Settings, theme, session, and menu polish session recap](docs/chats/2026-07-05-settings-theme-menu-polish-session.md)
- [Profile, decks, loadout, and menu UI session recap](docs/chats/2026-07-04-profile-decks-loadout-menu-session.md)
- [Hotbar tools and combat session recap](docs/chats/2026-07-04-hotbar-tools-and-combat-session.md)
- [FPS build mode session recap](docs/chats/2026-07-03-fps-build-mode-session.md)
