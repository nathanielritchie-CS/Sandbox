# 27 — Dungeon generation (Wave Function Collapse)

**Status:** Planned (2026-11-07). Documented at the user's request; **not approved to build**. The decisions under "Decisions needed" come first, then phase by phase.
**Design ref:** `docs/design/overview.md`:
- World: "Many kinds of dungeons, randomly generated so each one is unique, built in a way that keeps the game enjoyable."
- Co-op: "Parties take on dungeons and build fame."

## Goal
A party enters a dungeon that is generated fresh for every run. Rooms, corridors, doors, decoration, enemies and loot always fit together, are always connected from the entrance to the boss, and are the same for everyone in the session.

## Approach: two layers
Wave Function Collapse (WFC, Maxim Gumin 2016) fits pieces together on a grid. Every cell starts as "any piece"; the cell with the fewest options collapses to one (random, weighted); its neighbours drop options that no longer fit; repeat until every cell is decided.

WFC is excellent at **local** fit (no corridor into a wall, lots of variety from few pieces). On its own it cannot promise the **big picture**: that the boss is reachable, how many rooms there are, or the pacing. So generation has two layers:

1. **Layout (structure):** a room graph decides what the dungeon is: entrance → fights → treasure / rest → boss, with a branch or a loop. Rooms are placed on a coarse grid and joined by corridors.
2. **WFC (detail):** the layout's floors, doors and corridor paths are fixed in the grid first. WFC fills everything else: wall variants, pillars, alcoves, rubble, decoration, side nooks.

## Pipeline (runs on the server/host)
1. **Seed:** the host picks a random seed. It's kept for reproducing bugs.
2. **Layout:**
   - Build the room graph from the recipe: room count, types, one or two loops, critical path length.
   - Place rooms (rectangles in grid units) and route corridors between connected rooms (A* on the grid, preferring straight runs).
3. **Constraints:**
   - Mark cells as room floor, door (at each connection) or corridor.
   - Everything outside the dungeon becomes solid.
4. **WFC fill:**
   - The grid's free cells are filled from the theme's module set. Each module has a socket on each of its four sides (solid, open, door, corridor…), and two modules can touch only if their sockets match.
   - Weights per theme control the mix.
   - On a contradiction (a cell with no options left): backtrack a few steps, else restart the fill with seed + 1.
5. **Validate:**
   - A flood fill from the entrance must reach every required room, the boss and every loot spot.
   - Size stays within bounds.
   - If not, regenerate with seed + 1, up to N tries, then fall back to a hand-made layout.
6. **Populate:**
   - Per room type: enemies from the theme's enemy table, scaled by a difficulty budget (party size and level).
   - Chests (loot tables, spec 25), the player arrival point, the boss, and the exit portal.
7. **Build:**
   - Instantiate module prefabs (Addressables, one group per theme) and combine static meshes (`StaticBatchingUtility.Combine`).
   - Place torch lights from module markers.
   - Bake the NavMesh at runtime with AI Navigation's `NavMeshSurface`, on the server only, where enemy AI runs.

## Data (ScriptableObjects, `Data/Dungeons`)
- **`DungeonModule`:**
  - prefab (Addressable reference), footprint (1×1 cell; bigger later);
  - a socket per side, which rotations are allowed, weight;
  - tags (floor, wall, door, corridor, decoration, spawn point, light point).
- **`DungeonTheme`:** the module set and weights, lighting and fog, the enemy table, ambience/music, and the door and chest prefabs.
- **`DungeonRecipe`:**
  - room count range and the room types with counts;
  - loops, grid size bounds;
  - difficulty budget per player and per level;
  - which theme(s) can appear.
- **Grid unit:** about 4 × 4 m per cell, walls about 4 m high, doors about 2 m wide. This leaves room for 4 players, the Demon and the lock-on camera; the hero is 1.7 m.

