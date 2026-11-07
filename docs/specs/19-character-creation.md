# 19 — Character Creation in the Cosmic Space

**Status:** Phase A built and verified 2026-11-07: Editor, host and two clients through the harness; waits for the user's Multiplayer Play Mode sign-off. Phases B and C not started. They were designed around the Blender-built model, and the player is now the user's Meshy model (spec 20), so they need a new approach first.
**Design ref:** `docs/design/overview.md` (Premise: "In the cosmic space the player creates the visual appearance of their new character"; Character creation)

## Goal
Before their first adventure, a player wakes as a glowing astral figure in a starfield, is told they have died and will be reborn, and shapes their new self: body, face, hair, clothes, colours and name. The look is theirs for good; everyone in a co-op session sees it, and clothes and gear fit whatever body they made.

## Decisions (user, 2026-11-06)
- All three kinds of customization: **colours, shape sliders, swappable parts**.
- **One body with a separate slider for every feature** (no single male/female switch); any mix is possible.
- **Clothes and gear fit the body**: every garment reshapes with the body sliders.
- **Locked after creation**: the look and the name can't be changed later, except in rare special circumstances designed later.
- **Where and when**: in the cosmic space, alone, from the title screen, before playing; the character is saved on the PC and Host/Join plays as it.
- **The name** is chosen in the cosmic space (the title-screen name field goes away).
- **Look of the cosmic space**: deep blue-violet space, a slowly turning nebula, stars; you stand on a disc of soft light; the creator panels use the blue system-hologram style of the status window.

## Scope
- **Phase A — the cosmic space, the creator and colours**
  - Title screen: shows the saved Otherworlder (name) with Host/Join; "Create your Otherworlder" when there is none (Host/Join wait until one exists); "Start over" replaces it after a warning.
  - `CosmicSpace` scene (offline, no networking): stars and nebula, light disc, a few lines from the voice as on-screen text in the hologram style (placeholder story; skippable), the astral silhouette that materializes into the character when shaping begins.
  - Creator: a hologram panel with tabs (Body, Face, Hair, Clothes, Colours); a live 3D preview (drag to turn, scroll to zoom, the Face tab frames the face); Randomize and Reset.
  - Colours: skin, hair, eyes, top, trousers, leather, metal, accent; a swatch palette per slot plus a free hue/brightness picker.
  - Name step ("What is your name?"), then "Begin your new life" with a warning that the look can't be changed later; saved; back to the title screen.
  - In a session: your look is sent once when your character spawns; the server checks every value and shares it with everyone, late joiners included.
- **Phase B — shape sliders**
  - The Blender script generates a blend shape per feature. Body: shoulders, chest, bust, waist, hips, belly, arms, legs, neck. Face: face width and length, jaw, chin, cheeks, eye size/spacing/height/tilt, brow height and angle, nose size and length, mouth width and height, ear size and point (elf ears).
  - Height and head size by scaling (kept to about ±10% so the animations still land).
  - Every garment and armour piece gets the same blend shapes (transferred from the body by script), so they fit.
- **Phase C — swappable parts**
  - Hairstyles (about 6), facial hair (about 3), tops (about 4, including modern clothes from the old life), trousers or skirts (about 3), footwear (about 3), gloves (2), accessories (scarf, cape, headband, earrings). All made by the script on the same skeleton, all conforming, all colourable.
  - The current breastplate and pauldron become an "Armour" part until the equipment spec turns armour into gear (same parts system).
- **Out:** the class and single-item choices (later specs), a real voice, the opening death scenario, tattoos/scars/make-up layers, saving to the cloud, more than one character per PC, the "special circumstances" for changing the look later, facial animation.

## Requirements
- Appearance is data: a catalog (ScriptableObject: sliders, parts per slot, colour slots and palettes) and one appearance record (slider values, part per slot, colour per slot). The record's rules are plain C# with tests: defaults, clamping, unknown parts → defaults, a compact byte encoding and back, the save file round trip.
- Appearance is cosmetic only: the collision capsule, reach, speed and hit ranges never depend on it.
- The save file stores ids (not list positions), so adding options later doesn't scramble old characters.
- UI never references Net (channels as today).

