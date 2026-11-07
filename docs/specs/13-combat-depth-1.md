# 13 — Combat Depth I: Combo, Heavy Attack, Dodgeable Bites, Damage Numbers

**Status:** In progress (2026-11-06): built; verified with the harness except the dodgeable-bite check and a console check (see Done when).
**Design ref:** `docs/design/overview.md` (Combat: "satisfying but difficult", stamina bar)

## Goal
Melee has rhythm and weight: clicks chain into a 3-hit combo, right click spends stamina on a slow heavy blow, the Demon's bite can be dodged by stepping or rolling away, every hit shows a number, and the attacker feels the hit land.

## Scope
- In:
  - **3-hit combo** (left click): each click within 1.0 s of the previous swing continues the chain (1 → 2 → 3 → 1). Step 1 is today's swing; step 2 is faster; step 3 is a heavier finisher. Each step is its own `AttackDefinition` (damage, range, arc, cooldown, impact delay) and its own animation (Warrior: `Sword_Attack`, `Sword_AttackFast`, `Punch`; the other looks map to their own clips).
  - **Heavy attack** (right click): costs 25 stamina (server-checked), slower wind-up (impact at ~0.6 s), 15 damage, wider reach; resets the combo.
  - **Dodgeable bites**: the server re-checks the Demon's reach when the bite lands (1.3 × its attack range); stepping or rolling away makes it miss.
  - **Damage numbers** on every peer: the amount pops up above whoever lost health and floats away (warm white on monsters, Danger red on players); world-space TextMeshPro, pooled per character, facing the camera.
  - **Hit-stop on the attacker**: when the server confirms your hit landed, your character's animation freezes for 60 ms (your screen only; game time is untouched).
- Out:
  - Knockback, parrying, weak spots, stealth, lock-on, per-class combos (spec 18 swaps the definitions per class), crits.

## Requirements
- Combo state and the heavy attack's cost are server-side; the client only sends "light" or "heavy" intent.
- The combo chain is a plain C# class with tests; impact delays are tuned per clip.
- Damage numbers and hit-stop are presentation from replicated health and a server → owner confirmation.

## Networking
- Server-authoritative state: combo step, cooldowns, stamina cost, targets, damage, bite reach at impact.
- Client → server requests: `AttackRpc()` (light), `HeavyAttackRpc()` (heavy).
- Server → clients: the attack animation with its combo step (every peer); `HitConfirmRpc` to the attacking owner (hit-stop).
- What each client sees: everyone's combo and heavy animations, damage numbers on every damaged character, its own hit-stop.

## Plan (files)
- Data: `AttackDefinition` + stamina cost; assets `ComboAttack2`, `ComboAttack3`, `HeavyAttack` (`BasicAttack` is step 1).
- Gameplay: new `Combat/ComboChain`.
- Net: `PlayerAttack` (chain, heavy, hit confirm), `CharacterAnimator` (combo step, heavy trigger, hit-stop), `EnemyAI` (reach re-check), new `DamageNumbers`; Net asmdef + `Unity.TextMeshPro`.
- Assets: input action `HeavyAttack` (right mouse); animator `Combo` int + `Heavy` trigger + states `Attack2`, `Attack3`, `Heavy`, class overrides; `Player.prefab`; Enemy and TrainingDummy `DamageNumbers` in TestLevel.
- Tests: new `EditMode/ComboChainTests`.

## Done when
- [x] Host and client: three clicks play steps 1 → 2 → 3 on every peer with rising damage; a pause longer than the window restarts at 1. (Host: damage 10 → 11 → 14 (5/6/9 + Attack stat 5), then 10 after a 1.5 s pause; the client saw the host's `Attack > Attack2 > Attack3 > Attack`. Client's own chain: 10 → 11 → 14.)
- [x] Right click: heavy animation on every peer, 15 + Attack damage, costs 25 stamina; refused below 25. (Host: `Heavy` seen on the client, 20 damage, 100 → 75; refused at 10 stamina: no animation, nothing spent. Client's own heavy: 20 damage, stamina spent.)
- [ ] A Demon bite whose target stepped or rolled out of reach misses; one that stays in reach hits. **Not verified yet**: the first attempt never got a bite (the test's moves didn't put the host in reach); a debug run right after showed the Demon chasing and biting normally.
- [ ] Damage numbers show the right amount over the dummy, the Demon and players, on host and client. (Dummy only so far: the client saw `10, 11, 14, 10, 20` for the host's hits and the right numbers for its own. Not yet seen: numbers over the Demon and over players, and a capture of how they look.)
- [x] The attacker's animation pauses ~60 ms when its hit lands (owner only). (Animator speed 0 seen at each light hit, on the host for its hits and on the client for its own.)
- [x] Compiles with zero errors, no new warnings; tests pass (64/64, 4 new in `ComboChainTests`); client build 0 warnings / 0 errors. **Console:** the spec 13 play session ended with 7 Multiplayer-session warnings, all from `SessionController` (the online-session path: a session request while Netcode was already running from the direct connect). Two later host + client sessions using the same combat code (specs 14 and 15, direct connect, combos, heavies, damage numbers, hit-stop) logged **0** console entries, so they came from that test run, not from the game code.

## Open questions
- None blocking. Numbers are starting points (in the attack assets).

## Differences from spec
Filled in when Status is set to Done.
