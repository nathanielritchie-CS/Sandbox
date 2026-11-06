using System.Linq;
using Isekai.Gameplay.Party;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class PartyRosterTests
    {
        [Test]
        public void FirstJoin_CreatesPartyWithInviterAsLeader()
        {
            var roster = new PartyRoster();
            Assert.That(roster.TryJoin(0, 1), Is.True);
            Assert.That(roster.Members.ToArray(), Is.EqualTo(new ulong[] { 0, 1 }));
        }

        [Test]
        public void MemberCanInviteMore()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            Assert.That(roster.TryJoin(1, 2), Is.True);
            Assert.That(roster.Members.ToArray(), Is.EqualTo(new ulong[] { 0, 1, 2 }));
        }

        [Test]
        public void NonMember_CannotInviteIntoExistingParty()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            Assert.That(roster.CanInvite(2), Is.False);
            Assert.That(roster.TryJoin(2, 3), Is.False);
        }

        [Test]
        public void FullParty_RejectsFifthMember()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            roster.TryJoin(0, 2);
            roster.TryJoin(0, 3);
            Assert.That(roster.CanInvite(0), Is.False);
            Assert.That(roster.TryJoin(0, 4), Is.False);
            Assert.That(roster.Members.Count, Is.EqualTo(PartyRoster.MaxSize));
        }

        [Test]
        public void AlreadyMember_CannotJoinAgain()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            Assert.That(roster.TryJoin(0, 1), Is.False);
            Assert.That(roster.Members.Count, Is.EqualTo(2));
        }

        [Test]
        public void CannotInviteSelf() => Assert.That(new PartyRoster().TryJoin(5, 5), Is.False);

        [Test]
        public void LeaderLeaves_NextMemberBecomesLeader()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            roster.TryJoin(0, 2);
            roster.Remove(0);
            Assert.That(roster.Members.ToArray(), Is.EqualTo(new ulong[] { 1, 2 }));
        }

        [Test]
        public void PartyBelowTwo_Dissolves()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            Assert.That(roster.Remove(1), Is.True);
            Assert.That(roster.Members.Count, Is.EqualTo(0));
            Assert.That(roster.CanInvite(0), Is.True);
        }

        [Test]
        public void RemoveUnknown_ChangesNothing()
        {
            var roster = new PartyRoster();
            roster.TryJoin(0, 1);
            Assert.That(roster.Remove(9), Is.False);
            Assert.That(roster.Members.Count, Is.EqualTo(2));
        }
    }
}
