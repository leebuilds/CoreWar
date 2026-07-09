# Chat Recap: Anti-Material Sniper, Scope Sway, Cyborg, and Infantry Ability

**Date:** July 9, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Hunter, Ranger, Skirmisher, Heavy Session](2026-07-09-hunter-ranger-skirmisher-heavy-session.md)

This session implemented the **Anti-Material** legendary sniper card (`sniper_3`), the **Cyborg**
heavy card (`heavy_2`), a full **ADS scope sway** system for precision weapons, explosion
blindness/fire VFX, the **Brace** stabilizer ability, crosshair-accurate firing, and a reworked
**Infantry** speed-boost ability.

---

## 1. New playable cards and unlocks

Default owned cards expanded (profile migrations):

| Card ID | Display name | Primary | Secondary | E ability |
|---------|--------------|---------|-----------|-----------|
| `sniper_3` | Anti-Material | Anti-Material Rifle | Pistol | Brace (stabilizer pivot) |
| `heavy_2` | Cyborg | Laser LMG | Laser Sword | Regen boost |

**Files:** `CardCatalog.cs`, `CardKitDefinition.cs`, `ProfileSession.cs`

---

## 2. Anti-Material rifle (`sniper_3`)

### Primary weapon stats

| Stat | Value |
|------|-------|
| Draw | **2 s** |
| ADS transition | **1.05 s** (30% faster than initial 1.5 s spec) |
| Magnification | **12×** |
| Hipfire | **Disabled** — must be fully ADS to fire |
| Charge time | **1 s** before shot (large charge sound + animation) |
| Charge sound | **Blocked when magazine is empty** |
| Muzzle velocity | **1300 m/s** |
| Air drag | **3%** loss per 100 m (97% retention) |
| Direct hit damage | **90** body / **102** head at full velocity |
| Recoil | **~2×** standard sniper (effective scale **5.67**) |
| Ammo | **40 / 1** reserve / mag (45 max total) |
| Reload | **8 s** unbraced · **6 s** braced |
| Movement | **50%** when held · **20%** when ADS · **20%** when firing |
| ADS while reloading | **Blocked** (also blocked for hunting rifle) |

### Explosive round behavior

- On impact, round **sticks** for **2 s**, then detonates
- **10 m** blast radius — exponential falloff: **10** damage at edge, **100** at center
- **4.5 m** build destruction radius
- Sticks to players/dummies; **visual hidden** while attached to a living entity
- If target dies before detonation, projectile **drops to ground** at impact point and becomes visible again
- Projectile visual size matches standard bullets (**0.0275 m** radius)

### Explosion VFX and blindness

**New file:** `AntiMaterialExplosionEffect.cs`

| Effect | Detail |
|--------|--------|
| Fireball | **1 s** duration, **8 m** diameter, layered orange/yellow spheres |
| Explosion blindness | **2×** normal blindness multiplier from blast damage |
| Fiery phase | Orange/red overlay on **opaque black** backdrop while in fire radius |
| Fiery persistence | Continues even if player exits fire radius once started |
| Post-fire blindness | Transitions to pure black; **3×** potency vs. damage-based duration |
| Minimum post-fire black | **≥ 1 s** after fiery phase ends |
| Max total blindness | **7 s** cap |
| Red dim fade | Standard red fade-out when blindness fully ends |
| Regen block | Health regen **paused while blind** |
| Input block while blind | Reload, fire, ADS, hotbar, abilities, building |

**Files:** `AntiMaterialProjectile.cs`, `PlayerBulletHitFlash.cs`, `PlayerHealth.cs`, `ShootingRangeSession.cs`

---

## 3. Brace ability (E) — Anti-Material

Toggle with **E** (45 s cooldown):

| Feature | Behavior |
|---------|----------|
| Activation | Requires **mostly horizontal** aim (cannot brace looking steeply up/down) |
| Anchor | **0.5 m** ahead at player's **current height** — locks gun where you were looking |
| Movement | **A / D** orbit clockwise/counterclockwise around anchor only |
| Aiming | Crosshair always faces anchor; mouse tilts view within a small window (view follows gun) |
| Vertical mouse range | **40%** reduced while braced |
| ADS | Works while braced; mouse aim window applies in and out of scope |
| Recoil | **80%** less (`0.2×`) |
| ADS speed | **50%** faster (`0.5×` transition time) |
| Reload | **6 s** (vs. 8 s unbraced); **no reload gun dip** animation |
| Scope sway amplitude | **0.88×** sniper baseline (10% more than prior braced value) |
| Scope sway speed | **40%** of base anti-material speed |

**Files:** `ThirdPersonController.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`, `CardCatalog.cs`

---

## 4. ADS scope sway system

Smooth, semi-random scope sway while ADS for all precision weapons. Sway is applied as
camera rotation; bullets fire through **screen-center crosshair** (see §6).

### Per-weapon sway amplitude (vs. sniper baseline = 1.0×)

