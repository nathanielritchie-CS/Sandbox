# 07 — Animated Placeholder Characters

**Status:** Verified with the Editor as host and a standalone development build as client. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Art style, Co-op)

## Goal
Players and the enemy stop being capsules: each player is an animated fantasy character, the enemy is an animated monster, and everyone sees the same animations (idle, run, attack, hit, death).

## Scope
- In:
  - Free CC0 placeholder art by Quaternius (downloaded from OpenGameArt), imported under `Assets/_Project/Art/Placeholder/Quaternius/` with each pack's license:
    - RPG Characters (Cleric, Monk, Ranger, Rogue, Warrior, Wizard).
    - Cute Animated Monsters: the Mushroom only, for now.
  - Player prefab: a character model replaces the capsule. Each player gets one of the six characters by client id (so co-op players can tell each other apart; no class choice yet).
  - Enemy: the Mushroom replaces the capsule.
  - One presentation component drives the Animator for players and the enemy: run/idle from movement speed, attack from a server event, hit and death from replicated health. Death plays before the enemy disappears; it comes back idle at home.
- Out:
  - Class choice, character customization, weapons as separate items.
  - Hits timed to the swing (hits still land on click), root motion, foot IK.
  - Sound, VFX, ragdolls, the forest/lighting pass (spec 08), the HUD pass (spec 09).

## Requirements
- Animation is presentation only: no change to authority, damage, colliders or the CharacterController.
- Remote players and the enemy animate from state that is already replicated (transform, health); the only new message is "this character attacked" from the server to everyone.

## Networking
- Server → all: attack event per attacker (player or enemy), sent by the server after it accepted the attack.
- Everything else (speed, hit, death) is derived on each peer from replicated transform and health.

## Done when
- [x] Host and client each see both players animated: idle when still, run when moving (own and other player), attack on attack, hit reaction when damaged, death at 0 HP, idle again after respawn. (Host: every state checked and captured. Across processes: the host saw the client's Rogue go Idle → Walk → Run → Idle → Attack from real key and mouse input; the client saw the host's Warrior run and attack.)
- [x] Two players get different characters. (Host = Warrior, client = Rogue, the same on both machines.)
- [x] The enemy idles at home, runs when chasing, attacks on each hit, reacts when struck, plays death then disappears, and comes back idle at home. (Seen on the client: Idle → Walk → Hit/Attack alternating → Death → hidden 2.4 s later → Idle at home ~10 s after death.)
- [x] Existing gameplay checks still pass (damage, XP, respawn). (Kill gave 30 XP, bites did 5, host respawned at spawn with 100 HP.)
- [x] Assets import without errors; compiles with zero errors, no new warnings. (Console and client log clean; player build has 0 warnings.)

## Placeholder look notes (from the design doc's Art style; not a design decision)
- Style: stylized low-poly fantasy, rounded and colorful, cartoon rather than realistic ("slightly less than Pixar", medium-low poly, vibrant, glowing elements later).
- Palette and mood: set in spec 08 with the lighting pass.

## Assumptions
- All art here is placeholder, to be replaced.
- The character a player gets is cosmetic and follows the client id (host = Warrior).

## Differences from spec
- The enemy is the red **Demon**, not the Mushroom: the Mushroom FBX only carries an Idle animation (so do Chicken, Penguin and Pig). Red also reads better against a green forest.
- Each character uses its own clips through an override of one shared controller (`Art/Placeholder/Quaternius/Animation/`). Attack clips: Warrior Sword_Attack, Rogue Dagger_Attack, Wizard and Cleric Staff_Attack, Ranger Bow_Attack_Draw, Monk Attack. Sprint plays Run 1.75× faster.
- Generic rigs with no Avatar asset: the clips animate the skeleton without one.
- The enemy no longer uses the training-dummy component: `PlayerRespawn` became a general `Respawn` (moves players home; enemies are moved home by `EnemyAI`), and `CharacterAnimator` hides the enemy 2.5 s after death and turns its collider off while dead.
- Materials are matte URP Lit copies of each texture, remapped into the FBX imports.
- Not seen yet: the Wizard, Ranger, Cleric and Monk in a session (they need 3+ players); they load and animate through the same path.
- Verified with the Editor as host and a headless Windows development build as client (`tools/mp-harness/`), not Multiplayer Play Mode.
