# 22 — The starter sword (first pickup)

**Status:** Built and verified 2026-11-07 (Editor, host and two clients through the harness); waits for the user's Multiplayer Play Mode sign-off.
**Design ref:** `docs/design/overview.md`

## Goal
A worn starter sword lies in the test level. Press E next to it to pick it up: it appears in your hand, your attacks become sword attacks that hit harder and reach further, and everyone sees it.

## Decisions (user, 2026-11-07)
- The sword is **an item you pick up**, not held by default. Unarmed = punches and kicks (spec 20).
- **Press E near it**; the prompt reads "E  Pick up Sword". E also sends party invites; when both are in reach, the sword wins.
- **Sword hits harder:** +50% damage and +0.5 m reach over the unarmed attacks (which keep today's numbers).
- **Moves: the hero's Weapon_Combo_2**, cut into three combo hits; the charged heavy is its last big swing, and its wind-up is the charge pose.
- **Look: a worn starter blade.** Dull, chipped, slightly rusty blade; plain bar guard; cloth-wrapped grip. Modelled by `tools/blender/starter_sword.py`.
- Assumed and told to the user: after it's taken, the sword reappears after 20 s so a partner can grab one too. You keep it until you leave the session; no drop key; not saved.

## Scope
- In:
  - Sword model.
  - World pickup (`SwordPickup` prefab, in-scene in TestLevel near the spawn).
  - The held sword under the hero's right hand bone.
  - Two movesets (unarmed / sword: attack definitions + animator controller).
  - Server-checked pickup; the HUD prompt.
- Out: inventory, other items, dropping, saving, durability, equipment slots (the Items and Equipment specs later generalise this).

## Requirements
- Server authority:
  - The server decides the pickup (in reach, available, not already armed).
  - The server owns the armed state and picks the moveset's attack definitions for hit checks and damage.
- Sword stats: `SwordAttack1`–`3` and `SwordHeavy` = the unarmed values × 1.5 damage (rounded) and +0.5 m range; same arcs, cooldowns and stamina; impact delays fit the new clips.
- UI never references Net: the prompt goes through a text channel.

## Networking
- Server-authoritative state:
  - Each player's armed flag (NetworkVariable, server write).
  - Each pickup's available flag (NetworkVariable, server write) and its 20 s return timer.
- Client → server requests: `PickupRpc(pickup)` from the owner.
  - The server checks: within 2.5 m, available, alive, not armed.
- What each client sees:
  - The sword in every armed player's hand; their sword animations.
  - The pickup vanishing and returning.
  - Late joiners get all of it on arrival.

## Done when
- [x] Host and client: E near the sword picks it up. The sword shows in that player's hand on both peers. The pickup vanishes on both and returns after 20 s.
- [x] Armed attacks play the sword moves on both peers and deal ×1.5 damage at +0.5 m reach (measured on the dummy). Unarmed attacks are unchanged.
- [x] Refused (server): picking up from more than 2.5 m away, picking up while it's gone, or picking up while already armed.
- [x] E still invites a nearby player when no sword is in reach.
- [x] A late-joining client sees an armed player's sword and a taken pickup.
- [x] Compiles with zero errors, no new warnings; console clean; tests for any new pure logic.

## Open questions
- Should the sword be lost on death, droppable, or kept between sessions? (Items/Equipment specs.)

## Differences from spec
- **No prefab:** the pickup is a plain scene object in TestLevel, like the training dummy; one pickup doesn't need one.
- **Interact action:** the template's `Interact` (E) had a Hold interaction (~0.4 s); it's a plain press now (nothing else used it).
- **Damage:** the unarmed values × 1.5, rounded half up: sword hits 8 / 9 / 14, heavy 23–38. Ranges 2.5 / 2.5 / 2.7 / 2.9. Impact delays 0.33 / 0.33 / 0.33 / 0.17.
  - Measured on the dummy (Attack stat 5): fists 10 at 1.4 m and 0 at 2.3 m; sword 13 at 2.3 m.
- **Held sword:** posed from the knuckle bones (blade leaves the fist across the knuckles). The library animation's fist isn't angled for a real chop, so on the heavy's strike frame the blade points up. Tune later.
- **Sword model:** exported with Blender's Apply Transform, so the Unity root has no rotation and the blade is +Y.
- **No tests:** no new pure logic.
