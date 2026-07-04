# Chat Recap: Unity Voxel Prototype Session

**Date:** July 3, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Design reference:** [Third_Person_Shooter_Game_Design_v2.md](../Third_Person_Shooter_Game_Design_v2.md)

This document summarizes everything built and decided across the Cursor chat session that scaffolded the Unity prototype—from an empty repo with design docs only through a playable third-person voxel arena with team selection and a capsule robot player.

---

## 1. Starting point

- Repo contained only `README.md`, `docs/Third_Person_Shooter_Game_Design_v2.md`, and a minimal `.gitignore`.
- No Unity project existed yet; Unity was not installed on the dev machine at scaffold time.
- Design doc priorities: minimalist **white grid ground**, **voxel world**, 2–4 teams (Red, Blue, Yellow, Green), readable visuals, objective-driven TPS gameplay.

---

## 2. Unity project scaffold

Created a full Unity 6 project structure:

| Area | What was added |
|------|----------------|
| Scenes | `Assets/Scenes/MainMenu.unity`, `Assets/Scenes/Game.unity` |
| Scripts | Runtime-generated menu, field, player, lighting, shaders |
| Settings | `ProjectSettings/`, `Packages/manifest.json`, build settings |
| Git | Expanded `.gitignore` for Unity (`Library/`, `Temp/`, etc.) |

**Approach:** Scenes stay nearly empty; bootstrap scripts generate UI, voxels, lighting, and player at runtime. This keeps scene files trivial and makes iteration script-driven.

**Getting started (from README):**

1. Open project in Unity Hub (Unity 6.x).
2. Play `MainMenu.unity`.
3. Pick team → enter Game scene.

---

## 3. Main menu

**Script:** `Assets/Scripts/MainMenuController.cs`

- Minimalist UI: **COREWAR** title, team picker, play, quit.
- Built entirely from code (Canvas, buttons, text).
- **Team selection:** Red, Blue, Yellow, Green (design doc order).
- Play button label: `PLAY AS [TEAM]`.
- On play: `GameSession.BeginMatch(team)` then loads `Game` scene.

---

## 4. Voxel playing field

**Script:** `Assets/Scripts/VoxelFieldBuilder.cs`

- Flat **32×32** grid of unit voxels; top surface at **y = 0**.
- White grid texture with gray outlines per block (design doc “white grid ground”).
- Configurable: `gridWidth`, `gridLength`, `voxelSize`, `maxBuildHeight` on Field Bootstrap.
- **Seal overlap:** voxels rendered at `1.002×` scale so neighbors overlap slightly—prevents sub-voxel light leaks through corner/edge cracks in shadow maps.

**Grid manager:** `Assets/Scripts/VoxelLightingWorld.cs`

- Tracks voxel occupancy, place/remove rules, grid ↔ world cell conversion.
- Player-placed voxels marked with `PlayerBuiltVoxel` (removable only by builder).

---

## 5. Player controller evolution

### 5.1 First-person (initial)

**Script:** `Assets/Scripts/SimpleFlyCamera.cs` (superseded for gameplay, still in repo)

- Rigidbody + capsule collider.
- WASD, mouse look, jump, voxel build (LMB place, RMB remove).
- Later improvements retained in TPS controller:
  - **Ground-only jump** (not air jump).
  - **Wall sliding** when moving diagonally into blocks (project movement onto wall plane).
  - Zero-friction physics materials on player and voxels.

### 5.2 Third-person (current)

**Script:** `Assets/Scripts/ThirdPersonController.cs`

- **Over-the-shoulder** camera (offset to one side, ~4 m back).
- **Camera-relative** movement (W = away from camera).
- Mouse controls camera yaw/pitch; character visual rotates toward move direction.
- Keeps: jump, wall slide, voxel building, Esc → menu.

**Camera rig structure:**

```
Player (physics)
  Character Visual (CapsuleRobotVisual)
Camera Rig (world-space)
  Camera Yaw Pivot
    Camera Pitch Pivot
      Main Camera + PenInkShadowEffect
```

---

