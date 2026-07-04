# Chat Recap: Hotbar Tools, Combat, and Build Placement Polish

**Date:** July 4, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [FPS Build Mode Iteration](2026-07-03-fps-build-mode-session.md), [Unity Voxel Prototype](2026-07-03-unity-voxel-prototype-session.md)

This session continued the first-person build prototype into a more playable
field toolset: smarter placement previews, a three-slot hotbar, hammer
destruction, and a visual gun/combat pass with projectile bullets and recoil.

---

## 1. Build preview snapping (visible-side placement)

**Problem:** When aiming at a built wall, the preview often appeared on the far
(hidden) side in red, even though the player wanted to place on the near face.

**Solution in `ThirdPersonController.cs`:**

- Added visible-side candidate search when the raw raycast target is invalid or
  blocked.
- When the crosshair hits a built panel, anchor the search to that segment's
  cell and face normal.
- Rank snap candidates by screen-space distance to the crosshair center.
- Prefer closer-to-player steps when distances tie.
- Applies to all non-ladder build pieces (walls, windows, doors, ceilings, trap
  doors).

**Intent confirmed with user:**

- Prefer green valid placement on the camera-visible side of geometry.
- Search nearby alternatives if the first face slot is occupied.
- Always prefer green over red when a legal visible placement exists.

---

## 2. Build orientation controls

Replaced mouse-wheel orientation cycling with keyboard controls:

| Input | Behavior |
|-------|----------|
| `X` | Cycle wall/window/door orientation while blueprint is selected |
| `Z` | Toggle orientation lock (was briefly Left Shift hold, then toggle) |
| Scroll (no mouse move) | Lock targeted voxel while cycling orientations with `X` |

Build mode is no longer toggled with `F`. It is active only while the blueprint
hotbar slot is selected.

---

## 3. Ceiling Ctrl-drag rectangles

Ceiling Ctrl-drag was upgraded from a one-axis strip to a horizontal rectangle
across both X and Z at the drag start height.

**Batch placement fix:** Release previously placed only the start tile because
each piece was validated individually after prior tiles existed. Added
`VoxelLightingWorld.TryPlaceBuildPieceBatch()` so the whole validated rectangle
places atomically.

---

## 4. Three-slot hotbar

Added a bottom-screen hotbar and held-tool visuals in `ThirdPersonController.cs`:

| Slot | Tool | Left click |
|------|------|------------|
| 1 | Gun | Semi-auto fire (placeholder combat) |
| 2 | Hammer | Swing and destroy one owned build piece |
| 3 | Blueprint | Build mode (existing placement system) |

**Selection:** mouse wheel cycles slots; number keys `1`/`2`/`3` select directly.

**Held visuals:** simple runtime primitives parented to the camera (gun, hammer,
blueprint card).

**Doors:** still rotate with `X`, but no longer support Ctrl-drag rectangles
(wall/window/ceiling only).

---

## 5. Hammer destruction

Added `VoxelLightingWorld.TryRemovePlayerBuiltObject()`:

- Removes panel build pieces from `_buildPieces`.
- Falls back to legacy player voxel removal for non-panel markers.
- Hammer raycast destroys the first hit owned/man-made object only.
- Range measured from the closest point on the player capsule to the hit point,
  max one voxel.

---

## 6. Gun and projectile bullets

Added `Assets/Scripts/ProjectileBullet.cs` and wired gun firing in the
controller.

**Current behavior:**

- Semi-auto: one bullet per left click.
- Bullet spawns along true aim (`BuildCenterAimRay` from yaw/pitch pivots).
- High bullet speed with light gravity over distance.
- Muzzle flash at barrel on fire.
- Spawn position clamped forward when near geometry so bullets do not start
  on the far side of a wall the player is against.
- Bullets are visual-only (no damage yet).
- Pass through player-built panels, lose velocity, leave temporary bullet-hole
  decals.
- Stop and despawn on world/base geometry; timeout after landing or falling off.

---

## 7. Recoil iterations (final behavior)

Recoil went through several iterations in this chat:

1. Accumulating aim offset with recovery and spray suppression.
2. Visual-only camera rock (to fix pitch clamp issues).
3. Random `gunRecoilVerticalRandomness` / `gunRecoilHorizontalRandomness`.
4. **Final:** shot-first, then kick; crosshair ends away from shot line.

**Final recoil rules:**

- Bullet fires at current aim **before** recoil applies.
- Kick animates up with strong random vertical and small random horizontal
  components.
- Return phase is asymmetric: vertical kick comes partway back down but retains
  35–58% of the upward offset; horizontal offset is fully retained.
- When the kick finishes, residual offset is applied to `_yaw` / `_pitch`, so
  the crosshair settles away from where the shot was fired.
- Repeated shots stack drift; larger randomness values produce less predictable
  aim.

Tunable inspector fields:

- `gunRecoilVerticalRandomness`
- `gunRecoilHorizontalRandomness`
- `gunRecoilKickDuration`
- `gunMuzzleForwardOffset`

---

## 8. Ctrl-drag placement polish

**Line of sight:** Ctrl-drag preview turns red if **any** candidate in the shape
 lacks line of sight to its center (same rule as single placement).

**Occupied slots:** Outlines still render over already-built cells, but those
cells are excluded from validation and from batch placement. Only unoccupied
pieces in the drag shape must be valid and visible for a green preview.

---

## 9. Important files touched

| File | Changes |
|------|---------|
| `Assets/Scripts/ThirdPersonController.cs` | Hotbar, gun/hammer/blueprint tools, visible-side snapping, orientation bindings, ctrl-drag LOS/occupied rules, recoil, held-tool visuals |
| `Assets/Scripts/VoxelLightingWorld.cs` | `TryPlaceBuildPieceBatch`, `TryRemovePlayerBuiltObject` |
| `Assets/Scripts/ProjectileBullet.cs` | **New** — bullet flight, panel pass-through, bullet holes |
| `README.md` | Updated controls and architecture summary |

---

## 10. Verification performed

- `ReadLints` on edited C# files after substantive changes.
- `git diff --check` for whitespace issues.

Unity play-mode testing still recommended for:

- Hotbar feel and held-tool positioning.
- Hammer range at capsule edges.
- Bullet spawn clamp near walls.
- Recoil kick/drift feel when spamming vs pacing shots.
- Ctrl-drag preview over partial overlaps and occluded rectangles.

---

## 11. Not yet implemented

Natural next steps from this session:

- [ ] Gun damage / hit detection gameplay
- [ ] Ownership system for hammer (currently all local player-built pieces)
- [ ] Automatic fire option
- [ ] Recoil tuning pass in Unity inspector
- [ ] Shooting / objectives / drills from design doc

---

*Generated from Cursor agent chat session, July 4, 2026.*
