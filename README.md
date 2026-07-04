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
4. Pick a team (Red, Blue, Yellow, or Green), then click **PLAY AS …** to load the arena.

In the field scene:

- WASD to move (camera-relative)
- Mouse to look (first-person camera)
- Space to jump
- F to toggle build mode
- In build mode, left click places the selected build piece
- In build mode, right click and move the mouse to choose a build piece
- In build mode, Ctrl + left-drag places a wall/window/door rectangle or
  one-axis ceiling strip
- Mouse wheel rotates wall/window/door orientation in build mode
- Esc to return to the menu

Your robot gets a random jersey number (1–99) each match, with pen-and-ink
shading on the torn team jersey and the number on the back.

Build pieces currently include walls, windows, ceilings, doors, trap doors, and
ladders. Placement previews snap toward nearby valid visible positions, require
line of sight, avoid occupied slots, and show red when the full requested shape
cannot be placed.

The menu UI and the voxel field are generated from code at runtime:

- `Assets/Scripts/MainMenuController.cs` – team picker + play flow
- `Assets/Scripts/GameSession.cs` – carries team and jersey number into the game
- `Assets/Scripts/VoxelFieldBuilder.cs` – flat 32x32 grid of white voxels
  with overhead directional lighting and cast shadows
- `Assets/Scripts/ThirdPersonController.cs` – first-person physics controller
  with build-mode placement tools
- `Assets/Scripts/CapsuleRobotVisual.cs` + `Assets/Scripts/JerseyInkUtility.cs`
  – capsule robot with pen-and-ink jersey
- `Assets/Scripts/VoxelLightingWorld.cs` – voxel occupancy and build-piece
  placement rules
- `Assets/Scripts/PenInkShadowEffect.cs` + `Assets/Scripts/PenInkShadowPost.shader`
  – pen-and-ink crosshatch post effect for shadows

## Documentation

See the [full game design document](docs/Third_Person_Shooter_Game_Design_v2.md)
for detailed mechanics, progression concepts, class sketches, and remaining
design topics.
