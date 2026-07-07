# Chat Recap: Ballistics, Hotbar, and Player Damage

**Date:** July 7, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Shooting Range Solo Mode and Layout Polish Session](2026-07-05-shooting-range-mode-session.md)

This session polished the shooting range layout, replaced kinematic bullets with
physics balls, added velocity-based damage and player blindness, expanded the
hotbar to four slots (AR, pistol, build, hammer), and tuned bounce/friction
feel.

---

## 1. Shooting range layout fixes

| Issue | Fix |
|-------|-----|
| Distance signs read backwards | Rotate sign `TextMesh` 180° on Y so labels face the firing line |
| Dummies didn't flash red on hit | Red hit flash via `VoxelMaterialUtility` texture swap |
| Fence only half the lane width | Span full 48 m from wall to wall; posts evenly spaced |
| Floor grid misaligned with build grid | Use centered grid origin (same as standard arena); floor panel positioned from cell corners |

**Files:** `ShootingRangeBuilder.cs`, `ShootingRangeDummy.cs`, `ShootingRangeTerrain.cs`, `VoxelFieldBuilder.cs`

---

## 2. Physics bullets

Replaced raycast-only bullets that stopped on impact with **hybrid ballistics**:

| Phase | Behavior |
|-------|----------|
| **Flight** | Raycast integration at muzzle speed (avoids tunneling); real `Physics.gravity` |
| **Player builds** | Penetrate all player-built pieces; speed loss scales with material thickness crossed (`exp(-0.7 × meters)`) |
| **Map geometry** | Convert to `Rigidbody` + `SphereCollider` (10 g); bounce and roll via PhysX |
| **Lifetime** | Max **35** live bullets globally (oldest destroyed); no despawn timer |
| **Visuals** | Two-tone sphere texture for visible spin; renderer disabled beyond **50 m** (object + physics persist) |

**Removed:** bullet-hole decals (all modes).

**Files:** `ProjectileBullet.cs`, `ShootingRangeSession.cs`, `ThirdPersonController.cs`

---

## 3. Velocity-based damage

Shared tuning in `ProjectileDamage.cs`:

| Rule | Value |
|------|-------|
| Point-blank body damage | `(impactSpeed / muzzleSpeed) × 40` |
| Headshot multiplier | **2×** damage |
| Human penetration threshold | **≥ 25 m/s** — below this, bullets bounce off players/dummies |
| Dummy soak | Bullet destroyed on penetrating hit; rolling hits below threshold bounce |

Player headshots detected by hit height ≥ 1.35 m on the player capsule (local Y).

**Files:** `ProjectileDamage.cs`, `ProjectileBullet.cs`, `ShootingRangeDummy.cs`, `PlayerHealth.cs`

---

## 4. Player blindness (hit flash)

Full-screen overlay when the local player is struck (`PlayerBulletHitFlash.cs`):

| Phase | Visual |
|-------|--------|
| Fade in (~0.04 s) | Red flash → pitch black |
| Hold | Full black for most of the blind duration |
| Fade out | Black lifts through red; fade-out length scales with blind duration |

**Blindness duration** from damage as % of max HP (`ProjectileDamage.ComputeBlindnessDuration`):

- Below 50% HP damage: linear to zero
- 50% HP → **0.125 s** (⅛ second)
- 99% HP → **2 s** (log curve, exponent 2.2)
- Headshots **2×** duration
- **Re-hit while blind:** no flash; timer resets to new duration; stay at full black

`PlayerHealth` reads max HP from the active card's preview stats. Death/respawn not wired yet.

**Debug:** Pause menu → **Test Damage** (`PlayerDamageDebugPanel.cs`) — slider 1–99 damage, body/headshot toggle, fixed 100 HP pool, instant refill after each test.

**Files:** `PlayerBulletHitFlash.cs`, `PlayerHealth.cs`, `PlayerDamageDebugPanel.cs`, `GamePauseMenu.cs`, `VoxelFieldBuilder.cs`

---

## 5. Four-slot hotbar

| Key | Tool | Notes |
|-----|------|-------|
| `1` | AR | Full auto ~400 RPM; 155 m/s muzzle velocity |
| `2` | Pistol | Semi-auto; 95 m/s |
| `F` | Build | Blueprint / build mode |
| `H` | Hammer | Destroy player builds |

- Two groups separated by **20 px** (weapons `1`/`2`, tools `F`/`H`)
- Key label in upper-left corner of each slot
- Mouse wheel cycles all four slots
- Separate held visuals for AR (longer rifle mesh) and pistol

**Files:** `CardKitDefinition.cs`, `ThirdPersonController.cs`

---

## 6. Bounce and friction tuning

Iterative tuning across the session:

| Setting | Final-ish value | Notes |
|---------|-----------------|-------|
| Bounciness | 0.72 | Higher rebound off surfaces |
| Tangential retention on bounce | 100% | Capped (was 55%/75% originally) |
| Bullet `linearDamping` / `angularDamping` | 0.9 / 1.6 | Less rolling distance |
| Bullet surface friction | 1.6 | `Maximum` combine wins over slippery floor |
| Floor material (`VoxelFloorGrip`) | ~2.0 friction | Floor panels only; player still slides (`Minimum` combine on player) |

Walls/backstop/fence keep slippery `VoxelSlide` material; range floor and arena floor voxels use grippy floor material.

**Files:** `ProjectileBullet.cs`, `VoxelFieldBuilder.cs`, `ShootingRangeTerrain.cs`

---

## 7. Files added / changed

| Area | Files |
|------|-------|
| Range layout | `ShootingRangeBuilder.cs`, `ShootingRangeDummy.cs`, `ShootingRangeTerrain.cs`, `VoxelFieldBuilder.cs` |
| Ballistics | `ProjectileBullet.cs`, `ProjectileDamage.cs`, `ShootingRangeSession.cs` |
| Player damage | `PlayerHealth.cs`, `PlayerBulletHitFlash.cs`, `PlayerDamageDebugPanel.cs` |
| Hotbar / weapons | `CardKitDefinition.cs`, `ThirdPersonController.cs` |
| Pause menu | `GamePauseMenu.cs` |
| Docs | `README.md`, this recap |

---

## 8. Manual test plan

- [ ] Shooting range: distance signs readable from firing line; fence spans full width; build grid aligns with floor
- [ ] Dummies flash red on hit; ding / brighter headshot ding
- [ ] AR holds fire at ~400 RPM; pistol semi-auto; hotbar shows `1` `2` `F` `H` with 20 px group gap
- [ ] Bullets pass through player builds, bounce/roll on map, max 35 on ground
- [ ] Hits ≥ 25 m/s penetrate humans and deal velocity-scaled damage; slower hits bounce
- [ ] Player hit: red→black blindness; 50 damage ≈ 0.125 s blind; 99 damage ≈ 2 s; headshot doubles
- [ ] Re-hit while blind: no flash, timer resets
- [ ] Pause → **Test Damage**: inflict 1–99 body/headshot, screen flash, HP refills
- [ ] Landed bullets stop rolling sooner on grippy floor; player movement still slippery
- [ ] **Reset Map** clears bullets; no bullet holes
