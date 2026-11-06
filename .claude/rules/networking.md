---
paths:
  - "Isekai/Assets/_Project/Scripts/Net/**"
  - "Isekai/Assets/_Project/Scripts/Gameplay/**"
  - "Isekai/Assets/_Project/Tests/PlayMode/**"
---
# Networking rules

Stack: Netcode for GameObjects (host mode), Multiplayer Services for sessions.

- The server (host) is the only authority on gameplay state: health, damage, inventory, loot, XP, enemy AI, spawning.
- Clients send intent only ("I pressed attack", "move in this direction") through RPCs. The server validates and applies it.
- Never trust client-supplied values for damage, position jumps, cooldowns, or item counts. Check them on the server.
- NetworkVariables are server-write only (the default). Don't change write permission without asking.
- Owner-authoritative movement is allowed only for the local player's own character, and the server still validates it.
- Keep game logic in plain C# classes in Gameplay. NetworkBehaviours in Net stay thin and call into them.
- Only the server spawns and despawns NetworkObjects.
- Verify every networked feature with a host plus one client in Multiplayer Play Mode.
