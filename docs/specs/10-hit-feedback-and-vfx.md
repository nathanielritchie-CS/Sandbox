# 10 — Hit Feedback and VFX

**Status:** Built and verified (2026-11-06) with Editor host + headless client (harness), timed captures, machine scans and the visual scorecard; waiting for the user's Multiplayer Play Mode sign-off before Done (networked: the server's damage timing changed).
**Design ref:** `docs/design/overview.md` (Combat, Art style: "glowing elements")

## Goal
Every hit, death and level-up visibly answers the player within 100 ms, on every peer, and a swing's damage lands when the weapon connects instead of at the wind-up.

## Scope
- In:
  - **Hit flash + sparks** on anything with health (players, the enemy, the training dummy): a 0.08 s warm flash (`_BaseColor` → Sunlight × 2.5 through a MaterialPropertyBlock) and a burst of 10 palette sparks (Sunlight/Accent); 28 on death. One reused ParticleSystem per character (`Emit`), no Instantiate per hit.
  - **Hits timed to the swing**: the server applies a swing's damage after an impact delay (`AttackDefinition.ImpactDelay` for players, `EnemyDefinition.AttackImpactDelay` for the enemy), tuned by capture to the Warrior's sword and the Demon's bite.
  - **Health bar animates**: the HUD fill tweens over 0.3 s (OutCubic) instead of snapping.
  - **Level-up moment** (game-feel milestone recipe): on the leveling player's HUD, an accent flash (alpha 0.6 → 0 in 0.1 s) and a "LEVEL n" banner (pop in 0.35 s OutBack at 50 ms, out at 1.5 s); on that player's character, on every peer, 40 confetti particles (Accent, Sunlight, Sky).
  - **Camera shake** when the local player takes damage (Cinemachine Impulse: source on the camera rig, listener on the follow camera).
  - `Isekai.Core.Ease` (OutCubic, OutBack, coroutine tween on unscaled time) with EditMode tests.
  - Cleanup found by the scan: the Wizard model's empty (0-vertex) MeshRenderer uses URP's default `Lit.mat`; give it `Wizard_Texture`.
