# 11 — Placeholder Audio

**Status:** Done (2026-11-06). Presentation only (no network code changed); verified with the Editor host + a headless client (harness), the listener's measured output and the console.
**Design ref:** `docs/design/overview.md` (Combat: "satisfying"; Tone)

## Goal
The game stops being silent: swings, hits, deaths, level-ups, menu clicks and the meadow make sounds on every peer, through one mixer with volume handles for a later settings menu.

## Scope
- In:
  - CC0 placeholder clips from OpenGameArt, license file beside each pack: player swing ×3, Demon attack growl ×3, hit ×3, deaths (player fall, Demon roar, dummy wood break), level-up rise, UI click, a looping morning-meadow ambience.
  - One AudioMixer `Audio/MainMixer.mixer`: Master → Music, SFX, Ambient. The Music and SFX volumes are exposed as `MusicVol` / `SFXVol` (the names a later settings menu binds to). Every AudioSource routes to a group.
  - Combat sounds on every peer, 3D (full volume within 6 m, no Doppler): a swing or growl when a character attacks (`CharacterAnimator.AttackRpc`), a hit when its health drops and a death sound at 0 (`HitFeedback`). Random clip and ±8% pitch per play.
  - Level-up sting with the local player's level-up moment (2D).
  - Click on every Bootstrap menu button (2D); Bootstrap gets an AudioListener (it has no camera).
  - Ambience loop in TestLevel (2D, Ambient group).
  - `Isekai.Core.Sfx.PlayRandom` (random clip, pitch jitter).
- Out:
  - Music (the group and `MusicVol` exist, no track yet), ducking (nothing to duck without music), footsteps, settings sliders (game-shell spec), party/notification chimes, status-window sounds, per-surface impacts, voices.

## Requirements
- Presentation only: sounds react to already-replicated events (attack RPC, `NetworkHealth`, XP); no new RPCs or NetworkVariables.
- Import settings per `optimize-audio`: short 3D clips mono and Decompress On Load; the ambience streams.
- Exactly one AudioListener in each scene (TestLevel's camera, Bootstrap's menu).

## Networking
- Not networked beyond presentation. Each peer plays sounds from replicated state: the server's attack RPC → swing/growl everywhere; replicated health → hit/death everywhere; the local player's XP → level-up sting on that player only.

## Plan (files)
- Core: new `Sfx`.
- Net: `CharacterAnimator` (+ audio source, attack clips), `HitFeedback` (+ audio source, hit and death clips).
- UI: `LevelUpMoment` (+ sting source); new `ButtonSounds`.
- Assets: `Audio/Placeholder/*` clips + licenses, `Audio/MainMixer.mixer`; `Player.prefab` (AudioSource); TestLevel (Enemy and TrainingDummy AudioSources, `Ambience`, sting source on `LevelUpCanvas`); Bootstrap (`ConnectionCanvas`: AudioListener, click source, `ButtonSounds`).

## Done when
- [x] Host and client: an attack plays a swing (players) or a growl (Demon) on every peer; hits and deaths play their sounds on every peer. (Host: own swing playing 80 ms after the attack, the dummy's hit at the impact. Client: heard the host's swing, then the dummy's hit 250 ms later; its own swing 23 ms after attacking; the Demon's growl, then the host player's hit 283 ms later. Dummy kills on host and client; the death layer plays with the hit.)
- [x] A level-up plays the sting for the leveling player. (Host and client each reached level 2; the sting played on each.)
- [x] A Bootstrap button click plays the click. (Clicked the real Host button: click playing, then the host started.)
- [x] The ambience loops in TestLevel. (Playing and advancing on host and client; loop on.)
- [x] Every AudioSource in Bootstrap, TestLevel and `Player.prefab` has a mixer group; `MusicVol` / `SFXVol` respond to `SetFloat`. (Host 5/5, client 6/6 with both players, Bootstrap 1/1; both `SetFloat` calls true, read back −10 dB.)
- [x] The listener's output is not silent during a hit; one AudioListener per scene, no "2 audio listeners" warning. (`GetOutputData` RMS: 0.010 ambience only → 0.055 swing → 0.037 hit; one listener in Bootstrap and TestLevel on host and client; 0 warnings through the Bootstrap → TestLevel switch.)
- [x] Compiles with zero errors, no new warnings; EditMode tests pass (56/56); console clean; client build 0 warnings / 0 errors; host + client verified.

## Open questions
- None blocking. Clips are picked by length and balanced by measured level; your ear has the final say.

## Differences from spec
- **The mixer was made through the Editor's internal API.** Unity has no public API for creating mixers or groups; the `audio-setup-mixers` skill says not to use reflection for it, and the floor's fallback was a script-side volume layer. `MainMixer.mixer` was made with the same internal calls the Editor's Create menu and the Audio Mixer window's "+" use, then verified in Play Mode (routing, `SetFloat`, audible output). Open it once in Window > Audio > Audio Mixer to confirm; if you'd rather, recreate it by hand (Master → Music, SFX, Ambient; expose the Music and SFX volumes as `MusicVol` / `SFXVol`) and re-point the five sources.
- A death plays the hit **plus** the death sound (layered): the player's fall sound alone is quiet (RMS 0.031).
- Attack sounds play at half volume (`CharacterAnimator._attackVolume` 0.5): the swings and growls measured ~6 dB louder than the hits, and the hit is the payoff.
- Levels: mixer groups at 0 dB; ambience source 0.5 (≈0.011 RMS at the listener, a quiet bed); menu click 0.6; everything else 1. 3D sources: logarithmic rolloff, full volume within 6 m, 40 m max, Doppler off.
- Clips: swing/swing2/swing3, mnstr1/12/15 (Demon attack), mnstr14 (Demon death), bfh1_hit_03/08/13, bfh1_wood_breaking_03 (dummy death), bfh1_falling_03 (player death), Rise02 (level-up), interface1 (click), amb_morning (ambience). Import: short clips mono + Decompress On Load + Vorbis 0.7; Rise02 stereo + Compressed In Memory; ambience stereo + Streaming + Load In Background + Vorbis 0.6.
- On the client the dummy's hit came 250 ms after the host's swing (360 ms on the host). Probably the unfocused Editor host's frame pacing delaying the swing message; not investigated.
- Not verified: how it sounds (no ears here; levels were balanced by measurement); the Demon's death roar and the player's death fall separately (same code path as the dummy's death, different clips); the visual scorecard (no visual change).
