# Chat Recap: Gunner, Universal Grenades, C4/Vest Explosions, and Ranger Fix

**Date:** July 9–10, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Grenades, flashbangs, and blindness session](2026-07-09-grenades-flashbang-and-blindness-session.md)

This session added the **Gunner** class and machine gun, made grenades a universal per-life
inventory for all kits, tuned machine-gun spread and suppression, improved grenade throw UX,
swapped frag VFX to fire, wired explosions to detonate C4 and explosive vests, and fixed
Ranger hold-breath ability feedback.

---

## 1. Universal grenade inventory

All classes now carry the same grenade loadout until respawn:

| Type | Per life |
|------|----------|
| Frag | **2** |
| Flashbang | **1** |

- Counts reset on respawn / character reset
- Grenade hotbar slot shows remaining count for the selected type
- Radial wheel labels include counts; throws are blocked when empty
- After the last grenade is thrown, slot stays on grenade for **0.5 s**, then switches back
  to the prior weapon and starts its draw timer (prevents instant firing on auto-switch)
- **1.8 s** hand cooldown before another grenade can be pulled out (was **3 s**)

**Files:** `ThirdPersonController.cs`, `GameplayHud.cs`

---

## 2. Gunner class (`gunner_1`)

### Card / kit

| Field | Value |
|-------|-------|
| Display name | Gunner |
| HP | **110** |
| Primary | Machine Gun |
| Secondary | Service Pistol |
| Tools | Build · Hammer |
| Unlock | Default for new and existing profiles (`ProfileSession` migration) |

**Files:** `CardCatalog.cs`, `CardKitDefinition.cs`, `ProfileSession.cs`

### Machine gun weapon

| Stat | Value |
|------|-------|
| RPM | **1500** (normal) · **3000** during E ability |
| Muzzle speed | **2000 m/s** |
| Mag / reserve start | **280 / 1400** (max total **1680**) |
| Reload | **10 s** full mag |
| Draw | **2.16 s** |
| Max body / head | **7 / 9** (velocity-scaled) |
| Air drag | ~**24%** loss per 100 m |
| Recoil | **0.6×** SMG scale |
| Crosshair | **24 px** radius circle; bullets sample inside with center bias |

**Spread tuning**

| Mode | Center-bias exponent | Feel |
|------|----------------------|------|
| Normal | **4.5** | Tighter to center |
| E ability active | **0.42** | Favors outer ring (less accurate) |

Spread sampler now allows exponents **below 1** (was clamped to minimum **1**, which blocked
edge-weighted ability spread).

**Files:** `ThirdPersonController.cs`, `WeaponAmmo.cs`, `ProjectileDamage.cs`, `GameplayHud.cs`,
`HotbarIconDrawer.cs`, `ProjectileBullet.cs`, `CardKitDefinition.cs`

### Machine gun suppression (on hit)

Applies to **players**, **shooting-range dummies**, and future AI targets:

| Effect | Normal | Gunner E boost active |
|--------|--------|------------------------|
| Duration | **1.5 s** | **1.5 s** |
| Move speed | **80%** | **65%** |
| Screen flick intensity | **1×** | **2.5×** |

- Speed penalty does **not** stack below the strongest active slow
- Flick uses `PlayerBulletHitFlash.FlickFromGunshot(intensityScale)`

**New file:** `MachineGunSuppressionUtility.cs`  
**Also touched:** `ProjectileBullet.cs`, `ShootingRangeDummy.cs`, `PlayerBulletHitFlash.cs`

### Gunner E ability — Suppression Boost

| Stat | Value |
|------|-------|
| Activation | Tap **E** |
| Duration | **7 s** max |
| Cooldown | **30 s** (starts on activation) |
| RPM | **3000** |
| Crosshair radius | **+20%** |
| Spread | Edge-weighted (less accurate) |
| Suppression on hit | Enhanced (65% speed, 2.5× flick) |
| Early end | When machine gun mag hits **0** |

**Files:** `ThirdPersonController.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`

---

## 3. Frag grenade VFX

