# Chat Recap: Ammo, Reload, Ballistics Rewrite, and Hotbar Icons

**Date:** July 7, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Sniper Unlocks, Ballistics, and Abilities Session](2026-07-07-sniper-unlocks-ballistics-and-abilities-session.md)

This session added per-weapon ammo and reload, rewrote bullets from physics balls to
raycast flight, polished sniper ADS/scope UX, added weapon draw animations, replaced
hotbar text with procedural icons, and tuned crosshair/iron-sight blur.

---

## 1. UI cleanup

| Change | Detail |
|--------|--------|
| Removed top-of-screen debug HUD | No more on-screen state dump during play |
| Removed **Test Damage** from pause menu | Standard arena pause is now Respawn / Settings / Exit Match only |

**Files:** `ThirdPersonController.cs`, `GamePauseMenu.cs`

---

## 2. Ammo and reload

Each firearm has its own reserve + magazine pool (`WeaponAmmo.cs`):

| Weapon | Start (reserve / mag) | Mag size | Max total |
|--------|------------------------|----------|-----------|
| Pistol | 150 / 12 | 12 | 162 |
| AR | 200 / 30 | 30 | 230 |
| Sniper | 40 / 5 | 5 | 45 |

**HUD:** Small white panel with thin black border above the hotbar row. Shows
`reserve / mag` only while holding that weapon.

**Reload (`R`):**

| Weapon | Timing |
|--------|--------|
| Pistol | 1.2 s full mag |
| AR | 1.5 s full mag |
| Sniper | 1.5 s locked start, then 0.8 s per round |

**Reload rules:**

- **R** does nothing on a full mag
- Reload blocks shooting, hotbar swap, and E ability (sniper fully locked through start + first round; interruptible after that)
- Dark overlay on hotbar slots during reload (including sniper per-round phase)
- Gun model dips during reload (not the camera); sniper per-round reload adds a quick up/down pulse
- Starting reload exits sniper ADS, but you can re-enter ADS while reloading
- Ammo resets on match start, respawn, and character reset (shooting range Choose Character)
- Per-weapon gunshot audio via `MenuUiSounds.PlayWeaponGunshot`

**Files:** `WeaponAmmo.cs`, `ThirdPersonController.cs`, `MenuUiSounds.cs`

---

## 3. Combat tuning

| Change | Value |
|--------|-------|
| Pistol recoil scale | **0.5×** (half previous kick) |
| Pistol max body damage | **13** (was 15) |

**Files:** `ThirdPersonController.cs`, `ProjectileDamage.cs`

---

## 4. Sniper ADS and scope UX

| Feature | Behavior |
|---------|----------|
| Gun model visibility | Hidden during magnified ADS (4× / 10×); visible on iron sights and hip fire |
| **E** scope cycle | Works without ADS (instant index change); while ADS still uses the scope-swap dip animation |
| Ability hotbar icon | Shows **next** scope (`(_sniperScopeIndex + 1) % 3`): iron `\|▲\|`, bold red **4X**, **10X** |
| Iron sights blur | Same peripheral blur as scoped ADS, **no black vignette** |
| Iron / AR crosshair | Smaller reticle (`weaponCrosshairGap` 3, `weaponCrosshairLength` 6) |

**Files:** `ThirdPersonController.cs`, `SniperScopePostEffect.cs`, `SniperScopePost.shader`

---

## 5. Prep-phase fire fix

Holding left click during match prep no longer fires (or plays gunshot audio) when
gametime starts. Input is blocked until the mouse is released after prep ends.

**Files:** `ThirdPersonController.cs` (`_weaponMouseHeldDuringPrep`, `_blockWeaponFireUntilMouseRelease`)

---

## 6. Bullet rewrite

Replaced hybrid Rigidbody bounce/roll bullets with **raycast-integrated flight**:

| Rule | Behavior |
|------|----------|
| Flight | Raycast each frame + gravity + per-weapon air drag |
| Visual | Dark sphere visible to **everyone** while in flight |
| Player builds | Pass through with ~50% speed loss per piece |
| Map / floor | Stop and destroy — **no bounce** |
| AR / Pistol vs players | Destroy on hit; sub-30 m/s = no damage, destroy |
| Sniper vs players | Penetrates above **500 m/s**; speed + accuracy penalty per hit; tracks hit targets via `HashSet<GameObject>` |
| Lifetime | No global bullet cap; 20 s failsafe destroy |
| Distance culling | Removed — bullets stay visible at any range while airborne |

**Files:** `ProjectileBullet.cs`

---

## 7. Weapon draw times

Gun animates up from below the screen before firing, reloading, or ADS:

| Weapon | Draw time |
|--------|-----------|
| Pistol | 0.6 s |
| AR | 1.1 s |
| Sniper | 2.0 s |

Inspector fields: `pistolDrawSeconds`, `assaultRifleDrawSeconds`, `sniperDrawSeconds`, `weaponDrawHiddenLocalY`.

**Files:** `ThirdPersonController.cs`

---

## 8. Hotbar icons

Replaced text labels in hotbar slots with procedural `OnGUI` icons (`HotbarIconDrawer.cs`):

| Slot | Icon |
|------|------|
| Sniper ability | Next scope: iron `\|▲\|`, **4X**, **10X** |
| Infantry ability | Boot with wings |
| AR / Pistol / Sniper | Mini gun silhouettes |
| Hammer | Mini hammer |
| Blueprint | Mini blueprint sheet |

Key labels (`E`, `1`, `2`, `F`, `H`) remain in slot corners.

**Files:** `HotbarIconDrawer.cs`, `ThirdPersonController.cs`

---

## 9. Bug fixes

| Issue | Fix |
|-------|-----|
| Missing `BeginSniperScopeSwap` method header | Restored compile |
| `GetInstanceID()` obsolete in Unity 6 | Switched penetration tracking to `HashSet<GameObject>` |

---

## Files touched (summary)

| File | Role |
|------|------|
| `WeaponAmmo.cs` | New — ammo pool struct and defaults |
| `HotbarIconDrawer.cs` | New — procedural hotbar icons |
| `ThirdPersonController.cs` | Ammo HUD, reload, draw, prep fire block, scope UX, crosshair tuning |
| `ProjectileBullet.cs` | Raycast bullet rewrite |
| `ProjectileDamage.cs` | Pistol max body damage 13 |
| `SniperScopePostEffect.cs` | Iron-sight blur mode (no vignette) |
| `GamePauseMenu.cs` | Removed Test Damage button |
| `MenuUiSounds.cs` | Per-weapon gunshot sounds |

---

## Suggested follow-ups

- Network sync for ammo/reload state in multiplayer
- Reload animation polish per weapon (mag eject, bolt rack)
- Shooting range README alignment if range-specific ammo rules diverge later
