# 09 — HUD and Menus in TextMeshPro

**Status:** In progress — only step 1 is partly done (Fredoka font file and its license are in `Assets/_Project/UI/Fonts/Fredoka/`). Nothing else has been changed yet.
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
- [ ] Machine scan: no TMP text uses the stock LiberationSans asset; no Image uses the default uGUI sprite; panel Images are palette-tinted.
- [ ] Captures: menu; gameplay HUD with the health bar; status window open; party panel with two members; all readable over the bright meadow.
- [ ] The health bar drops when hit on host and client, shows the death line, refills on respawn.
- [ ] Menu regression with the harness: direct Host/Join and online Host Online/Join Online with a code still work.
- [ ] Visual scorecard (`craft-game-floors` → `visual-scorecard`): all six axes at 3 or better; the board is pasted below.
- [ ] Compiles with zero errors, no new warnings; console clean; client build runs.

## Assumptions
- Fredoka and every color are placeholders.

## Differences from spec
Filled in when Status is set to Done.
