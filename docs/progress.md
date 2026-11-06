# Progress

## Done
- 2026-11-05: Project skeleton — `Assets/_Project` folder layout, asmdefs (Core, Data, Gameplay, Net, UI, Tests.EditMode, Tests.PlayMode), packages (Netcode for GameObjects 2.13.3, Multiplayer Services 2.4.1, Addressables 4.1.2, Multiplayer Play Mode 2.0.2, Unity Pipeline 0.10.0-exp.1), empty Bootstrap scene. Assumed Data → Core and Gameplay → Data (not stated in CLAUDE.md).
- 2026-11-05: Docs — `docs/design/overview.md` (from design interview), `docs/specs/_TEMPLATE.md`, `.claude/rules/networking.md`.

## In progress
- **Spec 01: connect and move** (`docs/specs/01-connect-and-move.md`). Built and host-verified; client test pending.
  - Verified (host, in Play Mode): Host button → TestLevel loads, player spawns and is owned, Cinemachine follows it, walk 4.0 m/s, sprint 7.0 m/s, 20 m teleport snapped back by server, no runtime errors. 9/9 EditMode tests pass. Compiles with zero errors.
  - NOT verified: host + client in Multiplayer Play Mode (both spawn, see each other move, can't move each other, disconnect/rejoin). The user runs this manually (Window > Multiplayer Play Mode > Player 2 → Play in Bootstrap → Host in main, Join in Player 2). After it passes: set spec Status to Done and fill "Differences from spec".
  - Differences from spec so far: Net asmdef also references `Unity.Networking.Transport` (needed to resolve `UnityTransport.SetConnectionData`). Camera reads mouse look from Cinemachine's default input actions, not the project's `Player/Look`. Connection menu uses legacy uGUI `InputField`/`Text`, not TextMeshPro (avoids importing TMP Essentials for throwaway UI).

## Next steps
1. Finish spec 01 (client test above).
2. Spec 02: basic combat — one attack vs a dummy enemy, server-decided damage (agreed order: connect/move → combat → stats + status window → party forming).
3. Spec 03: stats + status window (plain C# stats, tested). Spec 04: party forming (pact design undecided).
4. Multiplayer Services sessions (lobbies/join codes) after spec 02.

## Current state (for a new session)
- Build Settings: 0 Bootstrap, 1 SampleScene (disabled, template leftover), 2 TestLevel.
- Bootstrap: `NetworkManager` (UnityTransport, PlayerPrefab = `Prefabs/Player.prefab`, scene management on) + `ConnectionController`; `ConnectionCanvas` with `ConnectionMenu` (address/port fields, Host/Join); `EventSystem` with `InputSystemUIInputModule`.
- TestLevel: Ground, 4 boxes, Directional Light, Main Camera (CinemachineBrain), FollowCamera (CinemachineCamera + OrbitalFollow + RotationComposer + InputAxisController), CameraRig (`CameraTargetBinder`).
- Event channels (ScriptableObjects, `Data/Events/`): `LocalPlayerChannel`, `CameraChannel` (TransformEventChannel, caches last value for late subscribers), `ConnectionRequestChannel`. Used because UI can't reference Net and nothing may use `FindObjectOfType`.
- Player prefab: capsule, CharacterController, NetworkObject, NetworkTransform (AuthorityMode = Owner), `PlayerMovement` (owner moves; server checks horizontal speed every 0.5 s against sprint × 1.5 and snaps back via `[Rpc(SendTo.Owner)]`).
- Spawn points are a shortcut: line by client id (`PlayerMovement.OnNetworkSpawn`).
- `NetworkManager` referenced via serialized field, not `NetworkManager.Singleton` (no-singletons rule).

## Known issues / gotchas
- The Unity Editor doesn't import new or changed files while unfocused. After writing files, run `AssetDatabase.Refresh()` via Unity MCP `script_eval`. If scripts still don't show up in `CompilationPipeline.GetAssemblies`, ask the user to restart the Editor (that fixed it once).
- After package installs the Editor reloads; `unity status --until-ready` can time out a few times before it's ready. MCP tools disconnect during reloads and come back.
- Unity MCP Multiplayer Play Mode tools (`editor.playmode list_players` etc.) don't work with MPPM 2.0.2 ("PlayModeManager not found"). Multi-player tests are manual.
- Remaining warnings: "no scripts associated" for `Isekai.Data` and `Isekai.Tests.PlayMode` (clear when they get scripts); Cinemachine package's HDRP sample asmref warning (package-side, harmless in URP).
- `docs/design/overview.md` has an "Still undecided" list — check it before writing any spec that touches those areas.
