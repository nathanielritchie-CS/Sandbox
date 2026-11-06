# 03 — Stats, XP and Status Window

**Status:** Verified with the Editor as host and a standalone development build as client. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Progression, "What makes the player special")

## Goal
Every player has a level, XP and stats. Killing the training dummy gives XP, XP raises the level, the level raises the stats, and the player opens a status window (C) to see it all.

## Scope
- In:
  - `StatsDefinition` ScriptableObject: XP cost per level step, and a base value + per-level gain for each stat.
  - Stats: Attack, Defense, Dexterity, Stealth, Luck, Mana, Stamina (the design doc's candidate list).
  - Level/XP math and stat-at-level math as plain C# in Gameplay.
  - `PlayerStats` on the player: XP is a server-write NetworkVariable; level and stats are derived from it.
  - The dummy awards 50 XP to the player who lands the killing hit.
  - Attack stat adds to attack damage (move damage 5 + Attack 5 = 10 at level 1, so level-1 combat is unchanged from spec 02).
  - Status window (uGUI, legacy Text like the connection menu): hidden by default, C toggles it, shows the local player's level, XP progress and stats, updates live.
- Out:
  - Stat point allocation, class differences, buffs/modifiers.
  - Any effect from stats other than Attack.
  - Player health, damage taken, defense effect.
  - Saving XP between sessions, party XP sharing, showing other players' stats.
  - Rebinding the key, styling, TextMeshPro.

## Requirements
- Level/XP/stat math is plain C# in Gameplay with EditMode tests.
- Only the server changes XP. Clients send nothing about XP or damage.
- The window gets its data from a Core event channel (UI cannot reference Net).

## Networking
- Server-authoritative state: XP, who gets kill XP, final damage (includes the Attack stat).
- Client → server requests: nothing new (still just "attack").
- What each client sees: its own status window. Everyone's XP is replicated (readable), but only the local player's is shown.

## Done when
- [x] Host kills the dummy: +50 XP. Two kills reach level 2 (100 XP): Attack 5 → 6, damage per hit 10 → 11. (Host: 50 XP, then level 2 with the window showing Attack 6; the next hit took the dummy 100 → 89.)
- [x] A client's kill gives the client XP and leaves the host's XP unchanged. (Client went to 50 XP at level 1 while the host stayed at level 2 / 100 XP, and each side's window showed its own player.)
- [x] C toggles the window for the local player; it shows level, XP progress and stats, and updates when XP changes. (C key on the client: closed → open → closed; layout checked in a Game view capture on the host.)
- [x] EditMode tests for level/XP/stat math pass. (8 new, 27 total.)
- [x] Compiles with zero errors, no new warnings. (Editor and player build.)
- [x] Works for host and a connected client. (A rejoining client starts again at level 1 / 0 XP; XP is not saved.)

## Assumptions (placeholders, not design decisions)
- The design doc leaves the final stat list, how stats grow, and what the window shows undecided. This spec uses the candidate list, a flat per-level gain, and level + XP + stats. All numbers are in `DefaultStats.asset` and easy to change.
- Going from level L to L + 1 costs `XpPerLevel × L` XP (100 → 200 → 300 ...). No level cap.
- Legacy uGUI `Text`, not TextMeshPro, to match the connection menu and avoid importing TMP Essentials for placeholder UI.

## Differences from spec
- The toggle key is a serialized `InputAction` on `StatusWindow` (`<Keyboard>/c`), not an entry in `InputSystem_Actions`, to avoid editing the shared actions asset.
- The kill XP is awarded in `PlayerAttack` (from an `XpReward` component on the target); `NetworkHealth.ServerTakeDamage` returns whether the hit killed.
- Unity added `PlayerStats` to the player prefab automatically because `PlayerAttack` has `[RequireComponent(typeof(PlayerStats))]`.
- The panel is taller than its text needs (cosmetic).
- Verified with the Editor as host and a headless Windows development build as client (`tools/mp-harness/`), not Multiplayer Play Mode.
