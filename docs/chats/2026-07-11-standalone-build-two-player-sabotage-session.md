# Chat Recap: Standalone Build Fixes, Menu→Game Diagnosis, and Two-Player Online Sabotage

**Date:** July 11, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Test Map 1, relay multiplayer, prep fixes, and sniper scope session](2026-07-11-test-map-multiplayer-sniper-session.md)

This session fixed standalone macOS/Windows builds failing to reach gameplay,
diagnosed the exact Editor-vs-player divergence, hardened menu→game scene flow,
and added a **two-player online mode** with **Red vs Blue** teams and
team-based drill sabotage rules.

---

## 1. Compile fix — `Object` ambiguous in `GameSession.cs`

**Error:**

```
Assets/Scripts/GameSession.cs(168,9): error CS0104: 'Object' is an ambiguous reference between 'UnityEngine.Object' and 'object'
```

**Cause:** `using System;` was added for `DateTime`, making bare `Object.Destroy(...)` ambiguous.

**Fix:** Qualify as `UnityEngine.Object.Destroy(...)`.

---

## 2. Standalone build diagnosis — menu opens, gameplay never starts

### Symptom

MainMenu worked in both Editor and standalone, but selecting a gameplay mode did
not reliably transition into playable Game scene content in macOS/Windows builds.

### Investigation method

- Compared `Logs/Editor.log` vs `~/Library/Logs/DefaultCompany/CoreWar/Player.log`
- Added structured boot tracing (`BootTrace.cs`) with tags:
  `[BOOT] [SERVICES] [SCENES] [NETWORK] [PLAYER] [MAP] [VOXELS] [UI]`
- Traced full flows: local single-player, online host, online client

### Verified timeline (both Editor and standalone through scene load)

```
Application starts
↓
MainMenu loads (build index 0)
↓
MenuNavigator / ProfileSession bootstrap
↓
Mode selected → matchmaking (local) OR multiplayer panel (online)
↓
GameSession.BeginMatch / BeginMatchForPrep + GameSessionLifetime (DDOL)
↓
SceneManager.LoadScene("Game") OR Netcode SceneManager.LoadScene (host only)
↓
VoxelFieldBuilder.Awake — guard passes, voxelMaterialSource resolves
↓
VoxelLightingWorld.Initialize → CreateBuildMaterial
```

### First divergence (standalone only)

**Player.log evidence:**

```
[VoxelFieldBuilder] Using material 'Voxel White Grid' shader='CoreWar/VoxelFaceLit'.
ArgumentNullException: Value cannot be null. Parameter name: shader
  at VoxelLightingWorld.CreateBuildMaterial ...
  at VoxelLightingWorld.Initialize ...
  at VoxelFieldBuilder.Awake ...
```

**Root cause:** `Shader.Find("Standard")` in `VoxelLightingWorld.CreateBuildMaterial()`
returned **null** in player builds. Floor material (`VoxelWhiteGrid.mat` →
`CoreWar/VoxelFaceLit`) was fine; **build-piece materials** still used runtime
`Shader.Find("Standard")`, which the Editor resolves but standalone builds strip
when no shipped material references that shader.

**Why Editor worked:** Editor ships the full built-in shader library.  
**Why standalone failed:** Player builds only include shaders referenced by
materials in the build or listed in **Always Included Shaders**.

### Fixes applied

| Change | File |
|--------|------|
| Added `Standard`, `Unlit/Color`, and `Hidden/CoreWar/*` post shaders to Always Included Shaders | `ProjectSettings/GraphicsSettings.asset` |
| `CreateBuildMaterial` logs probe result, falls back to `Legacy Shaders/Diffuse` / `CoreWar/VoxelFaceLit`, never calls `new Material(null)` | `VoxelLightingWorld.cs` |
| Serialized `voxelMaterialSource` on `VoxelFieldBuilder` (no `Shader.Find` for floor) | `VoxelFieldBuilder.cs`, `Game.unity` |
| Added `Assets/Materials/VoxelWhiteGrid.mat` + `Assets/Textures/VoxelGrid32.png` | new assets |
| Map material failure returns to MainMenu instead of freezing | `VoxelFieldBuilder.cs` |

### Ruled out as first failure (for local Test One Player)

