# 06 — Enemies and Player Health

**Status:** Verified with the Editor as host and a standalone development build as client. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Core gameplay loop, Combat, Progression)

## Goal
A monster hunts the nearest player and hurts them. Players have health, can die, and come back. Killing the monster gives XP and it returns later: the "survive, get stronger" loop in miniature.

## Scope
- In:
  - Player health: 100 (flat placeholder, not tied to a stat yet).
  - Defense reduces damage taken: `damage taken = max(1, raw − Defense)`.
  - Player death: at 0 HP the player can't move or attack; 5 s later the server restores full HP and returns them to their spawn point.
  - HUD: "HP 80 / 100" bottom-left; "You died. Respawning..." while dead.
  - One enemy (capsule placeholder) in TestLevel: 40 HP, chases the nearest living player within 8 m over a baked NavMesh at 3 m/s, attacks within 1.5 m every 1.2 s for 8 damage, gives 30 XP to its killer, hides at 0 HP and returns at its start point after 10 s.
  - Enemy values in an `EnemyDefinition` ScriptableObject.
  - Player attacks no longer hit other players (players now have health; no friendly fire).
- Out:
  - Ranged or varied enemies, spawners, loot, leashing/returning home, aggro memory, knockback, hit reactions, animations, telegraphed wind-ups.
  - Parry, block, dodge, weak spots, stamina (all undecided in the design doc).
  - Healing, regeneration, death penalties, choosing a respawn point, spectating, difficulty scaling.

## Requirements
- Enemy decisions and all damage to players happen on the server only. Clients see the enemy through its replicated transform and health.
- The NavMesh agent runs on the server only; on clients it is disabled and the server-authoritative NetworkTransform moves the enemy.
- Damage mitigation is plain C# with tests; target choice reuses the existing nearest-player picker.
- Dead players can't act: the owner stops moving, and the server refuses their attacks.

## Networking
- Server-authoritative state: enemy AI, enemy position (NetworkTransform, server authority), all health (NetworkVariable, server write), damage, death, respawn timing.
- Client → server requests: nothing new.
- What each client sees: enemy position and tint, its own HP label.

## Done when
- [x] The enemy approaches the nearest player within 8 m and attacks every 1.2 s; that player loses 8 − Defense HP per hit (5 at level 1). (Client HP fell in steps of 5; the HUD followed.)
- [x] With two players in range, the enemy goes for the nearer one; with nobody in range it stays put. (Both players walked in at the same time to 5.0 m and 7.2 m: it went for the 5.0 m one and left the other at full HP.)
- [x] A player at 0 HP can't move or attack; after 5 s they are back at spawn with full HP. (While dead: W held for 1.5 s did not move the client, an attack that hit the dummy a moment earlier did nothing, HUD showed "You died. Respawning...". Respawn landed exactly on the client's spawn point with 100 HP.)
- [x] Killing the enemy gives its XP to the killer; it returns at its start point after 10 s and hunts again. (Host kill and client kill each gave 30 XP to the killer only; the enemy reappeared at (10, 4) with full HP.)
- [x] Players can't damage each other. (Host attacked the client point-blank: no damage.)
- [x] EditMode tests for mitigation pass; compiles with zero errors, no new warnings. (52 tests.)
- [x] Works for host and connected clients. (Host + 1 client; not run with two clients.)

## Assumptions (placeholders, not design decisions)
- All numbers above (HP 100, enemy stats, 5 s / 10 s timers, the Defense formula) are placeholders.
- No friendly fire is implemented as "player attacks skip anything with `PlayerStats`" until teams/factions exist.
- The enemy reuses the training dummy's tint/hide/return behaviour.

## Differences from spec
- The death test stretched the server's respawn delay to 15 s at runtime (not saved) to have time to try moving and attacking while dead; a normal 5 s respawn was also observed.
- The nearest-player picker moved from `Gameplay/Party` to `Gameplay/Targeting`, since parties and enemies both use it.
- The enemy still reuses the training dummy's tint/hide/return behaviour (a death animation would need its own presentation).
- The NavMesh is baked on `Ground` with the enemy excluded (`NavMeshModifier`, ignore from build); data in `Scenes/TestLevel/NavMesh-Ground.asset`.
- Verified with the Editor as host and a headless Windows development build as client (`tools/mp-harness/`), not Multiplayer Play Mode. Not run with two clients.