## Code (assemblies as today)
- **`Gameplay/Dungeons`** (plain C#, tested, no Unity scene code):
  - `DungeonRandom`: a small integer PRNG (xorshift). Same results on every machine; not `UnityEngine.Random`.
  - `RoomGraph` + `LayoutGenerator`: the graph, room placement, corridor routing.
  - `WfcSolver`: options per cell as bit sets, propagation, lowest-entropy choice, backtracking or restart.
  - `DungeonValidator`: connectivity and size checks.
  - `DungeonPopulator`: spawn budgets per room.
  - Output: a compact `DungeonGrid` (module id + rotation per cell, plus a room list).
- **`Data/Dungeons`:** `DungeonModule`, `DungeonTheme`, `DungeonRecipe`.
- **`Net`:**
  - `DungeonSession` (server): runs the pipeline, sends the result, spawns enemies, chests and the exit as NetworkObjects, and tracks progress (rooms cleared, boss down).
  - `DungeonEntrance`: the overworld trigger and prompt.
- **Presentation:**
  - `DungeonBuilder` turns a `DungeonGrid` into GameObjects on every peer (geometry is never networked).
  - UI: an "Entering the dungeon…" overlay while it builds; a minimap later.
- **Editor tool:** a "Dungeon Preview" window. It generates from a seed with grey boxes or the real modules, has buttons to step the solver, show contradictions and run 1,000 seeds, and edits module sockets.

## Library or our own solver (decision 7)
- **Own solver:**
  - A few hundred lines of plain C#, fully tested, no dependency.
  - Connectivity comes from the layout layer, so WFC only decorates. This is enough for phase 1.
- **DeBroglie** (free, MIT, C#; by Boris the Brave):
  - Mature WFC with backtracking, 3D, and **path constraints** that guarantee connectivity inside WFC itself.
  - Pre-approved category (free, open source): keep its license file next to it.
  - (Tessera is the same author's paid Unity tool: not considered.)
- **Recommendation:** start with our own solver plus the layout layer. Switch to DeBroglie if we want WFC to carve the paths itself (caves, organic layouts).

## Networking
- **Server-authoritative state:**
  - The seed and the solved `DungeonGrid` (a few KB: about 32 × 32 cells × 2 bytes).
  - Room states (locked, cleared), enemies, chests and loot, boss state, the exit.
- **Server → clients:**
  - The solved grid itself, not just the seed. No risk of a client generating something different; it's one message per run.
  - Late joiners get it on arrival.
  - A grid hash is logged on every peer, so a mismatch shows up in tests.
- **Client → server requests:**
  - Enter (at the entrance, party checks done by the server), leave, open a chest or door. Each is checked for reach and state.
- **What each client sees:**
  - The same dungeon built locally.
  - Enemies, chests and doors as today's NetworkObjects.
  - The overlay while building.
- **Scene flow:**
  - The party moves into a `Dungeon` scene through NGO scene management; the host builds first, then clients build from the grid.
  - At the exit everyone returns to the overworld.
  - Whether non-party players stay behind is decision 3; mixing scenes per player is much harder.

## Art
- **Modules:**
  - Placeholder: a free CC0 modular dungeon kit (e.g. KayKit's dungeon pack; check the license and keep the license file next to it).
  - Or modules made by a Blender script, like the sword, to match the cartoon style. Re-measured to the 4 m grid either way.
- **First theme set:** about 25–40 modules.
  - Floor, wall, inner and outer corner, door frame, corridor straight/bend/T/cross, pillar, alcove, rubble, stairs (later).
  - Decorations: torches, banners, crates, bones.
- **Lighting:** torches from markers (cap the count per room), fog and ambient per theme, no baked lighting (it's generated at runtime).

## Phases
**Phase 1 — Generator core** (no networking, no art)
- Plain C# layout + WFC + validator + seeded random, with tests.
- The Dungeon Preview window with grey boxes.

**Phase 2 — Real modules, walkable**
- Module kit imported and sockets authored; the first theme and recipe.
- `DungeonBuilder`, runtime NavMesh, torches, fog; walkable by the host with the hero.

**Phase 3 — Co-op**
- `DungeonEntrance` in the overworld; `DungeonSession`; the `Dungeon` scene flow.
- The grid sent to clients; late joiners; leave/return; death and respawn inside.

**Phase 4 — Content and pacing**
- Room types: fight, treasure, rest, boss.
- An enemy budget scaled by party size and level, chests (needs spec 25), the boss room and exit portal, a second theme.

**Phase 5 — Later**
- Fame/guild rewards (not designed yet), traps and puzzles, stealth routes, multiple floors (stairs), a minimap.

## Done when
**Phase 1**
- [ ] 1,000 seeds per recipe all generate (retries allowed), every one is fully connected, and the same seed always gives the same grid.
- [ ] Tests: propagation, contradiction → backtrack/restart, determinism, the validator catches a disconnected room.

**Phase 2**
- [ ] Walk generated dungeons with the hero: no gaps, holes, floating pieces or blocked doors.
- [ ] Enemies path everywhere on the runtime NavMesh. The frame rate holds on the target PC.

**Phase 3**
- [ ] Host + clients get identical grids (hash).
- [ ] Enemies and chests are server-spawned; a late joiner gets the dungeon; the exit brings everyone back. Console clean.

**Phase 4**
- [ ] A full run (entrance → rooms → boss → exit) with 1 and 4 players. Difficulty scales; loot drops.

**Every phase**
- [ ] Compiles with zero errors and no new warnings; host + client where networked; tests for the pure logic.

## Prerequisites
- More enemy types and a spawner (roadmap "any time"); the Demon alone can't carry a dungeon.
- Items, loot and money (specs 23 and 25) for chests and rewards.
- Fame/guild design (open in the design doc) for phase 5 rewards.

## Risks
- **Contradictions on big grids:** keep grids bounded, backtrack, restart with seed + 1, and fall back to a hand-made layout.
- **Samey output:** weights per theme, a few hand-made "set piece" rooms mixed in, and room types with their own module tags.
- **Performance:** combine meshes at runtime, cap lights, keep dungeons bounded. Occlusion can't be baked for generated levels; rooms with doors limit what's visible.
- **NavMesh bake time:** build once per dungeon, behind the "Entering…" overlay.
- **Authoring sockets by hand is error-prone:** the preview window shows mismatches, and a test checks that every socket has a partner.

## Decisions needed (user) before building
1. **Style of the first dungeons:** caves, stone halls, ruins, or a mix.
2. **Length of a run:** about how many minutes, and how many rooms.
3. **Who goes in:** the whole session or only the party. Can someone join a run in progress?
4. **How a run ends:** boss + exit portal? Can a party leave early?
5. **Death inside:** respawn at the entrance, wait for the party, or the run fails if everyone dies.
6. **Rewards:** loot only for now, or fame/guild rank too (needs that design).
7. **Solver:** our own (recommended to start) or DeBroglie.
8. **Art:** a CC0 kit as placeholder, or Blender-scripted modules.
9. **Floors:** one floor at first, or stairs and levels from the start.

## Out (first build)
- Generated overworld, endless dungeons, editing tools beyond the preview window, multiple floors (phase 5), saving a dungeon between sessions.

## Differences from spec
Filled in when Status is set to Done.
