# Chat Recap: Unified Game UI, HUD Polish, and Combat Tuning

**Date:** July 8, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Ammo, Reload, Ballistics, and Hotbar Icons Session](2026-07-07-ammo-reload-ballistics-and-hotbar-icons-session.md)

This session unified in-game UI onto a shared canvas with proper scaling, fixed
menu/pause click and navigation issues, scaled the gameplay HUD, layered pause
submenus, and tuned ballistics, recoil, and bullet behavior.

---

## 1. Bullet tuning (penetration + visuals)

| Change | Detail |
|--------|--------|
| Sniper penetration deflection | `SniperAccuracyDeflectionDegrees` **1.2° → 0.24°** (~80% less randomness when penetrating players/objects) |
| Distant bullet size | `UpdateBulletVisualScale()` — bullets beyond **50 m** scale up so apparent size stays constant for distant viewers |

**Files:** `ProjectileBullet.cs`

---

## 2. Unified game UI canvas

All in-match HUD and overlays now parent under one scene canvas with shared
`CanvasScaler` (1920×1080, match 0.5).

| Component | Role |
|-----------|------|
| `GameUICanvas.cs` | Bootstrap (`Game UI Canvas` in `Game.unity` or runtime fallback), `CreateLayer`, `CreateScreenHost`, `CreateInteractionLayer` (nested canvas + sort order for clickable overlays) |
| `GameplayHud.cs` | Crosshair, hotbar, ammo panel, build selector (replaces `ThirdPersonController.OnGUI`) |
| `HotbarIconDrawer.cs` | Texture cache (`GetToolIconTexture`, `GetIronSightIconTexture`, etc.) for `RawImage` icons |

**Migrated to shared canvas:** `MatchClockHud`, `MatchPrepController`, `GamePauseMenu`, `RespawnClassPicker`, `ShootingRangeCharacterPicker`, `ShootingRangeDummyStatsPanel`, `PlayerBulletHitFlash`

**Files:** `GameUICanvas.cs`, `GameplayHud.cs`, `GameUICanvas.cs.meta`, `GameplayHud.cs.meta`, `Game.unity`, `VoxelFieldBuilder.cs`, `ThirdPersonController.cs`, overlay UI scripts above

---

## 3. Compile and layout fixes

- Restored truncated `ShootingRangeCharacterPicker.cs`
- `SniperScopePostEffect.cs` — `scopeRadius` field shadowing fix
- `GameUICanvas` — `FindFirstObjectByType` → `FindAnyObjectByType`
- Clock/hotbar/crosshair anchors — `CreateScreenHost()` full-screen stretch for clock, pause, pickers, hit flash
- `GameplayHud` — layer stays active; only **Content** child toggles (fixes `LateUpdate` not running when layer was disabled)

---

## 4. UI clicks and EventSystem

**Root causes fixed:**

1. `InitializeGameScene()` / `InitializeMainMenuScene()` destroyed `EventSystem` without reliably recreating it → `EnsureEventSystem()` after every `ResetTransientUiInfrastructure()`
2. Button/input `Image` components without sprites did not raycast on game canvas → `MenuUiFactory.WhiteSprite` on buttons, dims, card tiles, input borders
3. Interactive overlays below HUD → `CreateInteractionLayer()` with `overrideSorting` + `GraphicRaycaster`; `BringLayerToFront()` on show
4. Menu window fade at `CanvasGroup` alpha 0 blocked first click → default `animateFade: false` on `MenuWindowFrame`; input fields set `targetGraphic`

**Files:** `SceneFlow.cs`, `MenuUiFactory.cs`, `MenuWindowFrame.cs`, `MenuNavigator.cs`, `GameUICanvas.cs`, `GamePauseMenu.cs`, `MatchPrepController.cs`, pickers, `CardTileView.cs`

---

## 5. Gameplay HUD scale and layout