## Networking
- Server-authoritative state: each player's appearance (encoded, about 100 bytes) and name, after the server's checks; read by everyone.
- Client → server requests: `SetAppearanceRpc(bytes)` and the existing `SetNameRpc(name)`, both once at spawn, from the saved character.
- What each client sees: every player's look; a late joiner gets everyone's on arrival.

## Plan (files) — Phase A
- Data: `AppearanceCatalog`.
- Gameplay: `Appearance`, `AppearanceRules` (validate), `AppearanceCodec` (bytes), `CharacterSave` (JSON) + tests.
- Net: `PlayerAppearance` (NetworkVariable + RPC + server check); the per-player outfit tint in `CharacterAnimator` is replaced by the chosen colours.
- Presentation: `AppearanceApplier` on the model (colours per material slot now; blend shapes, parts and scaling in B and C). `HitFeedback`'s flash moves to emission so it doesn't fight the per-slot colours.
- UI: `CharacterCreator` (tabs, sliders, swatches, picker, preview control), `CosmicNarration`, title-screen character panel (replaces `NameField`).
- Scenes: new `CosmicSpace` (skybox, star particles, light disc, preview stage, creator canvas); Bootstrap title screen changes.
- Phases B and C: mostly `tools/blender/otherworlder.py` (shape keys, garment transfer, parts) plus catalog entries.

## Done when
- **Phase A**
  - [x] A new player goes title → cosmic space → narration → creator → name → begin → title, and the character is saved; restarting the game keeps it.
  - [x] Every colour slot changes the preview live; Randomize and Reset work; the preview turns and zooms.
  - [x] Host and client each create a different look; each sees the other's exactly (same bytes, captures).
  - [x] A client sending broken or out-of-range appearance bytes gets clamped or defaulted by the server.
  - [x] Tests for the appearance rules, encoding and save file; compiles clean; console clean.
- **Phase B**
  - [ ] Each slider visibly changes its feature only; extremes stay clean (renders and captures).
  - [ ] Clothes and armour fit at slider extremes (no skin poking through) in the idle, run and attack poses.
  - [ ] Height and head size scale within limits; animations still read correctly.
- **Phase C**
  - [ ] Every part option shows, fits every body, takes its colours, and animates with the character.
  - [ ] Host and client see each other's chosen parts.

## Open questions
- What the "rare special circumstances" for changing your look are (later design).
- The voice's actual lines and the opening scenario (still open in the design doc); Phase A uses placeholder lines.
- One character per PC for now; several slots later if wanted.

## Differences from spec
Phase A (2026-11-07):
- **Creator scope:** only the Colours section. The Body/Face/Hair/Clothes tabs and the Face framing come with phases B/C.
- **Colour slots:** since spec 20 there are 5 (skin, hair, top, trousers, boots), painted as regions on the user's model; it was 8 material slots on the old Otherworlder. Eyes, metal and accent were dropped.
- **Classes:** no separate `Appearance`/`AppearanceRules` classes. `AppearanceCodec.Decode` does the checks: wrong version, too short or over 90 bytes → defaults; fewer colours → the rest defaulted. `CharacterSave` handles ids and hex.
- **Names:** `CosmicNarration` is `CosmicSequence` (it runs the whole flow), and the RPC is `SetLookRpc(bytes)`.
- **Narration skipping:** line by line (click or Space finishes a line, the next one moves on); there is no skip-all.
- **Hit flash:** `HitFeedback` keeps its `_BaseColor` flash but saves and restores each material slot's colour, instead of moving to emission.
- **Cosmic lighting:** toned down (key light near-white, disc glow 0.2) so chosen colours read true.
- **Save file:** one per PC, shared by the Editor and the built client on the same PC. For a same-PC test, write the client's save just before it joins.
- **Verified 2026-11-07:**
  - The full flow, restart, Start over, every slot, Randomize/Reset/turn/zoom.
  - Host "Aria" and client "Rin" see each other's exact bytes and region colours.
  - Broken bytes from the client: wrong version → defaults, 300 bytes → defaults, 2 colours → 2 kept + 3 defaulted.
  - Console clean; 88/88 EditMode tests.
