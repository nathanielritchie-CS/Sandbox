# 18 — The Otherworlder: a Custom Player Character with Strafing

**Status:** Built and verified, first pass (2026-11-06), with the Editor host + a headless client; waiting for your look-over and Multiplayer Play Mode sign-off before Done.
**Design ref:** `docs/design/overview.md` (players are Otherworlders; combat "satisfying but difficult"; lock-on)

## Goal
Every player is a custom-made young swordsman built, rigged and animated for this game, with real strafing and attack animations, so locked-on combat looks like the character is circling and fighting the enemy instead of sliding.

## Scope
- In:
  - **Model** (made by script in Blender 5.1; no hand sculpting): a young man about 6 heads tall, stylized low-poly to match the art bible: short spiky hair, simple face, tunic with a light chest plate, shoulder guard, belt, bracers, trousers and boots, a straight sword in the right hand. One model for every player; each player's outfit gets its own color.
  - **Rig:** a humanoid skeleton Unity can map (Humanoid avatar), skinned to the body; the sword follows the right hand.
  - **Animations** (in place, made by keyframes in Blender): idle (breathing), walk, run, strafe left and right, walk backwards, three combo swings (slash, backhand, finishing thrust), charge stance (loop), heavy release, roll, hit, death.
  - **In Unity:** imported as Humanoid; the player's animator gets directional locomotion (blend by how you move relative to where you face); the six Quaternius looks are replaced by the new model.
  - **Lock-on becomes real strafing:** while locked you always face the target and move around it with the strafe and backpedal animations (the user's original reason for free running was that no strafe clips existed).
- Out:
  - Facial animation, cloth/hair physics, IK foot placement, per-class outfits (classes come later), a female body (the user chose one model), root motion, a new Demon.

## Requirements
- The Blender build is a script kept in the repo (`tools/blender/`), so the model can be regenerated and tweaked; the exported FBX goes to `Assets/_Project/Art/Characters/Otherworlder/`.
- Animation timing matches the server's impact delays (swing 0.36 s, combo 2/3 0.3 s, heavy 0.36 s after release) or the delays move to match the clips.
- Presentation only on the network: animations follow replicated movement and the existing attack/charge/roll messages.

## Networking
- Nothing new on the wire. Every peer computes the local move direction from the replicated position and rotation for the strafe blend.

## Plan (files)
- `tools/blender/otherworlder.py` (model, rig, animations, FBX export) + preview renders.
- Unity: FBX import settings (Humanoid, clip loops), `Art/Characters/Otherworlder/` materials, an animator controller with a 2D locomotion blend tree, `CharacterAnimator` (move X/Z parameters, outfit color per player), `PlayerMovement` (locked → always face the target), `Player.prefab` (model swap).

## Done when
- [x] Renders of the model (front, side, back) and frames of each animation look right. (Blender Workbench renders + contact sheets per action, reviewed and fixed over three passes: sword grip, swing arcs, fitted armour, brows, proportions.)
- [x] In Unity, host and client see each other as the new character with different outfit colors; idle/walk/run play with movement. (`s18g_idle.png`, `s18_strafe.png`: host blue #3B70BA, client red #BF3D3D, set per material slot by client id.)
- [x] Locked on, moving sideways and backwards plays strafe/backpedal while facing the target, on every peer. (Client locked on the dummy holding A with a real key: the host saw it face the dummy at 0° throughout, MoveX ≈ −3.9 m/s, blending `RunLeft`.)
- [x] Combo, charge, heavy, roll, hit and death play at the right moments; hits land when the blade connects. (Host captures of the slash, charge with the full-charge sparks, heavy; the clips put the blade on target at frames 9–11 = 0.30–0.37 s, the server's impact delays.)
- [x] Compiles with zero errors, no new warnings; tests pass (79/79); client build 0 warnings / 0 errors; host console 0 entries, client log clean.

## Open questions
- None blocking. The look is a placeholder made by script; a hand-made model can replace the mesh later on the same skeleton (keep the bone names and the T-pose).

## Differences from spec
- **Imported as Generic, not Humanoid.** Unity's Humanoid retargeting rebuilt the poses through its muscle system and lost the hand's twist, so the sword pointed the wrong way; Generic plays the authored bone rotations exactly. Consequences: no Unity foot IK (the gait plants the feet itself), and these clips only fit this skeleton.
- The FBX comes out facing −Z; the prefab turns the model 180°.
- Locomotion is a 13-entry 2D blend: walk 1.5 m/s, run 4 m/s and sprint (run × 1.75) forward, back, left and right, plus idle.
- Locked-on movement is now Souls-style strafing (always face the target), replacing spec 16's free running.
- The six Quaternius looks were removed from `Player.prefab` (their assets stay in the project).
- Not verified in the game: roll, hit and death (seen in the Blender renders only), the backpedal on another peer (same path as the strafe), the exact contact frame of each swing by eye, Multiplayer Play Mode, how it all feels with a real mouse and keyboard.
