# 17 — Status Window: System Hologram, Stat Points, Names

**Status:** Built and verified (2026-11-06) with the Editor host + a headless client; waiting for your Multiplayer Play Mode sign-off before Done (networked: points and names are server-owned).
**Design ref:** `docs/design/overview.md` (Otherworlders "see it as a status window, which shows their stats"; "each level gives stat points that the player allocates in the status window")

## Goal
Pressing C at any moment of the adventure opens the Otherworlder's status window: a translucent blue holographic "system" panel floating over the world that shows who you are and how strong you are, and lets you spend the stat points your levels earned.

## Scope
- In:
  - **Look** (the user's pick, "blue system hologram"): dark navy glass you can see the world through, a glowing cyan frame with corner brackets, a "STATUS" header with a glowing rule, faint scanlines, white and cyan text in a squared techy font (Rajdhani, SIL OFL), cyan bars. Opens with a quick scale-in and flicker plus a soft blip; closes quickly.
  - **Content:** name, class ("Unawakened" until classes exist), title ("Otherworlder"), level; HP, stamina and EXP bars with numbers; the seven stats; unspent points with a + button per stat while points remain.
  - **Toggle with C at any time** (in the game level). While open: the cursor shows and is free, you can still walk, roll and sprint, but attacking, lock-on and camera look are off (the mouse belongs to the window). The Esc menu takes over the input while it is open, and closing it returns to whatever the status window needs.
  - **Stat points:** 3 per level gained (data). + spends one on that stat; the server checks there is a point to spend. A stat = base + growth per level + points spent. Attack and Defense already matter in combat; the other five show but have no effect yet.
  - **Your name:** typed on the title screen (max 16 characters, remembered), sent once when your character spawns; the server cleans it (letters, digits, spaces, `-` `_`; empty → "Otherworlder") and everyone receives it. Shown on your status window.
- Out:
  - Status window tabs (inventory and skills come with their specs), names above heads, stat effects for Dexterity/Stealth/Luck/Mana/Stamina, saving progress, resetting points, the window popping up as a tutorial, gamepad navigation.

## Requirements
- Points math is plain C# with tests (points earned by level; can spend).
- UI never references Net: the window reads `PlayerStatusChannel` (now with name and points), `PlayerHealthChannel` and `PlayerStaminaChannel`, and asks for a point through a new `SpendPointChannel` (Text channel: the stat's name); the title screen hands the name over through a new `PlayerNameChannel` (Text channel).
- One small `InputGate` (UI) owns the cursor and which input is on, so the Esc menu and the status window don't fight over it.

## Networking
- Server-authoritative state: points spent per stat (NetworkList), unspent points (derived from level), the cleaned name (NetworkVariable).
- Client → server requests: `SpendPointRpc(stat)` (owner), `SetNameRpc(name)` (owner, at spawn).
- What each client sees: its own status window; everyone's names replicate (for later use).

## Plan (files)
- Core: `PlayerStatus` + name + unspent points.
- Data: `StatsDefinition` + points per level (3).
- Gameplay: `Progression` + points earned; tests.
- Net: `PlayerStats` (allocations, `SpendPointRpc`, name + `SetNameRpc`, owner listens to `SpendPointChannel`).
- UI: `StatusWindow` (rebuilt), new `InputGate` (GameMenu moves onto it), new `NameField` (title screen).
- Assets: Rajdhani font + TMP asset (OFL, license beside it); generated hologram sprites; TestLevel `StatusCanvas` rebuilt; Bootstrap name field; two channel assets.
- Tests: `ProgressionTests` (points).

## Done when
- [x] C opens and closes the hologram at any time; it looks like the pick (capture); the world shows through it. (`s17_window.png`, `s17_window_final.png`: cyan chamfered frame with glow and corner brackets, navy glass at 74% with scanlines, Rajdhani text.)
- [x] While open: the cursor is free, walking works, attacks/lock-on/camera look don't; Esc on top of it and back behaves. (Host: open → cursor `None`, Move on, Attack/LockOn/Look off; Esc → everything off; C ignored while the menu is open; Esc again → back to the window's state; C → closed, cursor `Locked`, all on.)
- [x] Name, level, bars and stats are right and update live (damage, stamina use, XP). (LV. 1 → LV. 3 and points 0 → 6 the moment 300 XP arrived; HP/STA/EXP bars with numbers.)
- [x] Levelling up gives 3 points; + spends one and raises that stat (and Attack raises damage), refused at 0 points (server check, also for a direct RPC); host and client. (Host: + on Attack 7 → 8, server `Get(Attack)` = 8, which is what `PlayerAttack` adds. Client: + on Defense 5 → 6; 8 direct `SpendPointRpc(Luck)` calls with 5 points left → only 5 accepted, points 0, + buttons hidden; the server agreed.)
- [x] A typed name reaches the server cleaned and shows on the window; host and client. (Host typed "  Kai the  Brave!! " → "Kai the Brave"; client "Lina <3 of the Glade" → "Lina 3 of the Gl"; each side sees both.)
- [x] Compiles with zero errors, no new warnings; tests pass (79/79: 2 new in `ProgressionTests`, 5 in `PlayerNameTests`); client build 0 warnings / 0 errors; host console 0 entries, client log clean.

## Open questions
- The user ticked "Other" for more window content, but the text didn't come through; added when they say what it is.

## Differences from spec
- The window floats on the left-middle of the screen (540 px wide) rather than the centre, so your character stays visible while it is open.
- The open blip is the existing UI click pitched up (×1.35); no new sound file.
- Class and title are fixed texts in the scene until classes exist. Bar colours: HP red, stamina green, EXP cyan.
- Names are ASCII only (both fonts are ASCII atlases). The Editor and player builds keep separate PlayerPrefs; the test names typed during verification were deleted again.
- Not verified: a real C key press and real mouse clicks on + (the harness calls `SetOpen` / `OnToggle` and `Button.onClick`), the open animation by eye, Multiplayer Play Mode.
