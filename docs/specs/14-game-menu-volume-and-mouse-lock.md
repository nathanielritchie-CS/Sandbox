# 14 — Game Menu: Volume Sliders and Mouse Lock

**Status:** Done (2026-11-06). Not networked; verified with the Editor host and a headless client.
**Design ref:** `docs/design/overview.md` (none: comfort/settings, asked for directly)

## Goal
In the game the mouse stays inside the window and is hidden. Esc opens a small menu with separate volume sliders and the list of controls; the volumes are remembered between sessions.

## Scope
- In:
  - **Mouse lock** (TestLevel): while the menu is closed the cursor is locked to the window and hidden. Unity frees it when the window loses focus and locks it again when you click back into the game.
  - **Esc menu** (TestLevel): a palette panel in the middle of the screen: heading "Menu", three sliders (**Master**, **Effects**, **Ambience**, 0–100%), the controls list, "Esc to close". While it is open the cursor shows and is free, and your character and the camera ignore input. The world keeps running (online co-op can't pause). Esc again closes it and locks the mouse.
  - **Volumes**: each slider drives one mixer parameter (`MasterVol` new, `SFXVol` existing, `AmbientVol` new); slider value v → 20·log10(v) dB (0 → −80 dB). Saved in PlayerPrefs and applied when Bootstrap and TestLevel start. Defaults: Master 50%, Effects 100%, Ambience 100%.
  - **Status window**: unchanged (C); the controls list names it.
- Out:
  - A Music slider (nothing plays music yet; `MusicVol` stays exposed for it), quit to menu, key rebinding, graphics settings, gamepad navigation of the menu, a volume slider on the title screen (the saved values still apply there).

## Requirements
- UI stays in the UI assembly. The menu reaches gameplay input only through the input actions asset: it turns the `Player` action map off while open. Cinemachine's look input reads that map's `Look` action, so the camera stops too (Cinemachine only auto-enables the action once, at start).
- Volume code is a thin MonoBehaviour; no new event channel.

## Networking
Not networked (local input, cursor and audio; every peer has its own menu).

## Plan (files)
- UI: new `GameMenu` (Esc toggle, cursor lock, `Player` map on/off), new `VolumeSettings` (sliders ↔ mixer ↔ PlayerPrefs).
- Assets: `Audio/MainMixer.mixer` (expose Master and Ambient volume, through the Editor's internal mixer API as in spec 11). TestLevel: `MenuCanvas` (GraphicRaycaster, sort order 20) with `MenuPanel`; an `EventSystem` + `InputSystemUIInputModule` (TestLevel has none, so a slider couldn't be clicked). Bootstrap: `VolumeSettings` without sliders on `ConnectionCanvas`.

## Done when
- [x] In game the cursor is hidden and locked; Esc shows it and the menu; Esc again hides both. (Host: `Locked`/hidden in TestLevel; menu open → `None`/visible; closed → `Locked`/hidden. Title screen: `None`.)
- [x] While the menu is open, moving, attacking, rolling and mouse look do nothing; clicking or dragging a slider doesn't attack. (Host: `Player` map, Attack and Look all disabled while open. Client, real keys: Esc opened it; a click + 1.5 s of W did nothing (no movement, no attack request on the server); after Esc again the same click + W moved 7.6 m and the server accepted the attack.)
- [x] Each slider changes its own group (mixer value read back, listener output measured); the values survive a restart and apply on the title screen. (Master 0.25 → −12.0 dB, Effects 0.25 → −12.0 dB, Ambience 0.6 → −4.4 dB, saved in PlayerPrefs. Listener RMS with the ambience playing: 0.0157 → 0.0017 at Ambience 10% → 0 at Ambience 0; Master 50% lowered it too. After a restart the title screen applied −12.04 / −12.04 / −4.44. Test values removed afterwards.)
- [x] Host and client each get their own menu and lock; console clean. (Client: own menu, its own `EventSystem`; host console 0 warnings/errors for the whole host + client session; client log clean.)
- [x] Compiles with zero errors, no new warnings; tests still pass (64/64); client build 0 warnings / 0 errors. Capture: `Isekai/Temp/harness/s14_menu.png`.

## Open questions
- None.

## Differences from spec
- "Esc to close" is the last line of the controls list ("Esc close this menu") rather than a separate hint.
- The controls list changed with specs 15 and 16 (charged heavy on the left-click hold, lock-on on right click); the panel widened 560 → 600 px so the longer lines fit.
- Not verified: the cursor lock on a windowed client (a headless player can't lock a cursor; the Editor host showed it), re-locking after alt-tab in a standalone build, the Effects slider by ear (its mixer value was read back; Master and Ambience were measured), Multiplayer Play Mode.
