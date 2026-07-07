# Chat Recap: Sniper Unlocks, Ballistics, and Abilities

**Date:** July 7, 2026 (afternoon session)  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Ballistics, Hotbar, and Player Damage Session](2026-07-07-ballistics-hotbar-and-player-damage-session.md)

This session restricted starter card unlocks, shipped tier-1 Infantry and Sniper
kits with distinct weapons, built full sniper ADS/scope gameplay, redesigned the
hotbar around an **E** ability slot, and replaced the global damage model with
per-weapon velocity scaling, air drag, and surface impact tuning.

---

## 1. Collection unlocks and profile migration

| Rule | Detail |
|------|--------|
| New accounts | Only **Infantry (tier 1)** and **Sniper (tier 1)** unlocked |
| Existing profiles | Migrated on **sign-in** or **session restore** (`profileDataVersion` → 1) |
| Locked cards | Show LOCK overlay in Decks |
| Invalid loadout slots | Cleared if they reference cards no longer owned |

**Files:** `LocalProfileRepository.cs`, `ProfileSession.cs`, `PlayerProfile.cs`, `CardCatalog.cs`

---

## 2. Tier 1 class kits

| Card | Hotbar (`1` / `2` / `F` / `H`) |
|------|--------------------------------|
| **Infantry** | AR · Pistol · Build · Hammer |
| **Sniper** | Sniper · Pistol · Build · Hammer |

- Sniper secondary is **Service Pistol** (same as Infantry).
- `CardCatalog.ResolveKit()` returns the correct kit per specialty.
- Other cards still exist in the catalog but remain locked.

**Files:** `CardKitDefinition.cs`, `CardCatalog.cs`, `GameSession.cs`

---

## 3. Sniper rifle — ADS, scopes, and feel

### Controls

| Input | Behavior |
|-------|----------|
| **Right click** | ADS (aim down sights) |
| **E** (ability) | Cycle magnification while ADS — **Iron → 4× → 10×** (no cooldown) |
| **Scroll wheel** | Cycles equippable hotbar slots only (not magnification) |

### Reticles

| Mode | Reticle |
|------|---------|
| Hip fire | Wide crosshair (2× previous gap/length); bullets spread between inner tips with center-weighted randomness |
| Iron sights ADS | Standard crosshair; very fast ADS transition |
| 4× / 10× ADS | Red dot; smooth scope vignette post-effect |

### Scope overlay (`SniperScopePostEffect.cs`)

- Magnified scopes (4×, 10×): circular bright center (~⅓ screen diameter, later widened ~200%), dark blurred band inside ring, lighter blurred corners outside (no solid black disc).
- Iron sights: unchanged — no scope ring overlay.
- Magnification label in red on a panel in the darkened band above the red dot.
- Higher magnification → darker inner band, stronger outer blur.
- Scope swap: brief hip-fire dip → re-ADS transition when pressing **E**.

### ADS timing and recoil

| Scope | ADS transition |
|-------|----------------|
| Iron sights | Very fast (~0.12 s) |
| 4× | ~0.38 s |
| 10× | ~0.58 s |

- Sniper recoil is **2×** rifle recoil.
- Can fire during ADS transition (inaccurate).
- Small scope sway while moving.
- Default magnification on match start / respawn / first ADS: **4×** (index 1).
- Magnification persists for the life until respawn or match restart.

### Bugs fixed

- Super zoom on first sniper equip (`_sniperFovTransitionTarget` initialized to hip FOV).
- Scroll bleed on first ADS changing magnification (scroll no longer controls scopes).
- Magnification resetting when releasing ADS (removed; only resets on respawn).

**Files:** `ThirdPersonController.cs`, `SniperScopePostEffect.cs`, `SniperScopePost.shader`, `VoxelFieldBuilder.cs`

---

## 4. Mouse sensitivity settings

Split into two sliders in hub and pause **Settings**:

| Setting | Behavior |
|---------|----------|
| **Look sensitivity** | Hip-fire and general camera look |
| **ADS sensitivity** | Base ADS look; further scaled by scope FOV ratio (more zoom → less sensitivity) |

Stored in `settings.json` as `lookSensitivity` and `adsSensitivity`.

**Files:** `MenuSettings.cs`, `MenuSettingsPanel.cs`, `ThirdPersonController.cs`

---

## 5. Hotbar redesign

Layout moved to **lower-left**, slots ~**50%** previous size (36 px), grouped:

```
[E ability]  |  [1 primary] [2 secondary]  |  [F build] [H hammer]
```

| Slot | Notes |
|------|-------|
| **E** | Ability — not equippable via scroll; shows cooldown overlay (dark = on cooldown, bright = ready) |
| **1 / 2 / F / H** | Equippable via scroll and direct keys |