## 6. Capsule robot player

**Scripts:**

- `Assets/Scripts/CapsuleRobotVisual.cs` — builds robot from primitives.
- `Assets/Scripts/JerseyInkUtility.cs` — procedural jersey textures.
- `Assets/Scripts/GameSession.cs` — team + random jersey number across scenes.

**Visual design (Barley-inspired, not a copy):**

- Capsule/sphere robot: head, eye, torso, hips, shoulders, arms, legs, feet.
- Neutral metal gray body (`CoreWar/VoxelFaceLit` shader).
- **Torn team jersey** (front + back panels) with **pen-and-ink crosshatch** shading.
- **Random jersey number (1–99)** on the back each match.
- Team color from menu selection (Red / Blue / Yellow / Green).

**Architecture:** Physics/collision on `Player`; visual model is a swappable child—ready for class variants later.

---

## 7. Lighting system evolution

Lighting went through several iterations based on playtesting and explicit user choices.

### 7.1 Initial

- Overhead directional light (later slight angle: pitch ~78°, yaw ~−45°).
- Ambient black → later raised for brighter grayscale look.
- Pen-and-ink post-process on camera for stylized shadows.

### 7.2 Voxel flood lighting (intermediate, later removed)

- BFS light propagation through air cells.
- Sun seeded from sky; bounce from lit top surfaces.
- Per-face brightness on voxels.
- Gradient ramp texture for smooth dark→bright transitions.
- **Problems addressed:** plus-shaped dark arms on ground near structures, interior faces lit because another face was bright, light leaking through diagonal voxel seams.

**Fixes applied during that phase:**

- Seed full open-air columns to sky (not just top layer).
- Strict diagonal seam occlusion for propagation (corner pinches block light).
- Per-face tinting instead of single block color.
- Reduced bounce; brighter ambient floor (~0.42).

### 7.3 Final: two-level sun lighting

**User decision:** Only two levels—**lit** (sees sun) vs **shadow** (does not). No per-face gradient from voxel BFS.

**Shader:** `Assets/Scripts/VoxelFaceLit.shader`

- Surfaces facing the sun and unoccluded → full brightness.
- Everything else → flat `_ShadowLevel` gray (~0.6).
- `noambient` so scene ambient doesn’t add a third level.
- Small facing threshold prevents shadow-map **edge speckle** (“light lips”) without heavy bias.

**Unity shadow map tuning:**

- Hard shadows, low bias, two-sided shadow casting on voxels.
- High shadow resolution, CloseFit projection.
- Voxel geometry overlap (`SealOverlap = 1.002`).

**Pen-and-ink post:** `PenInkShadowEffect.cs` + `PenInkShadowPost.shader` — kept for world stylization; tuned down so scenes aren’t overly dark.

### 7.4 Ground shadow shape

**User choice:** Footprint-only on open ground; crisp edges; Unity cast shadows handle outdoor ground; gradient only inside enclosed spaces.

- Removed plus-pattern darkening from BFS on open tiles.
- Narrowed seam sealing to true diagonal pinches only (not floor under walls).

### 7.5 Edge “lip” fix

Bright dotted line on top edge of shadowed side faces was shadow-map aliasing at grazing angles.

**Fix:** Scale direct sun by face orientation in `VoxelFaceLit`—faces angled away from sun get zero direct light regardless of shadow-map texel leaks.

**Later:** Sun yaw −45° so **two vertical faces** get sun equally; uniform direct strength per orientation (full top, fixed fraction on vertical sides).

---

## 8. Jump and movement fixes

| Issue | Fix |
|-------|-----|
| Stuck on ground / jump unreliable | Proper capsule ground cast; jump requires `_grounded` + head clearance |
| Too much friction against walls | Zero-friction materials; project wish direction onto wall normals |
| Air jumping | Removed; ground-only jump enforced |

---

## 9. Voxel building

- **LMB:** place voxel on grid (ray from camera center).
- **RMB:** remove only player-placed voxels.
- Snaps to grid via `VoxelLightingWorld.WorldToCell`.
- Build height and footprint constrained to playfield rules.

