# Chat Recap: Shooting Range Solo Mode and Layout Polish

**Date:** July 5, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Decks Collection Layout and Card Catalog Session](2026-07-05-decks-layout-and-card-catalog-session.md)

This session added a **Shooting Range** solo game mode for testing cards and weapons,
then iterated on range layout: a firing-line fence at 0 m, a 10 m walk zone behind it,
fan-spread tan dummies with front-facing distance signs, and merged terrain for
performance.

---

## 1. Shooting Range game mode

### Goals

- Solo practice mode with instant spawn (no match prep)
- Long lane (1 voxel = 1 meter) with humanoid targets at fixed distances
- Full hotbar, match clock, Red team — no persistent stats saved
- Pause menu tools to swap character, tune dummy HP, and reset the map

### Menu and matchmaking

| Behavior | Implementation |
|----------|----------------|
| Mode ID | `shooting_range` in `GameModeDefinition.cs` |
| Playability | Requires **loadout slot 1** only (`LoadoutRequirement.Slot1`) |
| Instant match | `skipMatchmakingDelay` + `skipPrepPhase`; `LocalSimMatchmakingBackend` completes immediately |
| Hub PLAY | Always enabled; per-mode padlocks on the game modes list |
| Entry path | `MenuNavigator` calls `BeginMatch` + `EnterGame()` directly (skips prep overlay) |

**Locked modes (unchanged):**

- **TEST ONE PLAYER** — requires both loadout slots
- **TEST TWO PLAYER** — locked until online play

### Session state (`ShootingRangeSession.cs`)

- Dummy HP default 100; adjustable 10–1000 via logarithmic slider
- Projectile entity pool capped at **100** (oldest bullet/hole removed when exceeded)
- `ResetMap` clears bullets, player-built voxels, refills dummies, teleports player to spawn
- Not persisted to profile

---

## 2. Range map and coordinates

### Grid and terrain

- Standard arena: 32×32 voxels; shooting range: **48×680** voxels (`VoxelFieldBuilder.cs`)
- Grid origin Z shifted to **−10 m** so world Z = 0 aligns with the firing line
- **Merged terrain panels** (`ShootingRangeTerrain.cs`) replace ~37k individual floor/wall cubes:
  - Floor, left/right side walls, backstop at ~620 m
  - Firing-line fence at Z = 0 (lower panel + upper rail + posts)
- `VoxelLightingWorld.RegisterOccupiedCell()` tracks occupancy without per-voxel GameObjects

### Firing line and walk zone

| Constant | Value | Meaning |
|----------|-------|---------|
| `FiringLineWorldZ` | 0 | 0 m line — fence you shoot over/behind |
| `BehindZoneDepthMeters` | 10 | Walkable area from Z = −10 to Z = 0 |
| `PlayerSpawnPosition` | (0, 1.1, −5) | Center of the behind zone |

Distances on signs are measured **from the fence forward** (10 m dummy at world Z = 10, etc.).

### Target layout (`ShootingRangeBuilder.cs`)

Eight dummies at **10, 50, 100, 200, 300, 400, 500, 600 m** — not in a straight row:

| Distance | Horizontal position |
|----------|---------------------|
| 10 m (closest) | **Right** (+16 m X) |
| 600 m (furthest) | **Left** (−16 m X) |
| Middle distances | Interpolated between right and left |

- **No wooden stands** — tan humanoid dummies only (`CapsuleRobotVisual.BuildNeutralDummy()`)
- Dummies rotated 180° on Y so they face the shooter
- Red distance sign on the **front** of each dummy (chest, facing the firing line)

---

## 3. Combat and dummies

### Hit zones (`ShootingRangeDummy.cs`, `ShootingRangeHitZone.cs`)

| Zone | Damage |
|------|--------|
| Body | 30 |
| Head | 60 |

- **Ding** SFX on hit; brighter ding on headshot (`MenuUiSounds.PlayRangeDing`)
- Brief hit flash via `VoxelMaterialUtility` texture swap on `_MainTex` (VoxelFaceLit has no `_Color`)
- Dummy falls when HP reaches 0; **auto-respawns after 3 seconds**

### Bullets (`ProjectileBullet.cs`)

- In range mode, bullets and bullet holes **persist** until Reset Map or pool eviction
- Hits register through dummy hit zones instead of standard player damage

---

## 4. Pause menu (range-specific)

Opened with **Esc** in shooting range (`GamePauseMenu.cs`):

| Button | Action |
|--------|--------|
| **Choose Character** | Owned cards overlay (`ShootingRangeCharacterPicker` + shared `DecksCollectionView`); swaps active card and teleports to firing-line spawn |
| **Dummy Stats** | Logarithmic HP slider 10–1000 (`ShootingRangeDummyStatsPanel`); applies to all dummies |
| **Reset Map** | Clears bullets/holes, player builds, refills dummies, resets player position |
| Settings / Exit Match | Same as standard modes |

---

## 5. Bugs fixed during implementation

| Issue | Fix |
|-------|-----|
| `ShootingRangeBuilder` compile error | Pass `standRoot.transform` (not GameObject) to `ShootingRangeDummy.Create` |
| `UnityAction` mismatch on Hide callbacks | Wrap as `() => Hide()` in character picker and dummy stats panel |
| Console spam: `_Color` on VoxelFaceLit | `VoxelMaterialUtility` solid-color textures on `_MainTex` |
| Severe lag (~37k voxel cubes) | Merged terrain panels + occupancy registration without per-cell objects |

---

## 6. Files added / changed

| Area | Files |
|------|-------|
| Mode & flow | `GameModeDefinition.cs`, `LocalSimMatchmakingBackend.cs`, `MenuNavigator.cs`, `GameSession.cs`, `PlayerProfile.cs`, `ProfileSession.cs` |
| Range core | `ShootingRangeSession.cs`, `ShootingRangeDummy.cs`, `ShootingRangeHitZone.cs`, `ShootingRangeBuilder.cs`, `ShootingRangeTerrain.cs` |
| World | `VoxelFieldBuilder.cs`, `VoxelLightingWorld.cs`, `VoxelMaterialUtility.cs` |
| Combat | `ProjectileBullet.cs`, `ThirdPersonController.cs`, `CapsuleRobotVisual.cs` |
| UI | `GamePauseMenu.cs`, `ShootingRangeCharacterPicker.cs`, `ShootingRangeDummyStatsPanel.cs`, `DecksCollectionView.cs`, `MenuUiSounds.cs`, `MenuUiFactory.cs` |
| Docs | `README.md`, this recap |

---

## 7. Manual test plan

- [ ] Hub **PLAY** → **SHOOTING RANGE** (requires loadout slot 1; padlock if empty)
- [ ] Matchmaking completes instantly; no prep overlay; spawn behind fence at ~Z = −5
- [ ] Fence visible at Z = 0; can walk ~10 m behind it (Z = −10 to 0)
- [ ] Eight **tan** dummies: 10 m on the right, 600 m on the left, others spread between
- [ ] Distance signs on dummy chests, readable from firing line
- [ ] Body/head hits ding; headshot ding brighter; dummy drops and respawns after ~3 s
- [ ] Bullets and holes persist; **Reset Map** clears them
- [ ] **Dummy Stats** slider changes max HP (log scale 10–1000)
- [ ] **Choose Character** swaps owned card and returns to spawn
- [ ] No stats written to profile after exiting range
- [ ] Unity console — no compile errors; acceptable frame rate (merged terrain)