- Scene list / build index (MainMenu=0, Game=1 — correct)
- Game scene authorization guard redirecting valid sessions
- `GameSession.IsMatchActive` lost across scene load
- Missing `Resources/NetworkManager.prefab` (local modes don't use it)
- `voxelMaterialSource` null in `Game.unity` (assigned to `VoxelWhiteGrid.mat`)

---

## 3. Game scene authorization guard + match lifetime

### Problem

Unauthorized Game scene entry guard could reject valid menu-started sessions if
static `GameSession` state was lost during scene transition.

### Solution

| Component | Role |
|-----------|------|
| `GameSessionLifetime.cs` | `DontDestroyOnLoad` carrier with `matchActive`, `gameModeId`, `inPrepPhase`, `entryToken` |
| `GameSession.HasAuthorizedGameEntry` | Requires `IsMatchActive` **and** lifetime object with `matchActive` |
| `RestoreFromLifetimeIfNeeded()` | Restores static state from DDOL object if static was cleared |
| `SceneFlow.TryBlockUnauthorizedGameScene()` | Logs ACCEPT/REJECT with mode, `IsMatchActive`, network state |
| Pre-load guards | Log error and **return** (no longer call `EnterMainMenu` while still on menu) |

### Diagnostic logging

`GameSession.LogDiagnostics(context)` and `BootTrace` probes at:
- `MenuNavigator.OnGameModeSelected` / `HandleMatchmakingCompleted`
- `SceneFlow.EnterGame` / `EnterGameForPrep` (includes `CanStreamedLevelBeLoaded`)
- `VoxelFieldBuilder.Awake`
- `MultiplayerSessionManager` host/join paths

---

## 4. Multiplayer NetworkManager standalone fix (from prior work, verified this session)

**Root cause:** Runtime `AddComponent<NetworkManager>()` does not create
`NetworkConfig` in standalone builds → `ConfigureNetworkManager` NRE.

**Fix:**

- `Assets/Resources/NetworkManager.prefab` with serialized `NetworkConfig`, `UnityTransport`, `PlayerPrefab`
- `MultiplayerSessionManager.TryEnsureNetworkManager()` loads prefab from Resources or serialized field
- Explicit validation per dependency (NetworkConfig, UnityTransport, PlayerPrefab, NetworkObject)
- Invalid instances destroyed, never started

---

## 5. Two-player online mode with team sabotage

### Mode access

| Entry | Flow |
|-------|------|
| **PLAY → TEST TWO PLAYER** | Opens host/join panel (no longer locked) |
| **Hub → MULTIPLAYER** | Same host/join panel |

Both use Relay + join codes. Local single-player modes (**SHOOTING RANGE**,
**TEST ONE PLAYER**) never call Unity Services.

### Team assignment (2 players)

| Client | Team | Drill island |
|--------|------|--------------|
| Host (client 0) | **Red** | Outer island 0 |
| Joiner (client 1) | **Blue** | Outer island 1 |

`NetworkPlayerAvatar.ServerPrepare()` assigns `spawnIndex % 2` for 2-player matches.
Local owner syncs `GameSession.SetLocalTeam()` for HUD.

### Sabotage rules (2+ players / online)

| Action | Who | When |
|--------|-----|------|
| **Sabotage** (stop enemy drill) | Enemy team only | Enemy drill is **running** |
| **Restart drill** (undo sabotage) | Same team only | Own drill is **stopped** |

- Hold **T** within **2.5 m** for **5 s**
- HUD shows `HOLD T SABOTAGE` or `HOLD T RESTART DRILL`
- Solo 1-player modes keep toggle-any-drill behavior

### Networked objectives

New `NetworkTestMapObjectiveSync` (server-authoritative):

- Syncs Red/Blue drill working state and team points
- Clients send interaction via `ServerRpc`; server validates team rules
- Win detection and match end replicate to all clients
- Spawned by server in `VoxelFieldBuilder.BuildTestMapOne()`

### New / changed files

| File | Change |
|------|--------|
| `NetworkTestMapObjectiveSync.cs` | **New** — networked drill/points/win state |
| `TestMapObjectiveManager.cs` | Team sabotage rules, network integration, `PlayerTeam` |
| `ThirdPersonController.cs` | `PlayerTeam` property |
| `GameSession.cs` | `SetLocalTeam()` |
| `NetworkPlayerAvatar.cs` | Red/Blue 2p assignment, local team sync |
| `GameModeDefinition.cs` | `test_two_player` playable via `requiresOnlineMultiplayer` |
| `MenuNavigator.cs` | Routes `test_two_player` → multiplayer panel |
| `MultiplayerSessionPanel.cs` | Two-team copy and sabotage help text |
| `TestObjectiveHud.cs` | Sabotage/restart interaction labels |

### Still local-only (not networked yet)

Projectiles, voxel building, health between players, match prep sync, point-steal
milestones from design doc.

---

## 6. BootTrace instrumentation (temporary diagnostics)

`Assets/Scripts/BootTrace.cs` — `RuntimeInitializeOnLoadMethod` hooks:

- Environment dump (platform, paths, build scene list, `UNITY_EDITOR` flag)
- `Shader.Find` probes for critical shaders at startup
- `Resources.Load` probes for NetworkManager / NetworkPlayer prefabs
- Scene load/unload events
- Exception capture to `[BOOT] CAPTURED` lines

Useful for comparing Editor Console vs `Player.log` after rebuild.

---

## 7. Button → callback map (this session)

| Button | Callback chain | Scene load |
|--------|----------------|------------|
| TEST ONE PLAYER | `OnGameModeSelected` → local matchmaking → `BeginMatchForPrep` → `EnterGameForPrep` | `SceneManager.LoadScene("Game")` |
| SHOOTING RANGE | same → `BeginMatch` → `EnterGame` | `SceneManager.LoadScene("Game")` |
| TEST TWO PLAYER | `OnGameModeSelected` → `MultiplayerSessionPanel` | — |
| MULTIPLAYER → HOST | `HostAsync` → services → `PrepareLocalGameSession` → `LoadGameForHost` | `NetworkManager.SceneManager.LoadScene("Game")` (server only) |
| MULTIPLAYER → JOIN | `JoinAsync` → services → wait for host scene sync | host-driven |

---

## 8. Test steps

### TEST A — Local without internet

1. Disconnect network
2. Launch standalone build
3. **PLAY → TEST ONE PLAYER**
4. Expect: Game scene, map, local player, no `ArgumentNullException` in Player.log

### TEST B — Editor host + standalone client

1. Editor → **MULTIPLAYER → HOST** → copy join code
2. Standalone → **MULTIPLAYER → JOIN**
3. Both enter Game scene; Red vs Blue; sabotage enemy drill, restart own drill

### TEST C — Two computers

1. Host standalone → share join code
2. Remote standalone → join with code (internet only; no port forwarding)
3. Both spawn on opposing teams

### Log locations

| Platform | Path |
|----------|------|
| macOS Player | `~/Library/Logs/DefaultCompany/CoreWar/Player.log` |
| Windows Player | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\CoreWar\Player.log` |
| Unity Editor | `Logs/Editor.log` (project-relative) |

---

## 9. Files touched (full session)

### New

- `Assets/Scripts/BootTrace.cs`
- `Assets/Scripts/GameSessionLifetime.cs`
- `Assets/Scripts/NetworkTestMapObjectiveSync.cs`
- `Assets/Resources/NetworkManager.prefab`
- `Assets/Materials/VoxelWhiteGrid.mat`
- `Assets/Textures/VoxelGrid32.png`

### Modified

- `Assets/Scripts/GameSession.cs`
- `Assets/Scripts/SceneFlow.cs`
- `Assets/Scripts/MainMenuController.cs`
- `Assets/Scripts/VoxelFieldBuilder.cs`
- `Assets/Scripts/VoxelLightingWorld.cs`
- `Assets/Scripts/TestMapObjectiveManager.cs`
- `Assets/Scripts/ThirdPersonController.cs`
- `Assets/Scripts/Multiplayer/MultiplayerSessionManager.cs`
- `Assets/Scripts/Multiplayer/NetworkPlayerAvatar.cs`
- `Assets/Scripts/Multiplayer/NetworkPlayerSpawner.cs`
- `Assets/Scripts/Matchmaking/GameModeDefinition.cs`
- `Assets/Scripts/UI/MenuNavigator.cs`
- `Assets/Scripts/UI/MultiplayerSessionPanel.cs`
- `Assets/Scripts/UI/TestObjectiveHud.cs`
- `Assets/Editor/MultiplayerPrefabSetup.cs`
- `Assets/Scenes/Game.unity`
- `ProjectSettings/GraphicsSettings.asset`

---

## 10. Inspector / build requirements

| Item | Requirement |
|------|-------------|
| `Game.unity` → VoxelFieldBuilder | **Voxel Material Source** = `Assets/Materials/VoxelWhiteGrid.mat` |
| Multiplayer prefabs | Run **CoreWar → Multiplayer → Rebuild Multiplayer Prefabs** if missing |
| Build scenes | MainMenu (0), Game (1) in Build Profile |
| Unity Cloud | `cloudProjectId` linked for online Relay modes |
| Rebuild | Required after `GraphicsSettings.asset` shader list changes |