Frag detonations now use a short **layered fireball** (orange / yellow / white-hot core) instead
of gray smoke. Same visual language as anti-material explosions, scaled to frag size (**5 m**
diameter, **0.5 s** lifetime).

**File:** `FragGrenadeSmokeEffect.cs` (class name unchanged)

---

## 4. C4 and explosive vest — explosion interactions

### C4 damage threshold (30 body damage)

Existing rule: **30** accumulated **body** damage detonates a charge (headshots ignored).

Extended to all major damage sources, not just direct bullet hits:

| Source | Wired via |
|--------|-----------|
| Gunfire / anti-material direct hit | `DetonateFromBullet` (existing) |
| Frag grenade blast | `C4ChargeProjectile.ApplyBlastDamage` + LOS via wearer root |
| All fiery explosions | `ExplosionBlastUtility` |
| Laser sword melee | `ApplyChargesInRange` |

**C4 on wearer fix:** blast line-of-sight now uses the **stick target** (player/dummy) as the
LOS root when attached, so grenades/explosions on your own body correctly damage attached C4.

**Files:** `C4ChargeProjectile.cs`, `GrenadeBlastUtility.cs`, `ExplosionBlastUtility.cs`,
`ThirdPersonController.cs`

### Explosive vest — explosion detonation

Any **explosion** in range now detonates equipped vests (not bullets):

- Frag grenades (`GrenadeBlastUtility`)
- Shared explosions (`ExplosionBlastUtility` — C4, anti-material, vest chain, etc.)

Vest wearer is killed on vest blast; bullets still only detonate vest **on death**, not on direct hit.

**Files:** `ExplosiveVestState.cs` (`DetonateEquippedInRadius`), `ExplosionBlastUtility.cs`,
`GrenadeBlastUtility.cs`  
**Compile fix:** added `using System.Collections.Generic;` for `HashSet<>` in `ExplosiveVestState.cs`

---

## 5. Ranger hold breath fix (`infantry_2`)

### Symptoms

Hold breath appeared to have **no duration** — ability feedback looked broken.

### Causes

1. **HUD overlay inverted** — active breath used `1 - remaining/max`, which is **0** at start.
   Overlay only renders above `0.001` fill, so the slot looked empty while the ability was active.
   Other abilities use `remaining/max` (depleting bar).
2. **Split activation/update** — activation ran in `HandleAbilityInput` at end of `Update` while
   duration ran in `UpdateHoldBreathState` at the beginning of the next frame.
3. **Hold-to-maintain** — ability requires **holding E** (up to **4 s**); tapping E ends on the
   next frame.

### Fix

- Consolidated all hold-breath logic into `UpdateHoldBreathState` (activate + tick + end)
- Corrected HUD overlay to deplete like other abilities

**Unchanged behavior:** **4 s** max while holding E, **14 s** cooldown, scoped AR recoil/sway
benefits while active.

**File:** `ThirdPersonController.cs`

---

## 6. File summary

| Area | Files |
|------|-------|
| Gunner class / unlock | `CardCatalog.cs`, `CardKitDefinition.cs`, `ProfileSession.cs` |
| Machine gun combat | `ThirdPersonController.cs`, `WeaponAmmo.cs`, `ProjectileDamage.cs`, `ProjectileBullet.cs` |
| Suppression | `MachineGunSuppressionUtility.cs`, `ShootingRangeDummy.cs`, `PlayerBulletHitFlash.cs` |
| HUD / icons | `GameplayHud.cs`, `HotbarIconDrawer.cs`, `MenuUiSounds.cs` |
| Grenades | `ThirdPersonController.cs`, `GameplayHud.cs`, `FragGrenadeSmokeEffect.cs` |
| C4 / explosions / vest | `C4ChargeProjectile.cs`, `GrenadeBlastUtility.cs`, `ExplosionBlastUtility.cs`, `ExplosiveVestState.cs` |
| Ranger fix | `ThirdPersonController.cs` |

---

## 7. README updates

`README.md` updated for: universal grenade inventory and timings, frag fire VFX, Gunner kit
and E ability, machine gun stats/suppression/crosshair, C4/vest explosion rules, default
unlocks including `gunner_1`, and new script references.
