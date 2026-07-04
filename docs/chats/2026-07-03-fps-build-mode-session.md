# Chat Recap: FPS Build Mode Iteration

**Date:** July 3, 2026  
**Project:** CoreWar  
**Context used:** `docs/Third_Person_Shooter_Game_Design_v2.md` and the prior Unity voxel prototype chat recap.

This chat continued the Unity prototype from the code-generated voxel arena and
focused on camera feel, player-facing behavior, and a more tactical build-mode
toolset.

## 1. Initial camera and crosshair pass

- Added a centered crosshair to the player controller.
- Moved the over-the-shoulder camera closer to the player.
- Kept build raycasts aligned to the center of the screen.

## 2. Aim-facing player and build mode

Implemented a build-mode flow in `Assets/Scripts/ThirdPersonController.cs`:

- Player visual turns toward the crosshair/camera aim direction.
- Pitch range increased to near-straight up/down.
- `F` toggles build mode.
- Build mode originally switched temporarily to first person, then later the
  whole controller was changed to always-first-person.
- Left click places the selected piece.
- Right click plus mouse direction opens a radial selector.
- Mouse wheel rotates wall/window/door orientation.

The local robot renderers are hidden in the FPS view so the camera does not see
through the player model.

## 3. Build piece system

Extended `Assets/Scripts/VoxelLightingWorld.cs` from full voxel placement into a
separate edge/face-mounted build-piece system.

Current build pieces:

- Wall
- Window
- Ceiling
- Door
- Trap Door
- Ladder

Removed during iteration:

- Horizontal Window / half-window item

Each piece uses generated Unity primitives and simple runtime materials. Ladders
are made from rail and rung primitives and can only attach to existing vertical
built surfaces.

## 4. Placement rules

The build system now tracks build-piece slots separately from full voxel
occupancy.

Important placement rules:

- Ground-supported vertical pieces and trap doors can start from the base grid.
- Ceilings cannot be free-floating; they must connect to other built pieces.
- Full side/edge contact counts as a connection.
- Corner-only contact does not count.
- Occupied slots are not valid for normal single placement.
- Rectangle drag skips already occupied slots on the same surface instead of
  failing the whole shape.
- Placement requires line of sight from the camera to the candidate center, so
  pieces cannot be placed through walls.

## 5. Preview and snapping

Build preview behavior changed several times and currently works as follows:

- Single-piece preview prefers the nearest valid green placement close to the
  crosshair.
- Snap candidates are ranked by screen-space distance to the crosshair center.
- Snap search ignores placements hidden behind walls.
- If no valid visible snap target exists, the aimed invalid placement may show
  red.
- Existing occupied slots are not suggested as normal single-placement targets.
- Preview outline objects are pooled during use and extra drag preview objects
  are destroyed after drag/build-mode exit to avoid accumulating hidden objects.

## 6. Ctrl-drag building

Added Ctrl + left-drag for faster building.

Supported pieces:

- Wall
- Window
- Door
- Ceiling

Behavior:

- Ctrl + left-drag starts from the current snapped/valid preview.
- Wall, window, and door drag creates a vertical rectangle.
- Ceiling drag creates a one-axis strip.
- Rectangle/strip size is capped to 12 cells from the start.
- Drag is locked to the starting piece plane, so looking away keeps the last
  valid endpoint instead of jumping to another surface.
- Wall rectangle width uses the camera's horizontal forward axis when possible,
  while still respecting the selected wall orientation.
- Ceiling strip direction uses the camera's horizontal forward axis.
- Dragged pieces are clamped to `y >= 1`, preventing underground suggestions.
- If any unoccupied piece in the intended preview cannot be placed or cannot be
  seen from the camera, the whole intended preview turns red and nothing places.

## 7. FPS conversion

The prototype was changed from a third-person/over-shoulder camera to an
always-first-person controller:

- Camera now sits at player eye height.
- The forward camera offset was removed so it does not peek through walls when
  the player runs against them.
- Field of view was widened to reduce the zoomed-in feel.
- The controller class is still named `ThirdPersonController.cs`, but its
  behavior is now FPS.

## 8. README update

Updated `README.md` to describe:

- First-person camera controls.
- Build-mode toggle and placement controls.
- Radial build selector.
- Ctrl-drag rectangle/strip placement.
- Current build pieces and placement-preview rules.
- Updated script responsibilities.

## 9. Important files touched

- `Assets/Scripts/ThirdPersonController.cs`
  - FPS movement/camera
  - crosshair
  - build-mode input
  - radial selection
  - Ctrl-drag placement
  - preview snapping and line-of-sight checks

- `Assets/Scripts/VoxelLightingWorld.cs`
  - build-piece types
  - placement candidates
  - occupancy and connection rules
  - batch validation
  - generated build-piece visuals

- `Assets/Scripts/PlayerBuiltVoxel.cs`
  - metadata for built piece cell, face normal, type, and panel state

- `README.md`
  - controls and architecture summary

## 10. Verification performed

Throughout the chat:

- `ReadLints` was run on edited scripts after substantive changes.
- `git diff --check` was run to catch whitespace issues.

Unity play-mode testing is still recommended for:

- exact FPS camera feel,
- build selector ergonomics,
- snap target ranking,
- Ctrl-drag axis feel,
- line-of-sight strictness.
