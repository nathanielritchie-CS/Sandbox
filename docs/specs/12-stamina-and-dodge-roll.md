# 12 — Stamina and Dodge Roll

**Status:** Built and verified (2026-11-06) with the Editor host + a headless client (harness); waiting for your Multiplayer Play Mode sign-off before Done (networked: stamina, roll and invulnerability are server-owned).
**Design ref:** `docs/design/overview.md` (Combat: stamina bar; "satisfying but difficult")

## Goal
Players have a stamina bar under their health. Sprinting drains it, and Space performs a dodge roll that costs a chunk of it and briefly makes the player untouchable, so a Demon bite can be rolled through.

## Scope
- In:
  - **Stamina** per player, owned by the server: max 100; sprinting drains 20/s; a roll costs 30; refills at 35/s after 0.8 s without spending. Numbers live in a ScriptableObject.
  - **Sprint needs stamina**: the owner sprints only while its stamina is above 0; at 0 it drops to walk speed until the bar refills.
  - **Dodge roll** (Space): ~4.5 m in 0.45 s in the movement direction (facing direction if no input), with the existing `Roll` clip on every peer and a whoosh. Refused while dead, mid-roll or below 30 stamina. Attacks are ignored mid-roll; a roll may cut short your own attack's recovery.
  - **Invulnerability**: the server ignores damage to a rolling player from 0.05 s to 0.35 s after the roll starts.
  - **HUD**: a stamina bar under the HP bar (palette colors, same tween as HP).
  - Tests for the stamina math.
- Out:
  - Heavy attacks (spec 13 spends stamina), the Stamina stat raising max stamina (spec 14), jump, stamina for light attacks, rolling through enemies, lock-on.

## Requirements
- Stamina logic is a plain C# class in Gameplay (spend, drain, refill after a delay, clamp); the NetworkBehaviour stays thin.
- The HUD reads stamina from a Core channel (UI can't reference Net).
- Movement stays owner-authoritative; the server's existing speed check still catches cheating (a roll at ~10 m/s fits under its sprint × 1.5 = 10.5 m/s tolerance; at 0 stamina the check uses walk speed).

## Networking
- Server-authoritative state: stamina (NetworkVariable, server-write); roll acceptance and its invulnerability window; the roll animation broadcast.
- Client → server requests: `RollRpc(direction)` (intent). Sprinting isn't sent: the server drains stamina from the movement speed it already measures.
- What each client sees: every player's roll animation and whoosh; its own stamina bar. The owner moves through its roll immediately (if its replicated stamina allows); if the server refuses (rare desync), there are no invulnerability frames and no stamina cost, and the position is still legal.

## Plan (files)
- Data: new `MobilityDefinition` (stamina and roll numbers) + asset.
- Gameplay: new `Movement/StaminaPool` (plain C#).
- Net: `PlayerMovement` (roll motion, sprint gate, server drain from measured speed, walk-speed check at 0 stamina); a new `PlayerStamina` NetworkBehaviour (NetworkVariable + server tick + HUD report) unless it fits cleanly in `PlayerMovement`; `NetworkHealth` (invulnerable window); `CharacterAnimator` (Roll trigger); `PlayerAttack` (ignore requests mid-roll).
- Core/UI: a stamina channel + `StaminaBar` (or `HealthBar` reused if the death label can stay optional).
- Assets: `Player.prefab`; the Roll state in `PlayerCharacter.controller` (the class overrides inherit it); the input actions asset (Space → Roll); TestLevel HUD.
- Tests: new `EditMode/StaminaPoolTests`.

## Done when
- [x] Host and client: sprinting drains the bar; at 0 the player walks; the bar refills after the pause. (Client held W+Shift: 7.0–7.2 m/s, stamina 69 → 19 in 10-point steps every 0.5 s (20/s), refill after ~0.75 s at ~36/s back to 100, as seen by the host. Host: 100 → 67 over a 2 s sprint, back to 100 within 2 s.)
- [x] Host and client: Space rolls ~4.5 m with the Roll animation on every peer, costs 30, and is refused below 30. (Client Space press: the host saw `Rogue:Roll`, stamina 100 → 70. Client-side roll: own Roll animation, 4.50 m, HUD fill 0.70. Host: 4.54–4.63 m, `Warrior:Roll`, 100 → 70. A roll request at 0 and at 16 stamina: refused.)
- [x] A Demon bite landing during the invulnerable window does no damage; a bite outside it still hits. (Host, real bites: during a roll 85 → 85, the next bite 85 → 80. Server checks on both players: a hit during the roll did nothing (100 → 100), the same hit after the roll landed.)
- [x] A client can't sprint at 0 stamina or get free rolls. (Cheat client moving at 7 m/s without the gate: stamina ran to 0, then 2 snapbacks; direct `RollRpc` at 0 stamina: refused, the host saw no invulnerability and no roll.)
- [x] Stamina bar on the HUD for host and client, palette colors, tweened. (`s12_hud.png`: Meadow fill on a Shadow track under the HP bar; client HUD fill followed its stamina.)
- [x] Compiles with zero errors, no new warnings; tests pass (60/60, 4 new in `StaminaPoolTests`); console clean; client build 0 warnings / 0 errors; host + client verified.

## Open questions
- None. Numbers are in `Data/Movement/Mobility.asset`, tune by feel.

## Differences from spec
- Sprint drain is billed from the **ground the server sees the player cover** each 0.5 s (turns included), not the straight-line distance: the first version under-charged anyone who changed direction (an 8 s zig-zag sprint cost 44 instead of 100). The speed limit itself is unchanged from spec 01 (straight-line per window), so a zig-zagging cheater at 0 stamina can still beat the walk-speed limit; noted, not fixed.
- After running dry, sprinting waits until stamina is back to 20 (`SprintRestart`), so the bar doesn't stutter at 0.
- Invulnerability is 0.3 s from the moment the server accepts the roll (no 0.05 s lead-in: network delay already provides one).
- The stamina HUD reuses `HealthBar` (its HP label became optional) and the `(current, max)` `HealthEventChannel` type (asset `PlayerStaminaChannel`).
- The template input action "Jump" (Space) was renamed "Roll" (references follow the action's id).
- The roll whoosh reuses the `swing` clip at half volume.
- Added with it (small fix): invisible walls just outside the 120 m meadow (`WorldEdge` in TestLevel). The test client walked off the edge twice; players now stop at 59.4 m.
- Not verified: Multiplayer Play Mode; the other four classes' roll clips in a session (they're mapped; only Warrior and Rogue were seen); how the roll feels with real keyboard input on a focused window (the harness's simulated keys reach the headless client late).
