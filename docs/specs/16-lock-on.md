# 16 — Lock-On

**Status:** Built and verified (2026-11-06, including revision 1) with the Editor host + a headless client; waiting for your Multiplayer Play Mode sign-off before Done.

## Revision 1 (2026-11-06): a lock-on camera
- While locked, a dedicated lock-on camera takes over with a short blend (0.4 s): it sits behind you on the line from the target through you, raised and over your right shoulder, and aims at a point between you and the target (weighted toward the target). The enemy sits near the middle of the screen and your character stays in view in the foreground, off to the side. Letting go blends back to the normal camera, which then faces the way you were looking.
- This replaces "the orbit camera swings behind you" and the 24° pitch tweak. Mouse look stays off while locked.
- Files: `Gameplay/Cameras/LockOnCamera` (moves a pivot and a focus point, switches the lock camera on/off, keeps the normal camera's yaw in step so the way back is smooth); TestLevel: `LockOnVcam` (CinemachineCamera: Follow → pivot, position from a CinemachineFollow offset, aim from a RotationComposer on the focus point), the pivot and focus objects under `CameraRig`, the brain's default blend 2 s → 0.4 s.
- Done when: locked on the dummy and the Demon, both the target and your character are on screen from the side and from close range; the switch in and out is a smooth blend; host and client. **Verified:** captures `s16r3_dummy_close.png` and `s16r3_demon.png` show the whole character in the lower middle and the target above it, clear of the character; host: `FollowCamera` → `LockOnVcam (blending)` 0.15 s after locking → `LockOnVcam` → `FollowCamera (blending)` after letting go → `FollowCamera` facing the target's bearing (45° = 45°, no snap), mouse look back on; client: `FollowCamera` → `LockOnVcam` → `FollowCamera`.
- Tuned values (lock camera): FOV 50, follow offset (1.2, 2.4, −5.0) from a pivot facing the target, aim at the midpoint between chest and target, composer screen position (0, −0.12) (Cinemachine 3: negative = above centre), damping 0.3 position / 0.4 yaw / 0.25 aim; brain default blend 0.4 s EaseInOut.
- Superseded by spec 18: with the new character's strafe animations, locked-on movement always faces the target (Souls-style strafing) instead of free running.
- Known limit: neither camera avoids walls or rocks yet (`s16r3_dummy_far.png`: a stone block between the camera and the player). A Cinemachine Deoccluder on both cameras is a follow-up.
**Design ref:** `docs/design/overview.md` (Combat)

## Goal
Right click locks onto the enemy you are looking at. The camera keeps it in view, your attacks go to it, and you still run freely; right click again lets go.

## Scope
- In:
  - **Toggle** with right click (the input action `HeavyAttack` is renamed `LockOn`; the heavy moved to the left-click hold in spec 15).
  - **Target choice**: among living enemies and the training dummy within 15 m of you, the one closest to the middle of the screen (smallest angle from the camera's centre line, at most 45° off it). Nothing in view → nothing happens.
  - **Release**: right click again, the target dies or hides, it gets farther than 20 m away, or you die.
  - **Camera**: swings behind you toward the target (at up to 360°/s) and keeps it there; mouse look is off while locked.
  - **Movement (free running)**: the existing animations; WASD stays camera-relative, so W runs at the target and A/D circle it. Standing still, you turn to face the target.
  - **Attacks**: pressing attack snaps you to face the target; while a swing or charge plays you keep facing it; the server aims the swing at the target when it is within that attack's range, so a client's hit never depends on its turn reaching the server first.
  - **Marker**: a small gold dot on the target's body, on your screen only.
- Out:
  - Switching targets without letting go, strafe/backpedal animations (the models have none), line-of-sight checks, gamepad, framing both you and the target.

## Requirements
- Target choice is a plain C# `LockOnPicker` with tests.
- The lock is local: the server never learns your target except as the optional target of an attack request, and checks that one itself.
- Systems talk through a new `LockOnTargetChannel` (Transform channel): the player's lock component raises it; the camera and the HUD marker read it.

## Networking
- Server-authoritative state: the hit check, now aimed at the named target when it is an enemy with health, alive and within the attack's range; otherwise the player's facing, as before. Range stays enforced, so naming a far target gains nothing (facing was never a security boundary: rotation is owner-controlled).
- Client → server requests: `AttackRpc(target)`, `ReleaseChargeRpc(target)` (target may be empty).
- What each client sees: its own lock, camera and marker; everyone sees players turn toward their targets through the replicated rotation.

## Plan (files)
- Input: rename `HeavyAttack` → `LockOn` (right mouse).
- Gameplay: new `Targeting/LockOnPicker`; new `Cameras/LockOnCamera` (steers the orbit camera's yaw, turns the look input off while locked).
- Net: new `PlayerLockOn` (owner: toggle, pick, release rules, channel); `PlayerMovement` (face the target when idle or busy); `PlayerAttack` (snap, send the target; server aim).
- UI: new `LockOnMarker`.
- Assets: `Data/Events/LockOnTargetChannel.asset`; `Player.prefab` (`PlayerLockOn`); TestLevel (`LockOnCamera` on `CameraRig`, `LockOnCanvas` with the marker).
- Tests: new `EditMode/LockOnPickerTests`.

## Done when
- [x] Host and client: right click locks onto the dummy or the Demon, whichever is nearer the middle of the screen; the camera turns to it; the marker sits on it; right click again releases. (Host: camera yaw 0° → dummy, 68° → Demon; camera yaw then equals the bearing (also after moving: 30° = 30°), mouse look off, marker 0 px from the target's projected centre, player facing 0° off; toggling again released it, look back on, marker hidden. Client: locked onto the dummy, its camera turned to the bearing and pitched 17.5° → 24°. Captures `s16_lock_dummy.png`, `s16_lock_final.png`.)
- [x] The lock releases when the target dies, beyond 20 m, and when you die. (Demon killed → released, marker hidden; moved 26 m away → released; player killed → released, mouse look back on.)
- [x] While locked: running in any direction uses the normal animations; standing still you face the target; an attack while running sideways turns you and hits the target, for the client too. (Client holding D around the dummy: facing within 4° of the run direction, ~100° off the target. Click while running 1.57 m away: snapped to 0° off, dummy 90 → 80. Idle and turned 120° away: back to 0° within 0.5 s.)
- [x] A client naming a target out of range doesn't hit it. (Client facing 146° away from the dummy: empty target → miss; dummy named → 100 → 90; the Demon named from 8.5 m → Demon 40 → 40, nothing else hit.)
- [x] Compiles with zero errors, no new warnings; tests pass (72/72, 4 new in `LockOnPickerTests`); client build 0 warnings / 0 errors; host console 0 entries, client log clean.

## Open questions
- None.

## Differences from spec
- While locked the camera also pitches up to 24° (from 17.5°): at the normal pitch your own character hid the middle of the target, the small Demon almost entirely (`s16_lock_demon.png` vs `s16_lock_final.png`). After letting go, the camera keeps its angle until you move the mouse.
- Choosing and the marker use the target collider's centre. The marker is a 22 px gold diamond with a Panel outline on a new `LockOnCanvas` (sort order 1).
- The Esc menu's controls list gained "Right click lock on / let go"; the menu panel widened 560 → 600 px so the lines fit.
- Harness: `KillDummy` passes an empty target to `AttackRpc`.
- Not verified: a real right-click on a windowed client (the harness calls `PlayerLockOn.Toggle`; the `<Mouse>/rightButton` binding was read back), how the camera swing feels with real mouse and keyboard, several enemies close together (only the Demon and the dummy exist), Multiplayer Play Mode.
