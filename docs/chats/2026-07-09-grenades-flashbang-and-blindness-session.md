# Chat Recap: Grenades, Flashbangs, and Blindness Layering

**Date:** July 9, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Hunter, Ranger, Skirmisher, Heavy session](2026-07-09-hunter-ranger-skirmisher-heavy-session.md)

This session implemented a full grenade system (frag + flashbang), a dedicated **Q**
hotbar slot with radial selector, rigidbody throw physics, flashbang white blindness,
blindness compositing and input rules, grenade hand-lock/cooldown/draw timing, and
several UX fixes across wheels, hotbar, and controls.

---

## 1. Grenade hotbar and controls

### Layout

Hotbar order: **`E` ability · `1` `2` weapons · `Q` grenade · `F` build · `H` hammer**

The grenade slot sits between the weapon pair and build tools. Mouse wheel scrolls
through all equippable slots including grenades.

### Q key behavior

| Input | Action |
|-------|--------|
| **Q tap** | Equip grenade slot (select last grenade type) |
| **Q hold** (~0.18 s) | Open grenade radial wheel |
| Drag on wheel | Highlight grenade type by sector |
| **Q release** (after hold) | Confirm wheel choice and equip grenade slot |

### Throw controls

| Input | Action |
|-------|--------|
| **Left click** | Throw immediately with full **5 s** fuse (no prime required) |
| **Right click** | Prime grenade in hand (**5 s** fuse); **left click** then throws with remaining fuse (cook) |
| Draw time | **0.5 s** after selecting grenade slot before prime/throw |
| Post-throw cooldown | **3 s** before another grenade can be pulled out |

### Hand lock (while primed)

While a grenade is primed in hand (**RMB**):

- Cannot switch hotbar slots (scroll, `1`/`2`/`F`/`H`)
- Cannot open grenade wheel or re-select grenade slot (**Q**)
- Cannot change grenade type on the wheel

After throw or in-hand detonation, hand lock clears but the **3 s** pull-out cooldown applies.

### Radial wheel UX (build + grenade)

- Build wheel (**RMB** in build mode) and grenade wheel (**Q** hold) are **mutually exclusive**
- While either wheel is open: block other gameplay input, hide crosshair
- Grenade wheel sectors split **evenly** by grenade count (2 types = 180° each)
- Drag direction selects sector (not fixed 4-quadrant layout)

**Files:** `ThirdPersonController.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`, `CardKitDefinition.cs`

---

## 2. Grenade types

| Type | Fuse | Damage / effect |
|------|------|-----------------|
| **Frag** | **5 s** | **70** at 0 m → **15** at **8 m** (LOS); gray smoke **0.5 s**; gun-style black blindness |
| **Flashbang** | **5 s** | No damage; white screen blindness in **150°** cone, **30 m** max range |

Both share throw physics via `ThrownGrenadeProjectile` base class.

**New / key files:**

- `GrenadeType.cs` — `Frag`, `Flashbang`
- `ThrownGrenadeProjectile.cs` — shared rigidbody physics
- `FragGrenadeProjectile.cs` — frag detonation
- `FlashbangGrenadeProjectile.cs` — flashbang detonation
- `GrenadeBlastUtility.cs` — frag damage + blindness
- `FlashbangBlindUtility.cs` — cone/LOS/duration math
- `FragGrenadeSmokeEffect.cs` — gray smoke VFX
- `FlashbangBurstEffect.cs` — burst VFX

---

## 3. Throw physics

Grenades use **Unity Rigidbody** + **SphereCollider** (`ContinuousDynamic`).

| Parameter | Value |
|-----------|-------|
| Throw speed | **30 m/s** along aim direction |
| Gravity | **9.81 m/s²** applied each physics step (no artificial air speed loss) |
| Radius | **0.09 m** sphere |
| Mass | **0.42 kg** |
| Ground friction | High (`0.92` dynamic / `0.96` static) — minimal rolling |
| Bounce | Low (`0.18`) |
| Ground damping | Extra horizontal × **0.94** and angular × **0.82** per step when grounded |
| Initial spin | Reduced (~**18%** of early values) |