| Weapon | Sway amplitude | Sway speed |
|--------|----------------|------------|
| Sniper | 1.0× | 40% |
| Hunting rifle | 0.8× | 40% |
| Anti-Material (unbraced) | **6.0×** | 70% |
| Anti-Material (braced) | **0.88×** | 40% |
| Ranger scoped AR | 0.7× | 70% |
| Ranger + Hold Breath | 0.7× | **20%** |

Global modifiers: **1.3×** range · **⅓×** speed (200% slower motion)

### Variable sway envelope

Sway cycles through weighted random phases with smooth transitions:

- **Wide long** — large slow arcs (most common)
- **Still** — nearly motionless
- **Light** — moderate movement
- **Slow drift** — medium amplitude, very slow
- **Heavy/fast** — rare; blocked from chaining back-to-back

Fast phases reduce Perlin noise (less mid-sway direction change) and use longer smoothing.

**File:** `ThirdPersonController.cs` (`UpdateSniperScopeSway`, envelope helpers)

---

## 5. Ranger scoped AR tuning

| Change | Value |
|--------|-------|
| ADS recoil | **50%** less (`0.5×`) while scoped |
| Hold breath recoil | **0.13×** (30% more than prior `0.1×`) |
| Hold breath sway speed | **20%** of ranger base while ability active |

---

## 6. Crosshair-accurate firing

Bullets now fire along the **screen-center reticle ray**, not raw yaw/pitch pivots.

- `BuildCrosshairAimRay()` syncs camera transform (sway + visual recoil) then casts through viewport center `(0.5, 0.5)`
- Sniper spread offsets sample from screen center
- Applies to all firearms including anti-material charge shot

**File:** `ThirdPersonController.cs`

---

## 7. Infantry ability rework (`infantry_1`)

**10 s** boost · **30 s** cooldown:

| Effect | Value |
|--------|-------|
| Move speed | **+15%** (`1.15×`) |
| Reload | **20%** faster (`0.8×` duration) |
| Weapon pullout | **20%** faster (`0.8×` draw time) |
| Recoil | **15%** less (`0.85×`) |

**Files:** `ThirdPersonController.cs`, `CardCatalog.cs`

---

## 8. Cyborg (`heavy_2`) — summary

Implemented in the same development stretch (see git diff for full detail):

| Feature | Detail |
|---------|--------|
| Primary | Overheating laser LMG — no bullet drop/drag, **120 m** range, red beam |
| Secondary | Laser sword melee swing |
| Passive | **+15%** max HP, health regen after avoiding damage |
| E ability | **6 s** regen boost at **20% HP/s** · **35 s** cooldown |
| HUD | Laser heat / overheat display |

**Files:** `ThirdPersonController.cs`, `ProjectileBullet.cs`, `ProjectileDamage.cs`, `PlayerHealth.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`, `CardKitDefinition.cs`

---

## 9. New and modified files

| File | Role |
|------|------|
| `AntiMaterialProjectile.cs` | Sticky explosive round, fuse, blast damage, entity stick/drop |
| `AntiMaterialExplosionEffect.cs` | 1 s fireball VFX (8 m diameter) |
| `ThirdPersonController.cs` | Anti-material weapons, Brace, sway, crosshair rays, infantry boost, cyborg |
| `PlayerBulletHitFlash.cs` | Fiery + black blindness phases, caps, regen/input coupling |
| `PlayerHealth.cs` | Blindness-aware damage, regen pause while blind |
| `ProjectileDamage.cs` | Anti-material + cyborg laser damage/drag |
| `WeaponAmmo.cs` | Anti-material ammo defaults (8 s reload base) |
| `MenuUiSounds.cs` | Anti-material charge audio (no play on empty mag) |
| `GameplayHud.cs` | Brace + cyborg HUD elements |
| `HotbarIconDrawer.cs` | Brace ability icon, cyborg icons |

---

## 10. Design notes and iteration history

1. **Brace placement** — early versions anchored to ground/eye height incorrectly; final design locks anchor at player Y with horizontal-only activation constraint.
2. **View vs. gun tilt** — mouse window now rotates camera (crosshair moves with gun), not just the held model.
3. **Sway tuning** — multiple passes: global slow-down, per-weapon speeds, variable envelope to avoid repetitive fast jitter.
4. **Reload while braced** — originally faster (0.6×); revised to fixed **6 s / 8 s** absolute times.
5. **Blindness** — fiery overlay sits on opaque black; no see-through gap between phases; input fully blocked while any blindness active.

---

## 11. Suggested playtest checklist

- [ ] Anti-material: ADS-only fire, 1 s charge, empty-mag charge silence
- [ ] Stick to dummy → kill dummy → round drops and detonates on ground
- [ ] Explosion fire blindness → black follow-up → red exit fade
- [ ] Brace toggle, A/D orbit, horizontal-only activation, 6 s braced reload
- [ ] Scope sway feel on sniper, hunting rifle, anti-material, ranger
- [ ] Bullets land on crosshair/red dot during sway
- [ ] Infantry E: speed, reload, draw, recoil for 10 s
- [ ] Cyborg laser overheat, sword swing, regen boost
