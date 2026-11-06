# 06 — Enemies and Player Health

**Status:** In progress
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
- [ ] The enemy approaches the nearest player within 8 m and attacks every 1.2 s; that player loses 8 − Defense HP per hit (5 at level 1).
- [ ] With two players in range, the enemy goes for the nearer one; with nobody in range it stays put.
- [ ] A player at 0 HP can't move or attack; after 5 s they are back at spawn with full HP.
- [ ] Killing the enemy gives its XP to the killer; it returns at its start point after 10 s and hunts again.
- [ ] Players can't damage each other.
- [ ] EditMode tests for mitigation pass; compiles with zero errors, no new warnings.
- [ ] Works for host and connected clients.

## Assumptions (placeholders, not design decisions)
- All numbers above (HP 100, enemy stats, 5 s / 10 s timers, the Defense formula) are placeholders.
- No friendly fire is implemented as "player attacks skip anything with `PlayerStats`" until teams/factions exist.
- The enemy reuses the training dummy's tint/hide/return behaviour.

## Differences from spec
Filled in when Status is set to Done.