| Element | Scale / layout |
|---------|----------------|
| Hotbar | **200%** (`HotbarScale = 2f`) |
| Crosshair | **250%** (`CrosshairScale = 2.5f`) |
| Ammo panel | Anchored just above hotbar (4 px gap at 1×, scales with hotbar); never overlaps slots |
| Hotbar slots | 1 px black borders (same as ammo panel) |

**Files:** `GameplayHud.cs`

---

## 6. Pause menu layering

Pause uses a **stack**: main pause → one submenu at a time.

| Action | Behavior |
|--------|----------|
| Settings / Dummy Stats / Exit confirm | Hides main pause; shows only submenu |
| Back / Escape / Stay / Apply | Closes submenu; restores main pause |
| Respawn / Choose Character | Hides pause overlay (pause stays open); picker **back** returns to pause |

**Files:** `GamePauseMenu.cs`, `ShootingRangeDummyStatsPanel.cs` (built into pause overlay via `BuildInto`), `RespawnClassPicker.cs`, `ShootingRangeCharacterPicker.cs`

---

## 7. Sniper ability and hotbar icon

| Change | Detail |
|--------|--------|
| **E** scope cycle | Works whenever ability is ready (`_sniperScopeSwapPhase == 0`), **not only when sniper is equipped**; animated swap still requires ADS + sniper held |
| Iron sight icon | Flipped vertically in `HotbarIconDrawer` (was upside down in ability slot) |

**Files:** `ThirdPersonController.cs`, `HotbarIconDrawer.cs`

---

## 8. Combat fixes and tuning

### Self-damage prevention

Bullets track `ownerRoot`; shooter colliders are ignored by default. Future reflector/ricochet can pass `canHitOwner: true` on spawn.

**Files:** `ProjectileBullet.cs`, `ThirdPersonController.cs`

### Air drag (exponential per 100 m)

| Weapon | Before | After |
|--------|--------|-------|
| AR | ~10% loss (0.90 retained) | ~5% loss (**0.95** retained) |
| Pistol | ~20% loss (0.80 retained) | ~25% loss (**0.75** retained) |
| Sniper | ~2% loss (0.98) | unchanged |

### Recoil scale

| Weapon | Before | After |
|--------|--------|--------|
| Pistol | 0.50 | **0.425** (−15%) |
| AR | 0.75 | **0.675** (−10%) |
| Sniper ADS | 2.7 | **2.835** (+5%) |
| Sniper hip | 4.8 | **5.04** (+5%) |

**Files:** `ProjectileDamage.cs`, `ThirdPersonController.cs`

---

## 9. Files touched (summary)

**New:** `GameUICanvas.cs`, `GameplayHud.cs` (+ `.meta`)

**Gameplay / ballistics:** `ProjectileBullet.cs`, `ProjectileDamage.cs`, `ThirdPersonController.cs`, `VoxelFieldBuilder.cs`, `SniperScopePostEffect.cs`, `SceneFlow.cs`

**UI:** `GamePauseMenu.cs`, `GameplayHud.cs`, `HotbarIconDrawer.cs`, `MatchClockHud.cs`, `MatchPrepController.cs`, `MenuUiFactory.cs`, `MenuWindowFrame.cs`, `MenuNavigator.cs`, `RespawnClassPicker.cs`, `ShootingRangeCharacterPicker.cs`, `ShootingRangeDummyStatsPanel.cs`, `PlayerBulletHitFlash.cs`, `CardTileView.cs`

**Scene:** `Assets/Scenes/Game.unity` (pre-built **Game UI Canvas**)

---

## 10. Testing checklist

- [ ] Main menu: first click on sign-in input and hub buttons works immediately
- [ ] Match prep: card select and READY clickable
- [ ] Pause → Settings → back → pause; Pause → Dummy Stats → Apply/back → pause
- [ ] Pause → Respawn / Choose Character → back arrow → pause (not full unpause)
- [ ] Hotbar/crosshair scaled; ammo above hotbar without overlap
- [ ] Sniper **E** cycles scope while holding pistol
- [ ] AR full-auto: no self-damage from own bullets
- [ ] AR retains more speed at range; pistol drops off faster
