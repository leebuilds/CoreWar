# CoreWar

CoreWar is a fast-paced, objective-driven third-person shooter set in a
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
4. Click **PLAY** in the menu to load the voxel playing field.

In the field scene:

- WASD to move
- Mouse to look
- Space to jump
- Left click to place a voxel on the grid
- Right click to remove only voxels you placed
- Esc to return to the menu

The menu UI and the voxel field are generated from code at runtime:

- `Assets/Scripts/MainMenuController.cs` – minimalist main menu
- `Assets/Scripts/VoxelFieldBuilder.cs` – flat 32x32 grid of white voxels
  with overhead directional lighting and cast shadows
- `Assets/Scripts/SimpleFlyCamera.cs` – first-person physics controller
  (movement, jump, and grid building)
- `Assets/Scripts/PenInkShadowEffect.cs` + `Assets/Scripts/PenInkShadowPost.shader`
  – pen-and-ink crosshatch post effect for shadows

## Documentation

See the [full game design document](docs/Third_Person_Shooter_Game_Design_v2.md)
for detailed mechanics, progression concepts, class sketches, and remaining
design topics.
