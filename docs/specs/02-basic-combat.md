# 02 — Basic Combat

**Status:** Verified with the Editor as host and a standalone development build as client. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Combat)

## Goal
A player presses attack near a training dummy and the dummy loses health. Damage is decided by the server, and every player sees the result.

## Scope
- In:
  - One melee attack on left mouse (`Player/Attack` in the existing input actions).
  - Attack values in a ScriptableObject: damage 10, range 2 m, 90° arc in front, 0.5 s cooldown.
  - A training dummy in TestLevel: 100 health, tints toward red as it loses health, disappears at 0, comes back at full health after 3 s.
- Out:
  - Player health, enemies that fight back, AI, stats, combos, parry, weak spots, stamina.
  - Animations, VFX, sound, health bars or any HUD.
  - Lag compensation (the server uses its own view of positions).

## Requirements
- Range/arc check and damage math are plain C# in Gameplay and unit-tested.
- The client sends intent only: an attack RPC with no parameters, owner-only.
- The server enforces the cooldown and does the hit check with its own transforms.

## Networking
- Server-authoritative state: dummy health (NetworkVariable, server write), cooldown, hit detection, respawn.
- Client → server requests: "attack" (no payload, owner of the attacking player only).
- What each client sees: dummy tint and visibility, driven by the health NetworkVariable.

## Done when
- [x] Host clicks next to the dummy (facing it): dummy health drops by 10 per hit, tint shifts toward red. (Verified on the host through the attack RPC; tint seen in the Game view.)
- [x] Clicking faster than the cooldown doesn't deal extra damage. (Host: 3 calls in one frame = 1 hit.)
- [x] Missing (out of range or facing away) deals no damage. (Host.)
- [x] At 0 health the dummy disappears and comes back at full health 3 s later. (Seen on host and client: hidden at 0, back at 100 exactly 3.0 s later.)
- [x] Host + client: a client's hit damages the dummy, and both see the same health and visibility. (A real simulated left click from the client landed; ten client attacks killed the dummy and both sides logged the same sequence.)
- [x] Compiles with zero errors, no new warnings.
- [x] EditMode tests for damage math and reach.

## Assumptions (made when writing this spec; change if wrong)
- Values above are placeholders until spec 03 (stats).
- Dummy max health is a serialized field on the dummy, not an enemy definition asset (no enemy defs exist yet).

## Differences from spec
- `[Rpc(..., RequireOwnership = true)]` is obsolete in NGO 2.13; `PlayerAttack` uses `InvokePermission = RpcInvokePermission.Owner` (this showed up as a build warning, not an Editor compile warning).
- Spec 03 changed the numbers: the move's damage is 5 and the Attack stat (5 at level 1) is added, so level-1 damage is still 10. `NetworkHealth.ServerTakeDamage` now returns whether the hit killed the target.
- Verified with the Editor as host and a headless Windows development build as client (`tools/mp-harness/`), not Multiplayer Play Mode.
