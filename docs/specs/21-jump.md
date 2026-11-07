# 21 — Jump

**Status:** Built and verified 2026-11-07 (Editor, host and two clients through the harness); waits for the user's Multiplayer Play Mode sign-off.
**Design ref:** `docs/design/overview.md`

## Goal
Space makes your character jump (about 1 m) with a take-off, airborne and landing animation that everyone in the session sees; the dodge roll moves to Left Ctrl.

## Decisions (user, 2026-11-07)
- **Space = jump; roll moves to Left Ctrl.**
- Assumed and told to the user: about 1 m high, no stamina cost, not while rolling or attacking.

## Scope
- In:
  - Input: a `Jump` action on Space in the Player map; `Roll` rebound to Left Ctrl.
  - `PlayerMovement` (owner): jump when grounded, alive, not rolling, not busy attacking or charging; gravity as today.
  - No attacks while airborne (owner side).
  - Animation: Jump (take-off), Fall (airborne, loops) and Land states cut from Regular_Jump.
    - `Jump` trigger: the owner plays it at once and the server relays it to everyone else, like the roll.
    - `Grounded` bool: each peer sets it from a short ground ray under every character, so remote players land correctly too.
- Out: jump attacks, double jump, climbing, fall damage, Run_and_Jump / Basic_Jump clips.

## Requirements
- The server's movement check stays horizontal-only (a jump is vertical), so jumping never snaps you back.
- The roll keeps all its rules; only its key changes.

## Networking
- Server-authoritative state: none new (position is owner-authoritative and server-checked as today).
- Client → server requests: `JumpRpc()` (owner), relayed to non-owners for the animation only.
- What each client sees: every character's jump, airborne and landing animation.

## Done when
- [x] Space jumps about 1 m on host and client; each sees the other jump (take-off, air, landing).
- [x] Left Ctrl rolls; Space no longer rolls.
- [x] No jump while rolling, attacking or dead; no attack while airborne.
- [x] Compiles with zero errors, no new warnings; console clean; tests pass.

## Open questions
- Should jumping cost stamina later?

## Differences from spec
- **Gamepad:** follows the same split. Jump on South; roll moved to East, which the template's unused Crouch action also has.
- **Vertical movement:** now uses the frame's average vertical speed. A long frame made the first version jump 2.75 m; it's now exactly 1.00 m at any frame rate, and falling is slightly more accurate too.
- **Measured:** the host rises 1.00 m; the host saw the client's JumpUp → Fall → Land with a 0.92 m rise (interpolated). Space while attacking doesn't jump; Left Ctrl rolls.
