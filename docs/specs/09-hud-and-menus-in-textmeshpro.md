# 09 — HUD and Menus in TextMeshPro

**Status:** Done (2026-11-06). Verified with Editor host + headless client (harness), captures, machine scan and the visual scorecard. Not networked, so no Multiplayer Play Mode sign-off needed beyond the session check.
**Design ref:** `docs/design/overview.md` (status window, Art style)

## Goal
The menu and the in-game HUD read as part of the game: one friendly font, the placeholder palette, and a real health bar instead of debug text.

## Scope
- In:
  - TextMeshPro Essential Resources, imported non-interactively (`TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false)` — never the menu item, it opens a modal dialog).
  - Fredoka (SIL Open Font License, from Google Fonts) as the UI font: one **static** TMP font asset with the Latin set (ASCII 32–126), sampling point size 90, padding 9 (10 %), Scale 1 (rules from the `optimize-text-mesh-pro` skill).
  - Health HUD (bottom-left): a bar (fill Image) plus "HP 80 / 100", and "You died. Respawning..." while dead. Fed with numbers, not text.
  - Status window (C), party panel (top-right) and join-code label (top-left) moved to TMP with palette panels, warm-white text and gold-accent headings.
  - Bootstrap menu rebuilt in the same style: a title, an Online panel (join code field, Host Online, Join Online, status line) and a smaller Direct (LAN/testing) panel (address, port, Host, Join), using `TMP_InputField` and TMP button labels.
- Out:
  - Live-scene title backdrop, pause / settings / end screens, screen transitions (the `game-shell` floor; a later spec).
  - Icons, animations, localization, controller/keyboard menu navigation.

## Requirements
- UI still only reads Core event channels (UI cannot reference Net). No gameplay change.
- Every color comes from `Isekai.Core.Palette`. No default uGUI skin sprites, no unstyled stock font.
- Text that changes often (HP) keeps its own Canvas; no AutoSize.

## Networking
Not networked: local presentation of state that is already replicated.

