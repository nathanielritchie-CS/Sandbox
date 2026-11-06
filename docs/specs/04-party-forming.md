# 04 — Party Forming

**Status:** Verified with the Editor as host and two standalone development builds as clients. Multiplayer Play Mode sign-off pending (yours); set Done after it.
**Design ref:** `docs/design/overview.md` (Co-op)

## Goal
Two players who meet can form a party, more can join later, and everyone in the session can see who is in it.

## Scope
- In:
  - One party per session, up to 4 members (the session itself is up to 4 players).
  - Invite: press E near another player. The server picks the nearest player within 5 m who is not already in the party and sends them an invite. Any party member may invite (placeholder).
  - Accept: the invited player presses E within 15 s. If the inviter had no party, a party forms with the inviter as leader.
  - Leave: press L. A party that drops below 2 members dissolves. A disconnect counts as leaving. If the leader leaves, the next member becomes leader.
  - Party panel (uGUI, legacy Text): party members (leader first, "(you)" marker), the pending invite prompt, and a one-line key hint. Always visible in TestLevel.
- Out:
  - **The pact / initiation.** The design doc leaves it open (for example a blood pact). Invite + accept stands in for it and is the piece to replace.
  - Kick, promote, party chat, shared XP, buffs or any gameplay effect of being in a party.
  - Player names (placeholder "Player N" from the client id), multiple parties per session, the capital entry rule, a decline key (ignore the invite and it expires).
  - Choosing a target by pointing or a menu.

## Requirements
- Roster rules, invite bookkeeping and nearest-player choice are plain C# in Gameplay with EditMode tests.
- Clients send intent only: invite, accept, leave, all without parameters. The server identifies the sender, picks the target, enforces range, size and expiry.
- The party roster is a server-write NetworkList. Only the invited player is told about an invite, by a server-only RPC.
- The panel reads from a Core event channel (UI cannot reference Net).

## Networking
- Server-authoritative state: roster, pending invites, who is invitable, acceptance, expiry.
- Client → server requests: invite, accept, leave (no payload).
- Server → client: "you were invited by X" / "invite cleared" to the target only.
- What each client sees: the full roster (replicated), only its own invite.

## Done when
- [x] Host presses E near the client: the client sees the invite; the client presses E and both show party [host (leader), client]. (The host's E was the key handler called directly, because the Editor ignores simulated keys; every client key press was a real simulated key. The invite went to the nearest player: client 1 at 2 m, not client 2 at 4 m.)
- [x] No one in range, or the target already in the party: E does nothing. An invite expires after 15 s. (Out of range at 10 m: nothing. A non-member's E while a party existed: nothing. An unanswered invite's prompt cleared by itself after 15 s.)
- [x] A third player can be invited by a member and joins; a fourth makes it full (size cap covered by unit tests). (Client 1 invited client 2; all three peers showed [0,1,2].)
- [x] L leaves; a party below 2 members dissolves for everyone. A disconnect removes the player from the party. (Leader left: Player 1 became leader. Then the party dissolved at 1 member. A disconnect left [0,1]; the next one dissolved it.)
- [x] A client cannot join without an invite, cannot be invited by a non-member while a party exists, and cannot call the server-only prompt RPC. (Forged accept with no invite and a late forged accept after expiry: ignored. Forged `ShowInviteRpc`: "This RPC can only be sent by the server.")
- [x] EditMode tests for roster, invites and nearest-player pick pass. (22 new, 49 total.)
- [x] Compiles with zero errors, no new warnings. (Editor and player build.)
- [x] Works for host and connected clients (host + two clients).

## Assumptions (placeholders, not design decisions)
- Invite + accept is not the final pact. It exists so parties can be formed and tested now.
- Range 5 m, expiry 15 s, keys E and L are placeholders.
- The party state lives in a `PartyManager` object in the TestLevel scene (a shortcut: it should move to a persistent, server-spawned object once levels change during a session).
- No rate limit on invites (a client could spam invites at the nearest player). Fine for friends in co-op; revisit before public matchmaking.

## Differences from spec
- Panel layout checked only through the text it shows (no screenshot with a party yet).
- The party panel is a second canvas (`PartyCanvas`) rather than part of `StatusCanvas`.
- Verified with the Editor as host and two headless Windows development builds as clients (`tools/mp-harness/`), not Multiplayer Play Mode.