### E abilities (tier 1)

| Card | Ability | Cooldown |
|------|---------|----------|
| **Sniper** | Cycle scope while ADS | 0 s |
| **Infantry** | Speed boost (1.35× move, 10 s) | 30 s |

- Respawn selects **hotbar slot 1** (primary weapon).

**Files:** `ThirdPersonController.cs`

---

## 6. Per-weapon velocity damage

Replaced the old global `40 × (speed / muzzle)` model.

### Max damage at muzzle velocity

| Weapon | Body | Headshot |
|--------|------|----------|
| Sniper | 60 | 130 |
| Pistol | 15 | 30 |
| AR | 17 | 22 |

### Damage formula

```
damage = maxDamage × Lerp(0.5, 1.0, impactSpeed / muzzleSpeed)
```

- **50%** of max damage at **0 m/s** impact speed.
- **100%** at muzzle velocity.
- Headshot uses the headshot max column (not a separate multiplier on body damage).
- Player blindness duration math unchanged (still scales from damage % of max HP).

### Muzzle velocities (m/s)

| Weapon | Muzzle speed |
|--------|--------------|
| Pistol | 325 |
| AR | 850 |
| Sniper | 950 |

### Air drag (exponential per 100 m)

| Weapon | Speed retained after 100 m |
|--------|---------------------------|
| Pistol | ~80% (~20% loss) |
| AR | ~90% (~10% loss) |
| Sniper | ~98% (~2% loss) |

Applied during flight-ray integration in `ProjectileBullet`.

### Surface impacts

- Floor, map geometry, player builds, and bounces: **~50%** speed retained (`SurfaceImpactSpeedRetention = 0.5`).

### Player penetration threshold

- Below **30 m/s**: bullet **bounces off players** (no damage, not destroyed).
- At **≥ 30 m/s**: normal velocity-scaled damage applies.
- Shooting range **dummies** still use the damage formula at all speeds (no 30 m/s bounce).

**Files:** `ProjectileDamage.cs`, `ProjectileBullet.cs`, `ShootingRangeDummy.cs`, `ThirdPersonController.cs`

---

## 7. Files added / changed

| Area | Files |
|------|-------|
| Profiles / unlocks | `LocalProfileRepository.cs`, `ProfileSession.cs`, `PlayerProfile.cs` |
| Cards / kits | `CardCatalog.cs`, `CardKitDefinition.cs` |
| Sniper gameplay | `ThirdPersonController.cs`, `SniperScopePostEffect.cs`, `SniperScopePost.shader`, `VoxelFieldBuilder.cs` |
| Settings | `MenuSettings.cs`, `MenuSettingsPanel.cs` |
| Ballistics / damage | `ProjectileDamage.cs`, `ProjectileBullet.cs`, `ShootingRangeDummy.cs` |
| Docs | `README.md`, this recap |

---

## 8. Manual test plan

- [ ] New account: only `infantry_1` + `sniper_1` owned; other cards show LOCK
- [ ] Existing profile: sign out/in migrates to two starter cards; invalid loadout slots cleared
- [ ] Infantry kit: AR · Pistol · Build · Hammer
- [ ] Sniper kit: Sniper · Pistol · Build · Hammer
- [ ] Sniper hip fire: wide crosshair, center-weighted spread between inner tips
- [ ] Sniper ADS: iron = crosshair (fast); 4×/10× = red dot + vignette scope overlay
- [ ] **E** while ADS cycles Iron → 4× → 10× with hip dip transition; magnification persists until respawn
- [ ] First sniper equip / first ADS: no super-zoom glitch; default **4×**
- [ ] Settings: separate Look and ADS sensitivity; higher zoom = slower ADS look
- [ ] Hotbar lower-left, ~half size; **E** ability slot with cooldown overlay; scroll only equippable slots
- [ ] Infantry **E**: 10 s speed boost, 30 s cooldown
- [ ] Respawn lands on hotbar slot **1**
- [ ] Damage at muzzle: Sniper 60/130, Pistol 15/30, AR 17/22 (body/head)
- [ ] Damage at 0 speed: 50% of max for all weapons
- [ ] Air drag: pistol loses ~20%/100 m, AR ~10%, sniper ~2%
- [ ] Surface hit: ~50% speed loss on floor/map/builds/bounces
- [ ] Player hit &lt; 30 m/s: bounce; ≥ 30 m/s: damage + bullet destroyed
- [ ] Dummy hits: velocity-scaled damage at all speeds

---

## 9. Not in scope / follow-ups

- Other class specialties have no **E** ability yet.
- README and older session recaps referenced the pre-session damage model (updated in this push).
- Card catalog `moveSpeed` and other preview stats are only partially wired to gameplay.