## Plan (files)
- Core: new `HealthEventChannel` (current, max; caches the last value like the other channels).
- Net: `PlayerHealthReporter` raises `HealthEventChannel` (current, max) instead of formatted text.
- UI: new `HealthBar`; `StatusWindow`, `PartyPanel`, `TextChannelLabel` switch their text field to `TMPro.TMP_Text`; `ConnectionMenu`, `OnlineMenu` switch to `TMPro.TMP_InputField`; `Isekai.UI.asmdef` references `Unity.TextMeshPro`.
- Assets: `Data/Events/PlayerHealthChannel.asset` is currently a `TextEventChannel`; it must become a `HealthEventChannel` (create the new asset, rewire `Player.prefab`'s `PlayerHealthReporter` and the HUD, then remove the old one).
- Scenes: Bootstrap (`ConnectionCanvas` rebuilt), TestLevel (`HealthCanvas`, `StatusCanvas`, `PartyCanvas`, `SessionCanvas` rebuilt). Changing a serialized field's type drops the old reference, so every rebuilt component must be rewired and read back.

## Done when
- [x] Machine scan: no TMP text uses the stock LiberationSans asset; no Image uses the default uGUI sprite; panel Images are palette-tinted. (Bootstrap 14 TMP texts / 10 Images, TestLevel 6 / 6: 0 problems, colors used Accent, Panel, Shadow, Text, Danger; no legacy Text/InputField/Outline left. TestLevel: 567 renderers, 0 default materials, 0 particle systems, Sky skybox.)
- [x] Captures: menu; gameplay HUD with the health bar; status window open; party panel with two members; all readable over the bright meadow.
- [x] The health bar drops when hit on host and client, shows the death line, refills on respawn. (Host: Demon hits 25 → 20 → 15 → 10, death line at 0.1 s with an empty bar, refill to 100 at 5.0 s. Client, read from its own HUD: Demon hits 10 → 5 → death line → 100 → 95; again after the rebuild: 75 → death line → 100.)
- [x] Menu regression with the harness: direct Host/Join and online Host Online/Join Online with a code still work. (The real menu buttons and TMP fields were driven; online code MW9FLB through the cloud services; the code shows in the HUD on host and client.)
- [x] Visual scorecard (`craft-game-floors` → `visual-scorecard`): all six axes at 3 or better; the board is pasted below.
- [x] Compiles with zero errors, no new warnings; console clean; client build runs. (52/52 EditMode tests; client build 0 warnings / 0 errors. One caveat in Differences: a *forced* reimport of `Player.prefab` logs an importer-consistency warning.)

## Visual scorecard (after the fix pass)
Frames: standard gameplay angle, Warrior close-up, busiest UI moment (status window open at the level-up, party of two, HP 65, swing at the dummy; plus a Demon exchange with its Hit flinch). Captures are in `Isekai/Temp/harness/s09_*.png` (not in git).
- palette discipline: 4/5 — machine scan clean (0 stock-font TMP, 0 default sprites, every UI Image on a Palette color, 0 default materials); panels hold their plum hue over the meadow.
- style uniformity: 4/5 — rounded Fredoka and rounded 9-slice panels match the chunky low-poly storybook models; menu and HUD share one kit; the straw capsule dummy and flat stone blocks are still primitive props.
- lighting depth: 4/5 — warm key, soft cool shadows, sky haze (spec 08, unchanged).
- silhouette: 4/5 — Warrior, Rogue and Demon read distinctly from the gameplay camera and in close-up.
- ui intentionality: 4/5 — palette HUD on a 32 px margin grid, styled TMP text with gold headings, title in accent with a soft darkest-value shadow, clear Online/Direct hierarchy; flat menu backdrop, stat values not column-aligned.
- feedback visibility: 3/5 — swing, Demon Hit flinch and the HP-bar drop are in frame, level-up shows in the status window; the dummy gives no visible hit answer (tint by HP only) and the level-up has no moment.

Before the fix pass: palette 4, style 4, lighting 4, silhouette 4, UI 3 (panels at 90% alpha read slate grey over the meadow, title had no shadow), feedback 3. Fix pass: HUD panel alpha 0.9 → 0.95; title gets the TMP material preset `Fredoka SDF - Title Shadow` (Panel-colored soft underlay, the game-shell title treatment).

## Assumptions
- Fredoka and every color are placeholders.

## Differences from spec
- `PlayerHealthChannel.asset` was switched to `HealthEventChannel` **in place** (its script reference swapped in the Editor, same GUID) instead of create-new, rewire, delete. `Player.prefab` needed no change and nothing was deleted. Side effects: the asset's `m_EditorClassIdentifier` still names TextEventChannel (cosmetic), and a *forced* reimport of `Player.prefab` logs "PrefabImporter generated inconsistent result" because the pre-swap import shares its dependency hash. Normal imports don't, and it goes away once any of the prefab's dependencies changes.
- Fredoka's variable font loads as its default instance, **Light**; the font asset's weights are raised (Normal 1.0, Bold 1.5) so text reads Medium/Bold. A static SemiBold TTF would be the cleaner fix later.
- TMP Settings: default font = `Fredoka SDF`; global fallback = `LiberationSans SDF - Fallback` (dynamic, 512 atlas, clears on build), so non-ASCII text (e.g. service error messages) doesn't turn into boxes.
- The health fill is a rounded Image whose RectTransform `anchorMax.x` follows HP (keeps the rounded ends), not `Image.Type.Filled`. Bar: Danger fill on a Shadow track.
- The party panel's "Party" heading moved from code to a static gold heading in the scene, so it always shows.
- `TextChannelLabel` gained an optional `_background`, hidden while the text is empty, so the join-code panel doesn't show as an empty box in direct (LAN) games.
- New generated sprite `UI/Sprites/RoundedPanel.png` (white rounded rect, 9-slice), tinted from `Palette` everywhere; the menu background is a flat Shadow fill.
- "Isekai" is a placeholder title (the design doc has no game name).
- Menu layout: Online panel = Host Online (full width), then the join code field and Join Online in one row, then the status line; Direct panel = address + port row, Host + Join row. Port and address keep their old defaults (127.0.0.1, 7777) and content types.
- Not done / noticed: the training dummy has no hit reaction (feedback follow-up for the game-feel spec); `OnlineMenu` keeps its buttons clickable while a request is in flight (as before).
