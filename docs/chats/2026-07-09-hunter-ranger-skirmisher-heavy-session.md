# Chat Recap: Hunter, Ranger, Skirmisher, Heavy, and Shooting Range Polish

**Date:** July 9, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Unified Game UI, HUD Polish, and Combat Tuning Session](2026-07-08-unified-game-ui-hud-and-combat-tuning-session.md)

This session implemented four new playable class cards (Hunter, Ranger, Skirmisher,
Heavy), tuned sniper damage, added weapon movement slows, pause-safe gameplay
timers, a Hunter mark UI system, heavy shield flash fixes, and a shooting-range
**Moving dummies** toggle.

---

## 1. New playable cards and unlocks

Default owned cards expanded (profile migration **v5**):

| Card ID | Display name | Primary | Secondary | E ability |
|---------|--------------|---------|-----------|-----------|
| `sniper_2` | Hunter | Hunting Rifle | Pistol | Mark (300 m forward arc) |
| `infantry_2` | Ranger | Scoped AR | Pistol | Hold breath |
| `infantry_3` | Skirmisher | AR | Machine Pistol | Dash |
| `heavy_1` | Heavy | LMG | Pistol | Shield |

**Files:** `CardCatalog.cs`, `CardKitDefinition.cs`, `ProfileSession.cs`

---

## 2. Sniper damage tuning

| Weapon | Body (full velocity) | Head (full velocity) |
|--------|----------------------|------------------------|
| Sniper rifle | **80** (was 60) | **100** (was 130) |

**Files:** `ProjectileDamage.cs`

---

## 3. Hunter (`sniper_2`)

### Hunting rifle

| Stat | Value |
|------|-------|
| Ammo | **48 / 1** (reserve / mag) |
| Reload | **2.1 s** per round, **manual (R)** — no auto-reload after firing |
| Reload lock | Cannot cancel once started (fire, hotbar swap blocked) |
| Damage | **65** body / **160** head at full velocity |
| Air drag | **4%** loss per 100 m (96% retention) |
| ADS | **6.5×** iron sights (peripheral blur, no scope label) |
| Draw | **80%** of sniper draw time |
| Movement | **85%** speed when held; **45%** when ADS or firing |

### Mark ability (E)

- Marks enemies within **300 m** in a **180°** forward hemisphere
- Screen-space **red target icon**: bullseye on head (wider than head hitbox, 4 cross ticks) + two teardrop guide lines below
- Icon scales with projected head size and distance; anchored to head center
- Visible **through walls** (UI overlay tracks `WorldToScreenPoint` each frame)
- Lasts **4 s**; **40 s** cooldown
- Icon **disappears immediately** when a marked dummy is eliminated
- Hunter marks layer renders **behind** gameplay HUD (crosshair, hotbar, health bar)

**New files:** `HunterMarkSystem.cs`, `HunterMarkOverlay.cs`, `HunterMarkOutlineDrawer.cs`  
**Files:** `ThirdPersonController.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`, `ShootingRangeSession.cs`, `VoxelFieldBuilder.cs`, `ShootingRangeDummy.cs`

### Hunter mark iteration notes

Early attempts used mesh silhouettes and camera post-effects; final design uses
procedural sprite + `HunterMarkOverlay` on `GameUICanvas`. Fixed dummy registration
order (`ShootingRangeSession.Initialize` was clearing dummies after they were built).

---

## 4. Ranger (`infantry_2`)

- **Scoped assault rifle** primary with **right-click ADS** (not left)
- **1.8×** magnification (not 2×)
- **Hold breath** (E): steady aim while holding E; **4 s** max, **14 s** cooldown
- Scoped AR recoil: **+50%** normal, **−50%** while hold breath active
- Pistol secondary

**Files:** `ThirdPersonController.cs`, `SniperScopePostEffect.cs`, `FullScreenBlur.shader`, `CardKitDefinition.cs`

---

## 5. Skirmisher (`infantry_3`)

- **AR** primary + **machine pistol** secondary (replaced SMG)
- Machine pistol: **150 / 18** ammo, velocity **halves every 100 m**, **2×** SMG recoil, pistol draw/reload timing
- **Dash** (E): **8 m** over **0.2 s**, **8 s** cooldown, full-screen blur during dash

**Files:** `ThirdPersonController.cs`, `WeaponAmmo.cs`, `ProjectileDamage.cs`, `CardKitDefinition.cs`