Removed distance-based horizontal speed cap (`SpeedLossPerTenMeters`). Arc comes from
gravity; ground contact friction stops roll.

**Iteration notes:** Early versions used kinematic sphere casts and tunneled through
floors; switched to rigidbody physics. Friction tuned multiple times per user feedback.

---

## 4. Flashbang blindness

### Effect math (`FlashbangBlindUtility`)

- **150°** horizontal view cone, **30 m** max range, line-of-sight required
- **Always 4 s** temporary white fade phase (`TemporaryBlindnessSeconds`)
- Within **15 m**: add **0–4 s** complete white first (lerp **4 s @ 0 m** → **0 s @ 15 m**)
- Peak fade alpha: **100% @ ≤15 m** → **20% @ 30 m** (0–1 scale, not 0–255)

### Stacking

When hit by a second flashbang while already blind:

- Keep the effect with the **longer total duration**
- **Do not restart** the animation (elapsed time preserved)
- Shorter overlapping flashes are ignored

### Input blocking

Flashbang white blindness is **visual only** — does **not** block reload, hotbar,
abilities, grenade throw, etc.

Gun/explosion **black** blindness still blocks gameplay input.

**Files:** `FlashbangBlindUtility.cs`, `PlayerBulletHitFlash.cs`, `ThirdPersonController.cs`

---

## 5. Blindness rendering (`PlayerBulletHitFlash`)

All blindness layers tick and composite simultaneously (no single-phase takeover).

### Draw order (back → front)

1. **White** — flashbang  
2. **Black** — gun hit / damage blindness  
3. **Fire** — explosion fireball overlay  
4. **Red** — gun hit fade-out tint  

Black always renders above white so gun blindness covers flashbang white when both active.

### API

| Property | Meaning |
|----------|---------|
| `IsBlind` | Any blindness effect active (visual) |
| `BlocksGameplayInput` | Black/fire blindness only — blocks reload, hotbar, etc. |

White overlay uses high sort-order canvas layer (`GameUICanvas` sort **300**).

**Files:** `PlayerBulletHitFlash.cs`, `PlayerHealth.cs` (regen uses `BlocksGameplayInput`)

---

## 6. Frag grenade blast

| Stat | Value |
|------|-------|
| Radius | **8 m** |
| Damage | **70** center → **15** edge (linear, LOS) |
| Smoke | Gray, **0.5 s** |
| Blindness | Gun-style black/red (not fiery explosion blindness) |

**Files:** `GrenadeBlastUtility.cs`, `FragGrenadeSmokeEffect.cs`

---

## 7. Visuals

| Grenade | Body color | Material |
|---------|------------|----------|
| Frag (held + projectile) | Shiny gray | Higher metallic/gloss |
| Flashbang (held + projectile) | Dull gray | Lower metallic/gloss |

Held grenade model shows only while **primed** (RMB). Instant LMB throw does not
show held model.

**Files:** `ThirdPersonController.cs` (held visual), `ThrownGrenadeProjectile.cs` subclasses

---

## 8. Related systems unchanged / context

- Kamikaze **C4** and **explosive vest** from prior sessions remain separate
- Build radial selector pattern reused for grenade wheel
- `CardHotbarTool.Grenade` added for selected-tool routing

---

## 9. File index (this session)

| Area | Files |
|------|-------|
| Controller / input | `ThirdPersonController.cs` |
| HUD / icons | `GameplayHud.cs`, `HotbarIconDrawer.cs` |
| Physics | `ThrownGrenadeProjectile.cs`, `FragGrenadeProjectile.cs`, `FlashbangGrenadeProjectile.cs` |
| Combat | `GrenadeBlastUtility.cs`, `FlashbangBlindUtility.cs` |
| VFX | `FragGrenadeSmokeEffect.cs`, `FlashbangBurstEffect.cs` |
| UI blindness | `PlayerBulletHitFlash.cs` |
| Health | `PlayerHealth.cs` |
| Types | `GrenadeType.cs`, `CardKitDefinition.cs` |

---

## 10. README

`README.md` hotbar, grenade, flashbang, blindness, and runtime architecture sections
updated to reflect this session.
