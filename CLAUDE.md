# CLAUDE.md

## Project
Isekai action RPG. Unity 6, URP, 3D third-person, online co-op multiplayer. Target: Windows PC. Solo developer on Windows.
Full game design lives in `docs/design/`. Read only the file relevant to the current task. Never read the whole folder.
Feature work is driven by specs in `docs/specs/`. If a task has no spec and is bigger than a small fix, ask for one.

## How to work (highest priority)
- Write only the code the task asks for. No extra features, no "while I'm here" refactors, no speculative abstractions or config options.
- Before writing more than ~50 lines or touching more than 2 files: state the plan (files, classes, approach) and wait for approval.
- State assumptions explicitly. If the spec is ambiguous, ask. Do not guess.
- Don't restyle or rewrite working code. Don't add comments explaining obvious code. Don't create docs/READMEs unless asked.
- When done, report: what changed (files), how it was verified, and what was NOT verified.
- Prefer the smallest change that meets the spec's "Done when" criteria.

## Definition of done
- Compiles with zero errors and no new warnings.
- Spec's "Done when" criteria met and verified (play mode, tests, or console check via Unity MCP).
- Works for host AND a connected client if the feature is networked.
- EditMode/PlayMode tests added for pure logic (stats, damage, inventory math). No tests required for pure presentation.

## Architecture
- Assembly definitions per area: `Core`, `Net`, `Gameplay`, `UI`, `Data`, `Tests`. Dependencies flow one way: UI → Gameplay → Core; Net → Gameplay → Core. Core depends on nothing.
- Data in ScriptableObjects (stats, items, skills, enemy defs). MonoBehaviours stay thin and delegate to plain C# classes where logic is testable.
- Systems communicate via C# events / ScriptableObject event channels, not direct cross-system references. No `FindObjectOfType`, no `GameObject.Find`.
- No singletons except a single bootstrap/service locator in `Core`.
- Composition over inheritance. Keep inheritance hierarchies at most 2 levels deep.
- New Input System only. Addressables for content loading.
- Networking rules: see `.claude/rules/networking.md` (auto-loads for net code).

## Folder layout
- `Assets/_Project/Scripts/{Core,Net,Gameplay,UI,Data}`: code, one asmdef each
- `Assets/_Project/Data/`: ScriptableObject assets
- `Assets/_Project/{Prefabs,Scenes,Art,Audio,UI}`
- `Assets/_Project/Tests/{EditMode,PlayMode}`
- `docs/design/`: game design (what the game is)
- `docs/specs/`: feature specs (what to build now). Template: `docs/specs/_TEMPLATE.md`

## Conventions
- C#: PascalCase types/methods, `_camelCase` private fields, `[SerializeField] private` over public fields.
- One public type per file; file name = type name.
- Namespaces: `Isekai.<Area>` (e.g. `Isekai.Gameplay.Combat`).

## Keeping docs current (exception to "no docs" rule)
- If a task changes a design decision, update the matching `docs/design/` file in the same task and tell me what changed.
- When a feature is done, set its spec's Status to Done and note anything that differs from the spec.
- Keep `docs/progress.md` current: one line per finished feature, plus an "In progress" section with next steps.
- Never edit CLAUDE.md or `.claude/rules/` yourself. Propose the exact change and wait for approval.

## Never
- Never hand-edit `.meta` files, `ProjectSettings/`, or `Packages/manifest.json`. Use the `unity-package-management` skill and ask first.
- Never add a package, asset, or third-party library without asking.
- Never modify a scene or prefab without saying which one first.
- Never delete files without asking.
- Never put gameplay authority on the client (see networking rules).

## Tools: Unity MCP
- The Unity Editor is reachable through the `unity-editor-mcp` server. Run `unity-cli-status` before any Editor action. If it's not connected, stop and tell me.
- Use the Editor (via MCP / `unity-cli` skill) to verify: compile, read console errors, run tests, enter play mode.

## Skills: load the matching skill BEFORE acting
| Task | Skill |
|---|---|
| Editor control, builds, running tests, CLI | `unity-cli`, `unity-pipeline` |
| Adding/upgrading packages | `unity-package-management` |
| Any UI (HUD, menus, status window, inventory) | `ui` first (router) → `ui-uitk` / `ui-ugui` |
| Custom editor windows/inspectors | `ui-imgui` |
| Finding assets/objects in the Editor | `generate-editor-search-query` |
| Post-processing, bloom, color grading | `urp-postprocessing` |
| Custom renderer features | `validate-urp-render-graph-renderer-feature` |
| Custom Shader Graph nodes | `shader-graph-create-custom-node` |
| Node-based editor tools (dialogue/quest graphs) | `build-gtk` |
| Collisions, triggers, raycasts | `physics-3d-collision` |
| Enemy/NPC pathfinding | `initialize-ai-navigation` |
| Audio routing / audio performance | `audio-setup-mixers`, `optimize-audio` |
| Text rendering / languages | `optimize-text-mesh-pro`, `localization` |
| Sessions, lobbies, matchmaking, hosting | `setup-multiplayer-services` |
| Server-side progress, auth, cloud save, leaderboards | `build-live-game` |
| Voice/text chat | `setup-vivox-voice-chat` |
| Importing 3D models / LODs | `asset-transformer-toolkit` |
| Look-and-feel polish, juice | `craft-game-floors` |
| Performance audit | `project-auditor-fixes` |

Do not use unless I explicitly ask: 2D skills (`2d-pixel-perfect`, `create-2d-physics`, `sprite-*`, `manage-sprite-atlas`, tilemap skills), `migrate-birp-to-urp` (already URP), `optimize-web`, `new-unity-project`, monetization (`implement-in-app-purchases`, `levelplay-unity-integration`).

Always apply `karpathy-guidelines`: simple code, surgical changes, stated assumptions, verifiable success criteria.