---

## 6. Heavy (`heavy_1`)

- **LMG** primary + pistol secondary
- **Shield** (E): **120** shield HP, decays **12/s**, **30 s** cooldown after break
- Blue flashing overlay on health bar while shield is active
- Shield flash speeds up in the last **20%** of shield; minimum period **0.1 s**
- Flash phase **resets each shield activation** (fixed drift across multiple uses)
- Movement: **70%** when holding LMG; **30%** when firing (LMG), **45%** when firing (sniper)

**Files:** `ThirdPersonController.cs`, `PlayerHealth.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`

---

## 7. Weapon movement slows

Heavier weapons reduce move speed while held, ADS, or firing (per-weapon tables in `WeaponHandlingSpeedFactor()`).

**Files:** `ThirdPersonController.cs`

---

## 8. Pause and gameplay timers

- Reloads, cooldowns, and ability timers **continue during pause**
- **No player input** processed while pause/overlays are open
- Prep-phase input rules unchanged

**Files:** `ThirdPersonController.cs`, `GamePauseMenu.cs`

---

## 9. Shooting range: moving dummies

Pause menu **Dummy Stats** → **Moving dummies** ON/OFF toggle:

- Dummies patrol **side to side** at **2.2 m/s**
- **Reverse direction** at inner face of left/right lane walls
- Alternate dummies start moving opposite directions
- OFF snaps dummies back to spawn positions
- Movement pauses while dummy is eliminated; resumes on respawn

**Files:** `ShootingRangeDummyStatsPanel.cs`, `ShootingRangeDummy.cs`, `ShootingRangeSession.cs`, `VoxelFieldBuilder.cs`

---

## 10. HUD and UI

- Health bar with **green / yellow / red** tiers by max HP
- **Shield flash** overlay (blue pulse on health bar)
- Ability icons for Hunter mark, Ranger hold breath, Skirmisher dash, Heavy shield
- Hotbar icons for hunting rifle, scoped AR, machine pistol, LMG

**Files:** `GameplayHud.cs`, `HotbarIconDrawer.cs`, `MenuUiSounds.cs`

---

## 11. Ballistics additions

| Weapon | Muzzle speed | Max body | Max head | ~loss / 100 m |
|--------|--------------|----------|----------|----------------|
| Hunting rifle | 950 m/s (sniper family) | 65 | 160 | 4% |
| Machine pistol | (SMG family) | — | — | 50% |
| LMG | — | — | — | ~5.5% |

**Files:** `ProjectileDamage.cs`, `WeaponAmmo.cs`

---

## 12. Bug fixes

| Issue | Fix |
|-------|-----|
| Hunter marks not showing on dummies | Initialize session before building targets; `SetPlayer` after spawn |
| Hunter mark top clipped | Sprite top padding + screen height includes cross-tick extent above head pivot |
| HUD behind hunter marks | Hunter marks layer `SetAsFirstSibling()` under game UI root |
| `HunterMarkOverlay` compile error | Renamed instance `ClearMarks` vs static `ClearMarks` → `ResetMarks()` |
| Heavy shield flash desync | Phase accumulator resets on shield start/end; fast flash in last 20% of shield |
| Hunting rifle reload cancel | `_sniperReloadLocked` for hunting rifle; no fire-cancel during reload |

---

## File index (new / major)

| Path | Role |
|------|------|
| `HunterMarkSystem.cs` | Scan targets, apply/clear marks |
| `HunterMarkOverlay.cs` | Screen-space mark icons |
| `HunterMarkOutlineDrawer.cs` | Procedural bullseye + teardrop sprite |
| `FullScreenBlur.shader` | Ranger hold breath / Skirmisher dash blur |
| `ThirdPersonController.cs` | All class weapons, abilities, movement slows |
| `GameplayHud.cs` | Health bar, shield flash, ability icons |
| `ShootingRangeDummy.cs` | Patrol movement, mark anchor helpers |
| `ShootingRangeDummyStatsPanel.cs` | HP slider + moving dummies toggle |

---

## Suggested follow-ups

- Skirmisher dash duration/cooldown may need re-tuning (currently 0.2 s / 8 s in code)
- Hunter card catalog text still says 5× ADS (implementation is 6.5×)
- Player-vs-player Hunter marks (no `ShootingRangeDummy` component on other players yet)
- Anti-Material sniper tier 3 remains catalog-only
