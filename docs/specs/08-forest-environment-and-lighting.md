# 08 — Forest Environment and Lighting

**Status:** Verified (host captures, host + client session). Not networked, so no Multiplayer Play Mode sign-off needed beyond the usual session check.
**Design ref:** `docs/design/overview.md` (Art style; World: forests)

## Goal
The test level reads as a lit forest clearing instead of a grey plane: trees, rocks, bushes, grass and flowers around a meadow, warm sunlight with soft shadows, a tinted sky, and gentle post-processing.

## Scope
- In:
  - Placeholder art bible (below) and one code source for its colors (`Isekai.Core.Palette`).
  - Free CC0 art: a subset of Quaternius' Stylized Nature MegaKit (standard version, from OpenGameArt): common trees, pines, rocks, bushes, grass, flowers, ferns, clover, plants, mushrooms.
  - TestLevel: a 120 m meadow (palette-tinted noise texture), a ring of trees around the clearing and scattered trees beyond, rocks and bushes, ground cover across the meadow. The four test boxes become stone blocks and the training dummy straw-colored.
  - Trees and rocks block players (colliders) and the enemy (NavMesh rebaked from colliders). Grass, flowers and bushes don't block.
  - Lighting: warm key light with soft shadows, gradient ambient, palette-tinted procedural sky, light distance fog, one global post-processing Volume (ACES tonemapping, bloom, vignette, color adjustments, white balance). Camera post-processing on, SMAA.
- Out:
  - Day/night, weather, foliage wind, baked GI, LODs and culling work, terrain sculpting, water, buildings, audio.
  - World boundaries (players can still walk off the far edge of the meadow).

## Requirements
- No gameplay change. Spawn points, the dummy and the enemy's home stay clear.
- Placement is seeded, so rebuilding gives the same layout.

## Networking
Not networked: scene content is identical on every peer.

## Done when
- [x] Machine scan: no renderer uses a default material, the skybox is the palette sky, camera post-processing is on, the Volume has its overrides. (0 default/stock materials; Sky; post on; Tonemapping, Bloom, Vignette, ColorAdjustments, WhiteBalance all overridden.)
- [x] Captures from the gameplay camera and a character close-up read as a lit stylized forest clearing (scorecard palette, style and lighting axes at 3 or better). (Palette 4, style 4, lighting 4 for the environment; the HUD is spec 09.)
- [x] Volume on/off captures differ clearly. (First pass: mean channel delta 24.6 / 14.6 / 38.9, which looked neon; pulled back one notch, see Differences.)
- [x] Trees and rocks block players; the enemy still chases around them; earlier checks still pass. (NavMesh carved at a trunk; a path past a tree goes around it; the host walking into a trunk stopped 0.92 m from its center.)
- [x] No console errors; the client build still runs. (Client at its 60 fps cap with all 560 props; Editor ~200 fps.)

## Placeholder art bible (from the design doc's Art style; not a design decision)
1. **Style:** stylized low-poly storybook fantasy: chunky rounded shapes, matte surfaces, soft warm light; anything magical glows gold.
2. **Palette:** meadow `#7CC05A` · foliage `#3F9A55` · bark `#8A5A3B` · stone `#A8A296` · sky `#9ED4F2` · shadow `#4A5A86` · sunlight `#FFE8C6` · UI panel `#2A2140` · UI text `#FFF6E5` · accent / glow gold `#FFC94A` · danger `#E0533D` · straw `#D9B26F`.
3. **Mood:** late-morning sun over a forest clearing: warm key from the upper left, cool sky bounce in the shadows, soft haze in the distance.

## Differences from spec
- Volume after tuning: Tonemapping **Neutral** (ACES with saturation +10 turned the meadow neon), Color Adjustments saturation +4 / contrast +5, White Balance +4, Bloom 0.35 @ threshold 1.1, Vignette 0.22. Profile: `Art/Placeholder/Environment/TestLevelVolume.asset`.
- The first meadow texture showed a visible grid; regenerated as smooth diagonal waves (±8%) tiled every 15 m.
- Ground enlarged from 50 m to 120 m; 74 trees (ring at 14–24 m, scattered out to ~55 m), 12 rocks, 20 bushes, ~450 pieces of ground cover; fog from 30 to 85 m hides the far edge.
- The NavMesh is now baked from physics colliders only (trunk capsules, rock mesh colliders, blocks, dummy).
- Bush_Common needed one more leaf texture (`Leaves_TwistedTree_C.png`).
- The training dummy is still a capsule (straw-colored); it reads as a prop, not a character.
- Not done: world boundary, foliage wind, light probes/baked GI.
