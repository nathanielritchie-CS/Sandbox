# 15 — Charged Heavy Attack and Weighty Swings

**Status:** Built and verified (2026-11-06) with the Editor host + a headless client (harness); waiting for your Multiplayer Play Mode sign-off before Done (networked: charge timing, damage and stamina are server-owned).
**Design ref:** `docs/design/overview.md` (Combat: stamina bar, "satisfying but difficult")

## Goal
Holding left click after a swing charges a heavy blow that hits harder the longer you hold, and swinging or charging slows you down so attacks have weight. Right click is freed for lock-on (spec 16).

## Scope
- In:
  - **Press left click**: the light combo swing, exactly as today (no added delay).
  - **Keep holding**: 0.45 s after the press you start charging: a combat stance on every peer (Warrior/Wizard/Ranger/Monk `Idle_Attacking`, Rogue `Attacking_Idle`, Cleric `Attack_Idle`), and you move at 30% speed.
  - **Ready** 0.35 s into the charge (0.8 s after the press): a small gold spark on your character (your screen only). Releasing now hits for 15 + Attack.
  - **Full** 0.75 s into the charge (1.2 s after the press): a bigger spark. Releasing hits for 25 + Attack. Between ready and full the damage rises linearly; holding longer adds nothing.
  - Releasing before ready cancels the charge (nothing spent).
  - The heavy costs 25 stamina on release; with less than 25, holding doesn't charge. Rolling or dying cancels a charge. Hits don't interrupt it.
  - **The release swing** is the Heavy animation at normal speed; the hit lands 0.36 s after release. It resets the combo.
  - **Weighty swings**: while an attack, heavy or charge animation plays you move at 30% speed, so the feet don't skate.
  - Right click does nothing until spec 16.
- Out:
  - A charge sound (no fitting placeholder clip yet), knockback, per-class charge timing, charging while rolling, gamepad.

## Requirements
- The server times the charge with its own clock (from the charge request to the release) and computes the damage; the client only says "started charging" and "released". 50 ms of grace covers network jitter.
- Charge damage comes from a plain C# `ChargeLevel` with tests; the numbers live in `HeavyAttack.asset`.
- Slowdown and facing stay owner-side presentation: movement is owner-authoritative and the server's speed check only caps speed.

## Networking
- Server-authoritative state: charge start time, ready/full thresholds, damage, stamina cost, cooldowns, the `Charging` animator flag for the other peers.
- Client → server requests: `AttackRpc()` (press, unchanged), `ChargeRpc()` (the hold passed 0.45 s), `ReleaseChargeRpc()` (released while charging), `CancelChargeRpc()` (the owner dropped the charge for a reason the server can't see, e.g. the Esc menu switched the input off).
- What each client sees: everyone's stance and heavy swing and the damage numbers; its own ready/full sparks and its own slowdown. The owner shows its stance at once; the server shows it to everyone else (like the roll).

## Plan (files)
- Data: `AttackDefinition` + max damage, charge ready/full seconds; `HeavyAttack.asset` (15–25, 0.35/0.75 s, impact 0.36, stamina 25).
- Gameplay: new `Combat/ChargeLevel`.
- Net: `PlayerAttack` (hold → charge → release, server timing; the right-click heavy input goes away), `CharacterAnimator` (Charging flag, sparks, "busy" query), `PlayerMovement` (30% speed while busy; `IsRolling` for the owner).
- Assets: `PlayerCharacter.controller` (`Charging` bool, `Charge` state, Heavy speed 0.6 → 1, an "Attack" tag on the attack states), the five class overrides (stance clips), `Player.prefab` (sparks reference).
- Tests: new `EditMode/ChargeLevelTests`.

## Done when
- [x] Host and client: a click still swings at once; holding shows the stance on every peer and slows you; release at full charge deals 25 + Attack, at ready 15 + Attack, in between in proportion; release before ready deals nothing extra and costs nothing; stamina −25 per heavy. (Host, 1.25 s hold: light 10 at 0.36 s, stance at 0.6 s, heavy 30; both spark bursts (34 particles); the client saw `Attack > Charge > Heavy > Idle`. Client: full hold → heavy 30 (server timed the charge at 0.82 s), 0.95 s hold → 25, 0.6 s hold → light 10 only, no stamina spent; stamina 100 → 75 per heavy; the host saw the client's `Attack > Charge > Heavy`.)
- [x] Holding with < 25 stamina doesn't charge; a roll during a charge cancels it (no heavy on release). (Stamina held at 0 by the server: the owner never charged, the server never saw a request, only the light landed. Roll at 0.75 s: owner and server charge ended at the roll, stamina −30 for the roll only, nothing landed on release. Opening the Esc menu mid-charge also cancelled it on the server.)
- [x] Swinging slows movement to ~30%; walking resumes at full speed after the swing. (Client holding W, logged every frame: ~4.0 m/s walking, ~1.2 m/s from the swing through the charge and the heavy, ~4.0 m/s again right after.)
- [x] A client can't skip the charge time (a direct `ReleaseChargeRpc` right after `ChargeRpc` is refused). (Immediately and after 0.2 s: no heavy, no stamina spent.)
- [x] Compiles with zero errors, no new warnings; tests pass (68/68, 4 new in `ChargeLevelTests`); client build 0 warnings / 0 errors; host console 0 entries for the whole host + client session, client log clean.

## Open questions
- None. Numbers are starting points (in the attack asset and `PlayerAttack`).

## Differences from spec
- Added `CancelChargeRpc()`: the Esc menu switches the input off mid-charge, which the server can't see; the owner tells it (also sent on a roll, harmlessly).
- The stance clips don't loop (1.67 s); a charge held longer keeps the last pose. Full charge comes at 1.2 s, inside the clip, so the import settings were left alone.
- The ready/full cue reuses the `HitSparks` emitter on the player, recoloured gold per burst (10 / 24 particles); no sound.
- The stance cuts the light swing's follow-through at 0.45 s, after its hit (0.36 s).
- Harness note: holds are timed in real seconds while the owner's 0.45 s threshold uses game time, and the client's game clock stalls while an eval starts; the first test hold therefore charged short (25 instead of 30). A repeat with both clocks logged gave 30.
- Not verified: a real mouse hold on a windowed client (the harness calls the owner's `Press`/`Release`), how the sparks look (particle counts only), the stance clips of the four classes not seen in a session, Multiplayer Play Mode.
