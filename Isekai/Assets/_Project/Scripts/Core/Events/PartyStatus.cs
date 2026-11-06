namespace Isekai.Core.Events
{
    // What the party panel shows. Built by the Net layer, read by UI.
    public sealed class PartyStatus
    {
        public readonly string[] Members;   // display names, leader first; empty when not in a party
        public readonly int Self;           // index of the local player in Members, or -1
        public readonly string InviteFrom;  // who invited the local player, or null

        public PartyStatus(string[] members, int self, string inviteFrom)
        {
            Members = members;
            Self = self;
            InviteFrom = inviteFrom;
        }
    }
}
