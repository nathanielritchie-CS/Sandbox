using System.Collections.Generic;

namespace Isekai.Gameplay.Party
{
    // At most one pending invite per target; a newer invite replaces the older one.
    public class PendingInvites
    {
        private readonly Dictionary<ulong, (ulong inviter, float expiresAt)> _byTarget = new();
        private readonly List<ulong> _scratch = new();

        public void Add(ulong inviter, ulong target, float now, float lifetimeSeconds) =>
            _byTarget[target] = (inviter, now + lifetimeSeconds);

        // Takes the target's invite if it is still valid. An expired one is dropped and reported as missing.
        public bool TryConsume(ulong target, float now, out ulong inviter)
        {
            inviter = 0;
            if (!_byTarget.Remove(target, out var invite) || now >= invite.expiresAt) return false;
            inviter = invite.inviter;
            return true;
        }

        // Drops invites that expired and lists their targets (cleared first).
        public void CollectExpired(float now, List<ulong> targets)
        {
            targets.Clear();
            _scratch.Clear();
            foreach (var pair in _byTarget)
                if (now >= pair.Value.expiresAt) _scratch.Add(pair.Key);
            foreach (var target in _scratch)
            {
                _byTarget.Remove(target);
                targets.Add(target);
            }
        }

        // Drops every invite to or from this client; lists the other targets that lost an invite (cleared first).
        public void Remove(ulong clientId, List<ulong> affectedTargets)
        {
            affectedTargets.Clear();
            _scratch.Clear();
            foreach (var pair in _byTarget)
                if (pair.Key == clientId || pair.Value.inviter == clientId) _scratch.Add(pair.Key);
            foreach (var target in _scratch)
            {
                _byTarget.Remove(target);
                if (target != clientId) affectedTargets.Add(target);
            }
        }
    }
}