- Out:
  - Audio (the floor's "audible" half; a separate spec).
  - Global hit-stop: `Time.timeScale` on the host would slow the authoritative simulation (AI, cooldowns, physics, the movement validator) for every player.
  - Damage numbers, per-class impact timing (all classes share the Warrior's delay; the Ranger's 1.25 s bow draw will look early), dodge windows, a poof when the Demon's model hides 2.5 s after death.

## Requirements
- Presentation reacts to already-replicated state (`NetworkHealth`, the XP NetworkVariable); no new RPCs or NetworkVariables.
- Targets are still chosen when the swing starts, with the same range/arc/friendly-fire checks; only the moment damage is applied moves. Whether attacks can be dodged belongs to the undecided combat depth.
- Every effect color comes from `Isekai.Core.Palette`; particles use one project material (`Sprites/Default` + a generated soft dot), never a default material.
- UI only reads Core channels.

## Networking
- Server-authoritative state: unchanged (health, XP). The server applies a swing's damage `ImpactDelay` seconds after accepting it, only if the attacker and the target are still alive.
- Client → server requests: unchanged (`PlayerAttack.AttackRpc`).
- What each client sees: flash, sparks, death bursts and level-up confetti for every character, driven by replicated health and XP; its own HUD level-up moment, health-bar tween and camera shake.

## Plan (files)
- Core: new `Ease`.
- Data: `AttackDefinition` + `ImpactDelay`, `EnemyDefinition` + `AttackImpactDelay` (values set in `BasicAttack.asset` / `BasicEnemy.asset`).
- Net: new `HitFeedback`; `PlayerAttack` and `EnemyAI` apply damage after the delay; `PlayerStats` plays an optional level-up burst.
- Gameplay: new `Cameras/DamageShake`.
- UI: `HealthBar` tween; new `LevelUpMoment`.
- Tests: new `EditMode/EaseTests`.
- Assets: `Art/Placeholder/VFX/SoftDot.png` + `Particle.mat`; `Player.prefab` (HitFeedback + HitSparks, LevelUpBurst, Wizard material); TestLevel (Enemy and TrainingDummy HitFeedback + HitSparks, `LevelUpCanvas`, impulse listener on `FollowCamera`, impulse source + `DamageShake` on `CameraRig`).

## Done when
- [x] Host and client: hitting the dummy and the enemy shows a flash and sparks at the weapon's contact, a player hit by the enemy flashes and sparks, deaths burst. (Client saw the host's hits: dummy 14 sparks + flash, Demon 14 sparks + flash. Client's own player bitten: 14 sparks + flash. Host's player bitten: flash + sparks in the 200 ms frame. Dummy death: 32 sparks, cleared within 0.9 s.)
- [x] Damage lands at the contact. (Host: the Demon's flash starts 0.36 s after the swing, when the blade crosses it in the timed captures; the Demon's bite lands ~0.25 s in, at full lunge.)
- [x] Level-up: flash and banner on the leveling player's HUD; confetti on that player's character on host and client. (Host: "LEVEL 2" at scale 1.06 mid-overshoot, 40 confetti, banner gone by 1.8 s. Client levelled: its banner showed, and the host saw 40 confetti on the client's character.)
- [x] Health bar fill animates and settles on the right value. (Host: 1.00 → 0.82 → 0.71 → 0.64 → 0.61 → 0.60 over ~0.3 s; client: 0.99 → 0.95.)
- [x] The camera shakes when the local player is hit. (Camera moved 0.37 m from rest at force 0.35 on the host, 0.26 m at the final force 0.25 on the client; 0 jitter at rest.)
- [x] Pooling: the ParticleSystem count is unchanged after 10 hits. (4 → 4 after 20+ hits and two deaths.)
- [x] Machine scan: no default materials on particles or on any player variant; particle start colors from the palette. (0 problems: 4 systems on `Particle.mat`, none play on awake or loop; 0 default materials across all six variants and the scene; spec 09 UI scan still 0 with the new `LevelUpCanvas`.)
- [x] Visual scorecard: feedback visibility above 3; all six axes at 3 or better (board below).
- [x] Compiles with zero errors, no new warnings; all EditMode tests pass (56/56, 4 new); console clean; client build runs (0 warnings / 0 errors); host + client verified.

## Visual scorecard
Frames: the gameplay camera at the sword's contact on the Demon (`s10_score_busy.png`), the level-up moment (`s10_levelup.png`), timed swing/bite sheets; the close-up is unchanged from spec 09. Captures in `Isekai/Temp/harness/s10_*.png` (not in git).
- palette discipline: 4/5 — scans clean; sparks Sunlight/Accent, confetti Accent/Sunlight/Sky, flash Sunlight, banner Accent with a Panel shadow; one project particle material; no default material on any of the six player variants.
- style uniformity: 4/5 — soft round sparks and confetti and the rounded banner fit the storybook look; the dummy and stone blocks are still primitive props.
- lighting depth: 4/5 — unchanged; the 80 ms flash's bloom is the single glow accent in frame.
- silhouette: 4/5 — unchanged.
- ui intentionality: 4/5 — the level-up banner uses the title treatment, the HP bar animates instead of snapping; over the bright meadow the banner leans on its shadow for contrast.
- feedback visibility: 4/5 (was 3) — the busy frames show the game answering at the contact: the struck Demon or dummy flashes and sparks, the HP bar tweens down, a level-up gets a banner and confetti; still silent (no audio), no damage numbers, and the Demon's model still vanishes 2.5 s after death without a poof.

Fix pass (from captures): impact 0.30 → 0.36 s so damage meets the blade; sparks 0.08–0.15 → 0.16–0.3 m, 10 → 14 per hit, 28 → 32 per death (they were specks at gameplay distance); shake force 0.35 → 0.25.

## Open questions
- None blocking. Impact delays are tuned by capture (Warrior sword, Demon bite).

## Differences from spec
- Tuned values: `BasicAttack.ImpactDelay` 0.36 s (the Warrior's blade crosses the target at ~360 ms), `BasicEnemy.AttackImpactDelay` 0.25 s, sparks 0.16–0.3 m / speed 3–5.5 / life 0.35–0.6 s, 14 per hit and 32 per death, shake force 0.25 (Bump, 0.2 s, uniform). The flash stays at Sunlight × 2.5: 4× was only slightly stronger on the Demon's deep-red texture.
- `PlayerAttack` now keeps each swing's targets in its own set (the shared `_hit` field is gone) so a pending strike can't be overwritten.
- Editor side effect: Unity re-serialized seven Quaternius character materials (`_MainTex` synced to the same texture as `_BaseMap`) when `Player.prefab` was re-saved. No visual change.
- Not verified: confetti on the *host's* character as seen from the client (the client → host direction was verified; same code path); Wizard/Ranger/Cleric/Monk timing (needs 3+ players). The Ranger's 1.25 s bow draw will look early with the shared 0.36 s impact.
- Not done (as scoped): audio, global hit-stop, damage numbers, a poof when the Demon's model hides.
