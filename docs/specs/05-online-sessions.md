# 05 — Online Sessions (join codes)

**Status:** Verified with the Editor as host and a development build as client, both through the cloud. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Co-op)

## Goal
A player hosts an online game and gets a join code; friends type the code to join. No IP addresses, no port forwarding.

## Scope
- In:
  - Bootstrap menu gets a second panel, "Online": a join code field, **Host Online** and **Join Online** buttons, and a status line. The existing direct Host / Join (IP + port) stays for LAN and testing.
  - Host Online: sign in anonymously, create a private 4-player session with brokered connectivity (the multiplayer package also starts Netcode as host), load TestLevel like the direct flow. The join code is shown in TestLevel (top-left).
  - Join Online: sign in anonymously, join by code; Netcode starts as client and the host's scene sync brings the client into TestLevel.
  - The menu shows progress and failures ("Creating game...", "Failed: ...").
  - The session is deleted (host) or left (client) when the game object is destroyed.
- Out:
  - Browsing a list of open games, quick join, automatic matchmaking, passwords, reconnecting after a crash, moving the host, player names, platform accounts (Steam etc.).
  - Changing anything after connection: movement, combat, stats and parties work as before.

## Requirements
- Direct connect keeps working unchanged (`ConnectionController`, `ConnectionRequestChannel`).
- UI cannot reference Net: the menu sends a request through a Core channel and shows feedback from a Core text channel.
- A failed request (no network, services unavailable, bad code) shows a message and leaves the menu usable.
- Only one request runs at a time.

## Networking
- Not a gameplay change. Server authority and the networking rules are as before; the session only decides how the client reaches the host.
- Join code is the only barrier to joining (private session, max 4 players). No password yet.

## Done when
- [x] Host Online creates a session and shows a join code in TestLevel; the host's player spawns as with direct hosting. (Clicked through the button's own click event; the label reads "Join code: 9BNLM6" in the HUD.)
- [x] A client enters the code, joins, lands in TestLevel, and both see each other (positions replicate). (Code typed in lowercase with spaces was normalised. A client attack over this connection took the dummy to 90 on both sides.)
- [x] A wrong or empty code shows a failure message and the menu still works. ("Enter a join code.", "Failed: session code 'ZZZZZZ' contains an invalid character…", "Failed: session not found"; the real code worked afterwards without reloading.)
- [x] Direct Host / Join still works (regression with the harness).
- [x] Compiles with zero errors, no new warnings. (Editor and player build.)
- [x] Verified with the Editor as host and a development build as client, both going through the cloud. (Same machine, so not tested across two home networks.)

## Assumptions (placeholders, not design decisions)
- Private session, 4 players, anonymous sign-in. Stable accounts and friends lists come with a later account design.
- The session is deleted when the host's game ends; a host crash leaves it to expire on the service side.
- Needs internet and the project's cloud services enabled (they are: a probe created and deleted a session before this spec).

## Differences from spec
- Clients also show the join code in the HUD (the code they typed), so any player can pass it on, not just the host.
- The Online panel is a clone of the existing menu placed beside it; `ConnectionMenu` and `ConnectionController` were not changed. `SessionController` sits on the persistent `NetworkManager` object, because the Bootstrap scene unloads when the level loads.
- The join code label has a black outline (white text was unreadable on the bright sky).
- Stopping the host deleted the session: rejoining the old code from the Editor gave "session not found".
- Not tested: a client on another network, two online clients on one PC (they may share one anonymous sign-in because both read the same saved player data), a host crash.
- Unity Services only initialise in Play Mode, so nothing here can be tried in Edit Mode.
