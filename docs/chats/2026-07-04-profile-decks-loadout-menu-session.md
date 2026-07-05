# Chat Recap: Profile, Decks, Loadout, and Menu UI

**Date:** July 4, 2026  
**Project:** [CoreWar](https://github.com/leebuilds/CoreWar)  
**Prior context:** [Hotbar Tools and Combat Session](2026-07-04-hotbar-tools-and-combat-session.md)

This session turned the prototype from a simple team picker into a full menu
flow with local profiles, a 30-card collection, two-slot loadouts, shared window
chrome, in-game pause UI, and procedural menu sounds. All menu UI is still
built at runtime in C# (no scene prefabs).

---

## 1. Profile sign-in and local persistence

**Goal:** Start the game at sign-in; support multiple local profiles with
username + passcode; plan for future online auth without exposing local data on
GitHub.

**Implemented in `Assets/Scripts/Profile/`:**

| File | Role |
|------|------|
| `IProfileRepository.cs` | Abstraction for future server-backed auth |
| `LocalProfileRepository.cs` | JSON profiles under `Application.persistentDataPath/CoreWar/profiles/` |
| `PlayerProfile.cs` | Username, passcode hash/salt, owned cards, loadout slots, last-active timestamp |
| `PasscodeUtility.cs` | SHA-256 passcode hashing with per-profile salt |
| `ProfileSession.cs` | Active session, 1-hour offline expiry, manual logout |

**Auth flow:**

- Separate **Sign In** and **Sign Up** screens (no forgot-passcode flow).
- Usernames are unique locally; repository is structured for future public-server
  migration.
- Unlimited local profiles; session restores on launch if still within timeout.
- New accounts unlock all 30 catalog cards immediately (placeholder progression).

**Git hygiene:** `.gitignore` excludes `ProfileData/` and `*.profile.json`. Runtime
data lives only in `persistentDataPath`.

---

## 2. Cards, loadout, and match session

**Catalog (`Assets/Scripts/Cards/`):**

- 10 specialties × 3 tiers = **30 cards** (`CardCatalog.cs`).
- Each card has rarity, specialty label, display name, preview stats, and a
  placeholder kit (gun / hammer / blueprint for all cards today).
- `CardKitDefinition.cs` defines hotbar tools; `ThirdPersonController` reads
  `GameSession.ActiveKit` at spawn and respawn.

**Loadout rules:**

- **Decks** = full collection browser.
- **Loadout** = two slots chosen outside a match; both cards available on every
  respawn via in-game class picker.
- Tapping a collected card opens **Preview** (stats modal) or **Select Slot 1 /
  Select Slot 2** (action sheet).

**Match entry (`GameSession.cs`):**

- Replaced team picker with **Play** screen: two loadout cards flank a **PLAY**
  button.
- Team is hard-coded **Red** for now (`quick play only`).
- Player taps a card before starting to choose spawn class; selected card gets
  green outline + footer label (`spawning as …`).
- `BeginMatch(team, loadoutA, loadoutB, initialActiveCardId)` carries choices
  into the Game scene.

**In-match respawn (`RespawnClassPicker.cs`):**

- Overlay to pick loadout slot A or B after death / from pause menu **Respawn**.
- ESC closes picker without opening pause menu.

---

## 3. Menu navigation and screens

**`MenuNavigator.cs`** routes:

```
Sign In / Sign Up → Hub → Play | Decks | Settings | Logout | Quit
                         ↓
                    Play (confirm loadout + spawn class)
                    Decks (scroll collection + loadout bar)
                    Settings (placeholder)
```

**Hub buttons:** Play, Decks, Settings, Logout, Quit.

**Decks layout:**

- Vertical scroll: one row per specialty (10 rows).
- Each row: Tier 1 → Tier 2 → Tier 3 with arrow separators.
- Header strip (`LoadoutSlotBar.cs`) shows both loadout slots; tap to clear.
- Card tap → modal with Preview / Select Slot 1 / Select Slot 2.

**Back navigation:**

- Title-bar back arrow (small bordered square button).
- **ESC** closes preview → card action modal → previous screen (Sign In / Hub
  with empty stack do nothing).

---

## 4. Shared menu window language

**`MenuWindowFrame.cs`** + **`MenuUiFactory.cs`** provide consistent chrome:

- Thin **black border**, white interior, title bar with centered title.
- Optional header band (Decks loadout strip), footer hint/error line.
- Modal overlays with dim background and fade-in animation.
- Large centered windows with generous white space (user preference).

**Card tiles (`CardTileView.cs`):**

- Layer order: loadout outline → black border → rarity-colored fill → text.
- Rarity fill uses saturated colors from `CardRarityColors.cs`.
- Hover: scale + drop shadow (`CardTileHover`).
- Green outline when card is in loadout or selected for spawn on Play screen.

**Raycast fixes:** Decorative text and modal fill images set `raycastTarget =
false` so Preview / Select buttons receive clicks.

---

## 5. UI polish iterations

| Request | Resolution |
|---------|------------|
| Preview / Select buttons not working | Fixed raycast blocking on text and modal fill |
| Two select buttons (Slot 1 / Slot 2) | Added to action sheet and preview panel |
| Card styling: white + black border + rarity banner | Evolved to rarity-colored fill, black outline, larger text |
| Cards looked all black | Border was drawn on top of fill; reordered layers |
| Hub Settings button | Added placeholder Settings screen |
| Solid black buttons → bordered white buttons | `CreateBorderedButton` in `MenuUiFactory` |
| Login field borders | Black border + white inner on username/passcode inputs |
| Back arrow as small square box | `CreateTitleBarButton` uses bordered 40×40 button |
| Hammer range 1.5 voxels | `hammerRangeVoxels = 1.5f` in `ThirdPersonController` |

---

## 6. ESC, pause menu, and UI sounds

**Menus:** ESC = back (see §3).

**In-game pause (`GamePauseMenu.cs`):**

- ESC toggles pause overlay; **does not freeze time** (game continues).
- Options: **Respawn** (opens class picker), **Settings** (placeholder sub-modal),
  **Exit Match** (returns to MainMenu).
- ESC while Settings open closes Settings first, then pause on second press.
- While paused: all gameplay input blocked, movement stopped, crosshair and HUD
  hidden, cursor unlocked for UI clicks.

**Sounds (`MenuUiSounds.cs`):**

- Procedural hover, click, and gunshot clips (no audio assets).
- Wired to all menu buttons via `WireButton`.
- **Gunshot** plays only when starting a match from Play (`StartMatch()`), not
  on respawn or in-game menus.
- `AudioListener` added to menu camera in `MainMenuController.cs`.

---

## 7. Important files added or touched

| Area | Paths |
|------|-------|
| Profile | `Assets/Scripts/Profile/*` |
| Cards | `Assets/Scripts/Cards/*` |
| Menu UI | `Assets/Scripts/UI/MenuNavigator.cs`, `MenuUiFactory.cs`, `MenuWindowFrame.cs`, `CardTileView.cs`, `CardPreviewPanel.cs`, `LoadoutSlotBar.cs`, `MenuUiSounds.cs`, `GamePauseMenu.cs`, `RespawnClassPicker.cs` |
| Bootstrap | `MainMenuController.cs` (replaces old team picker) |
| Gameplay | `ThirdPersonController.cs` (kit from session, pause menu, hammer range), `GameSession.cs` |
| Git | `.gitignore` (local profile paths) |

---

## 8. Controls summary (after this session)

### Main menu

| Input | Action |
|-------|--------|
| ESC | Back (close modals, then previous screen) |
| Click | Navigate; hover/click sounds on buttons |

### In match

| Input | Action |
|-------|--------|
| ESC | Pause menu (or close respawn picker / settings sub-modal) |
| WASD / Mouse / Space / Hotbar | Normal gameplay (blocked while pause or respawn picker open) |

---

## 9. Not yet implemented

Natural next steps from this session and the design doc:

- [ ] Online auth / server profile sync
- [ ] Real Settings screen (audio, controls, account)
- [ ] Per-class kits (only infantry placeholder kit today)
- [ ] Card unlock progression (all 30 unlocked on signup for now)
- [ ] Team selection beyond hard-coded Red
- [ ] Pause menu that actually freezes simulation (if desired later)
- [ ] Gun damage, drills, objectives from design doc

---

*Generated from Cursor agent chat session, July 4, 2026.*
