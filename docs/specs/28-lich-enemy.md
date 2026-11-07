# 28 — The Lich: the user's model as a second enemy

**Status:** Phase A built and verified 2026-11-07 (Editor host + one headless client through the harness); waits for the user's Multiplayer Play Mode sign-off. Phase B (the bolt) not started.
**Design ref:** `docs/design/overview.md` (Core gameplay loop, Combat)

## Goal
The user's Lich floats in TestLevel beside the Demon: it glides toward you, plays its cast, flinches when hit, dies and comes back, all with its own animations.

## Decisions (user, 2026-11-07)
- Source: the user's `Lich.blend` (Meshy "Crystal Wraith", custom 49-bone rig with robe/sleeve/staff/wisp bones, one action `Float_Glide`, texture packed in). Moved to `art-source/Lich.blend` (outside `Assets/`, so Unity doesn't import the .blend through Blender); the texture (identical to the packed one) moved to `Art/Characters/Lich/`.
- **Added beside the Demon** (the Demon stays), as a prefab.
- **Player height, ~1.8 m** (the model is 0.9 m as made: import scale ×2), hovering.
- **Animations scripted in Blender** (headless, like the Otherworlder).
- **Phase A (this spec): model and animations only.** The cast does nothing: no bolt, no damage. The ranged bolt is phase B (later, its own go).

## Scope
- In:
  - `tools/blender/lich.py` (Blender 5.1, headless): opens `art-source/Lich.blend`, keeps `Float_Glide` as **Idle**, adds **Glide** (forward lean, trailing robe), **Cast** (staff raised, then thrust), **Hit** (flinch back) and **Death** (recoil, crumple, sink), all layered on the floating loop; exports `Art/Characters/Lich/Lich.fbx` (Generic rig) and contact-sheet renders.
  - `Lich.mat` (URP Lit, the texture) and `Lich.overrideController` on the Demon's `EnemyDemon.controller` (same states and parameters, Lich clips).
  - `Data/Enemies/Lich.asset` (an `EnemyDefinition`): attack damage 0.
  - `EnemyAI`: damage 0 means the attack is only an animation (no damage roll).
  - `Prefabs/Lich.prefab` (the Demon's component set with the Lich model), one in TestLevel.
- Out (phase B or later): the bolt, keeping distance/backing off, facing the target while casting, a spawner, loot, a spell sound.

## Numbers (placeholders)
| | Lich | (Demon) |
|---|---|---|
| HP / XP | 50 / 45 | 40 / 30 |
| Aggro range | 12 m | 8 m |
| Move speed | 2.5 m/s | 3 m/s |
| Attack range (stops at 80%) | 9 m | 1.5 m |
| Cooldown | 2.2 s | 1.2 s |
| Damage | 0 (cast is animation only) | 8 |
| Respawn | 10 s at its home | 10 s |

## Requirements
- Reuses the existing enemy pipeline (`EnemyAI`, `NetworkHealth`, `CharacterAnimator`, `HitFeedback`, `XpReward`, `Respawn`, lock-on); no new systems.
- The Demon's behaviour doesn't change.
- Cosmetic only: the collider, agent and timings don't depend on the model.

## Networking
- Nothing new. Same as the Demon: server AI and NavMesh agent, server-authority NetworkTransform, replicated health; animation from the replicated speed/health and the attack RPC.

## Done when
- [x] The Lich (~1.8 m, textured) floats in TestLevel next to the Demon and idles, on host and client. (Both peers: at (−10, 12), 50/50 HP, `Loco(Idle)`, mesh from 0.18 m to ~2 m above the ground.)
- [x] It glides toward a player within 12 m (Glide plays), stops ~7 m away and plays Cast every 2.2 s, on both peers; nobody takes damage. (Watched from the client: Idle at 15.6 m → Glide → Cast at 9.2 m → stops at 6.9 m → Cast again 2.2 s later, facing the host; host HP stayed 100.)
- [x] Hitting it plays Hit, flash and sparks; killing it plays Death, hides it, gives 45 XP, and it returns home after 10 s. (Client saw Hit at 40/29/15/5 HP, collider off and Death at 0, hidden 2.3 s later, back home with 50 HP 10.3 s after the kill; the host got 45 XP. Flash and sparks are the unchanged `HitFeedback`, not measured separately.)
- [x] The Demon still chases and bites as before. (Host next to it: 95, 90, 85, 80 every 1.2 s.)
- [x] Contact sheet of Idle, Glide, Cast, Hit, Death; captures of the Lich in TestLevel. (`Isekai/Temp/harness/s28_lock.png`: locked on from ~7 m.)
- [x] Compiles with zero errors, no new warnings; console clean on host and client; existing tests pass. (88/88 EditMode; client build 0 warnings / 0 errors; host console and client log 0 errors / 0 warnings.)

## Open questions
- Phase B: the bolt (drafted 2026-11-07 and set aside: server-flown sphere-cast bolt, RPC visuals, 9 m cast range, backs off inside 4 m, waits for line of sight, faces its target).

## Differences from spec
- **Death** sinks the Lich right into the ground (recoil, slight slump, then down ~1.8 m) instead of crumpling: a forward crumple read as lying flat because of the big hood. It's hidden at 2.5 s as before.
- **Home** is (−10, 1, 12), ~15.6 m from the spawn line; (−12, 10) was blocked by a pine.
- **It starts casting while still gliding in** (at ≤ 9 m) and settles at ~7 m: the attack check is distance only, as for the Demon. It doesn't turn toward a target that moves sideways while it stands still (the agent only turns while moving); phase B adds facing.
- **Sounds:** the cast plays the swing whooshes (no spell sounds in the project); hits and the death growl are the Demon's.
- **Damage numbers** float 1.4 m above its root (the Demon's 1.2 was for a shorter model).
- `_attackImpactDelay` is 0.63 s (the thrust frame), unused while damage is 0; it's there for the bolt.
- NGO added `Lich.prefab` to `DefaultNetworkPrefabs.asset` by itself when the prefab was made (not needed for the in-scene Lich, needed for a future spawner).
- Not run with two clients or Multiplayer Play Mode.
