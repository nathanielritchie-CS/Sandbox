using System.Collections.Generic;

namespace Isekai.Gameplay.Party
{
    // One party per session. The first member is the leader.
    public class PartyRoster
    {
        public const int MaxSize = 4;

        private readonly List<ulong> _members = new();

        public IReadOnlyList<ulong> Members => _members;

        public bool Contains(ulong id) => _members.Contains(id);

        // A member may invite while there is room; anyone may invite while no party exists.
        public bool CanInvite(ulong inviter) => _members.Count == 0 || (Contains(inviter) && _members.Count < MaxSize);

        // Adds newcomer to inviter's party, creating it (inviter as leader) if none exists.
        public bool TryJoin(ulong inviter, ulong newcomer)
        {
            if (inviter == newcomer || Contains(newcomer) || !CanInvite(inviter)) return false;
            if (_members.Count == 0) _members.Add(inviter);
            _members.Add(newcomer);
            return true;
        }

        // Removes a member; a party below 2 members dissolves. Returns true if anything changed.
        public bool Remove(ulong id)
        {
            if (!_members.Remove(id)) return false;
            if (_members.Count < 2) _members.Clear();
            return true;
        }
    }
}
