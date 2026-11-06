# 01 — Connect and Move

**Status:** Verified with the Editor as host and a standalone development build as client. Multiplayer Play Mode sign-off pending (yours); set Done after it.
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
- [x] Pressing Play in Bootstrap shows Host / Join buttons, then loads the test level. (Host button verified; Join verified through the same channel.)
- [x] Host + 1 client: both characters spawn, both move with WASD, each sees the other move. Verified with a headless development build as the client: real key presses moved the client, and the host reported the identical position; host-side moves replicated to the client. Not measured: smoothness, mouse look on the client, a real Multiplayer Play Mode run.
- [x] Client disconnects → its character disappears for the host. Rejoining spawns it again (new client id, fresh spawn point).
- [x] A client can't move another player's character. A forged `SnapBackRpc` from a client teleported the host's player; fixed (see Differences) and re-verified: forged `SnapBackRpc` and `AttackRpc` calls are refused, and an old client's forged message is dropped by the host.
- [x] EditMode tests pass for the move-direction logic.
- [x] Compiles with zero errors, no new warnings.

## Decisions
- Camera: Cinemachine (package approved).
- Host / Join buttons: framework chosen via the `ui` skill.
- Sprint included.

## Differences from spec
- Net asmdef also references `Unity.Networking.Transport` (needed to resolve `UnityTransport.SetConnectionData`).
- Camera reads mouse look from Cinemachine's default input actions, not the project's `Player/Look`.
- Connection menu uses legacy uGUI `InputField`/`Text`, not TextMeshPro (avoids importing TMP Essentials for throwaway UI).
- Bug fixed: the host's player spawned before TestLevel finished loading and fell through the world. `PlayerMovement` now returns the owner to its spawn on `SceneManager.OnLoadEventCompleted`, and `Teleport` resets vertical velocity.
- Security fix: `SnapBackRpc` was `[Rpc(SendTo.Owner)]` with the default permission, so any client could teleport another player by calling it on their object (the server's speed check only undid large jumps). It is now `InvokePermission = RpcInvokePermission.Server`.
- Verified with the Editor as host and a headless Windows development build as client (`tools/mp-harness/`), not Multiplayer Play Mode.