---

## 10. Key files reference

| File | Role |
|------|------|
| `MainMenuController.cs` | Team picker, play/quit |
| `GameSession.cs` | Selected team, random jersey number |
| `VoxelFieldBuilder.cs` | Field, light, player spawn |
| `VoxelLightingWorld.cs` | Grid occupancy, build/remove |
| `ThirdPersonController.cs` | TPS movement, camera, build |
| `CapsuleRobotVisual.cs` | Capsule robot mesh assembly |
| `JerseyInkUtility.cs` | Torn jersey + crosshatch + number texture |
| `VoxelFaceLit.shader` | Two-level voxel/robot lighting |
| `PenInkShadowEffect.cs` | Screen-space ink stylization |
| `PenInkShadowPost.shader` | Ink post-process pass |
| `PlayerBuiltVoxel.cs` | Marker for removable blocks |
| `SimpleFlyCamera.cs` | Legacy first-person controller |

---

## 11. GitHub

All work pushed to **main** on `https://github.com/leebuilds/CoreWar`.

- Commit `1c7eb34`: Unity voxel prototype (menu, FP building, lighting iterations).
- Later session work (TPS, robot, team picker) may need a follow-up commit if not yet pushed.

---

## 12. Design decisions log (user Q&A)

| Topic | Decision |
|-------|----------|
| Indirect lighting | Started with voxel flood; ended on **two-level sun only** for voxels |
| Dark enclosed areas | Near-black early → later **light gray** floor for darkest tier |
| Ink style | Strong crosshatch on jersey; world post-effect softened over time |
| Sun direction | Slightly angled; **−45° yaw** for two equally lit vertical faces |
| Light leaks at seams | Strict corner/edge seal + geometry overlap + shadow settings |
| Ground shadows | **Footprint only** on open ground; no plus-shaped BFS dark arms |
| Interior faces | Per-face → then **sun visibility only** (two levels) |
| TPS camera | Over-the-shoulder |
| Movement | Camera-relative |
| Robot body | Code-built capsules (prototype) |
| Team identity | **Torn jersey + random back number**; team picked at menu |
| First TPS milestone | Walk, jump, camera, visible robot (no shooting yet) |

---

## 13. Not yet implemented (natural next steps)

From design doc and chat direction:

- [ ] Basic **shooting / reticle / damage**
- [ ] **Class-specific** robot silhouettes (Sniper, Heavy, etc.)
- [ ] **Drills**, teams, objectives, scoring
- [ ] Third-person **voxel building** UX polish
- [ ] **MagicaVoxel** or imported model swap for robot visual
- [ ] Networking / 4v4 multiplayer
- [ ] Card/class system, traps, dynamic events

---

## 14. Controls (current)

**Main menu:** Click team → `PLAY AS [TEAM]` → Quit.

**Game scene:**

- **WASD** — move (camera-relative)
- **Mouse** — look (third-person)
- **Space** — jump (ground only)
- **LMB** — place voxel
- **RMB** — remove placed voxel
- **Esc** — return to menu

---

## 15. Session arc (timeline)

1. **Scaffold** Unity project + minimalist menu → flat voxel field.
2. **First-person** physics, jump, grid building.
3. **Pen-and-ink** shadows + overhead light; iterative leak fixes.
4. **Voxel flood lighting** + per-face tint; brightness and seam tuning.
5. **Two-level lighting** shader; remove BFS from final voxel look.
6. **Plus-shadow** and **interior face** bugs fixed via seeding + seam rules + per-face → sun-only.
7. **Sub-voxel corner leaks** — overlap geometry, two-sided shadows, bias, bounce seam checks.
8. **Edge lip** — face-angle scaling in shader; dual-face sun.
9. **TPS pivot** — over-shoulder camera, camera-relative move.
10. **Robot + team flow** — capsule robot, jersey ink, menu team pick, random number.
11. **Push to GitHub** — initial prototype commit on `main`.

---

*Generated from Cursor agent chat session, July 3, 2026.*
