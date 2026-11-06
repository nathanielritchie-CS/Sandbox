# 01 — Connect and Move

**Status:** In progress (host verified; client test pending)
**Design ref:** `docs/design/overview.md` (Co-op)

## Goal
A host starts a game and a second player joins. Each player spawns a character, moves it around a test level in third person, and sees the other player moving.

## Scope
- In:
  - Bootstrap scene holds the `NetworkManager` and loads a test level.
  - Test level: a flat ground plane and a few boxes to walk around. No art.
  - Host / Join by direct connection (IP + port, default localhost).
  - Minimal Host / Join buttons (debug-style, replaced later).
  - Networked player prefab: capsule placeholder, spawned by the server on connect, despawned on disconnect.
  - Third-person movement (walk, sprint) and camera (orbit with mouse, follows local player), using the New Input System.
- Out:
  - Lobbies, join codes, Multiplayer Services sessions.
  - Jumping, combat, stats, animation, character models.
  - Party forming, spawn-point randomization, the cosmic intro.

## Requirements
- Exactly one `NetworkManager`, living in Bootstrap and persisting across scene loads.
- Only the server spawns player objects.
- Each client controls only its own character. Input from other players' characters is ignored.
- Camera follows only the local player.
- Movement and camera logic that can be plain C# (e.g. turning input + camera yaw into a move direction) lives in Gameplay and is unit-tested.

## Networking
- Server-authoritative state: which players exist, spawning and despawning.
- Client → server: movement. The local player moves its own character (owner-authoritative, as the networking rules allow). The server rejects moves faster than max sprint speed.
- What each client sees: all players' positions and rotations, smoothed.

## Done when
- [ ] Pressing Play in Bootstrap shows Host / Join buttons, then loads the test level.
- [ ] Host + 1 client in Multiplayer Play Mode: both characters spawn, both move with WASD + mouse, each sees the other move smoothly.
- [ ] Client disconnects → its character disappears for the host. Rejoining spawns it again.
- [ ] A client can't move another player's character.
- [ ] EditMode tests pass for the move-direction logic.
- [ ] Compiles with zero errors, no new warnings.

## Decisions
- Camera: Cinemachine (package approved).
- Host / Join buttons: framework chosen via the `ui` skill.
- Sprint included.

## Differences from spec
Filled in when Status is set to Done.
