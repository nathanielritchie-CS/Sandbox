using System.Collections.Generic;
using Isekai.Gameplay.Party;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class PendingInvitesTests
    {
        [Test]
        public void ValidInvite_IsConsumedOnce()
        {
            var invites = new PendingInvites();
            invites.Add(0, 1, 10f, 15f);
            Assert.That(invites.TryConsume(1, 12f, out var inviter), Is.True);
            Assert.That(inviter, Is.EqualTo(0ul));
            Assert.That(invites.TryConsume(1, 12f, out _), Is.False);
        }

        [Test]
        public void NoInvite_CannotBeConsumed() => Assert.That(new PendingInvites().TryConsume(1, 0f, out _), Is.False);

        [Test]
        public void ExpiredInvite_IsRefused()
        {
            var invites = new PendingInvites();
            invites.Add(0, 1, 10f, 15f);
            Assert.That(invites.TryConsume(1, 25f, out _), Is.False);
        }

        [Test]
        public void NewerInvite_ReplacesOlder()
        {
            var invites = new PendingInvites();
            invites.Add(0, 2, 10f, 15f);
            invites.Add(1, 2, 11f, 15f);
            invites.TryConsume(2, 12f, out var inviter);
            Assert.That(inviter, Is.EqualTo(1ul));
        }

        [Test]
        public void CollectExpired_ListsAndDropsOnlyExpired()
        {
            var invites = new PendingInvites();
            invites.Add(0, 1, 10f, 5f);
            invites.Add(0, 2, 10f, 20f);
            var expired = new List<ulong>();
            invites.CollectExpired(16f, expired);
            Assert.That(expired, Is.EqualTo(new List<ulong> { 1 }));
            Assert.That(invites.TryConsume(2, 16f, out _), Is.True);
        }

        [Test]
        public void Remove_DropsInvitesToAndFromThatClient()
        {
            var invites = new PendingInvites();
            invites.Add(0, 1, 10f, 15f); // 0 -> 1
            invites.Add(2, 3, 10f, 15f); // 2 -> 3
            var affected = new List<ulong>();
            invites.Remove(0, affected);
            Assert.That(affected, Is.EqualTo(new List<ulong> { 1 }));
            Assert.That(invites.TryConsume(1, 11f, out _), Is.False);
            Assert.That(invites.TryConsume(3, 11f, out _), Is.True);
        }

        [Test]
        public void Remove_TargetDisconnecting_DoesNotListItself()
        {
            var invites = new PendingInvites();
            invites.Add(0, 1, 10f, 15f);
            var affected = new List<ulong>();
            invites.Remove(1, affected);
            Assert.That(affected, Is.Empty);
            Assert.That(invites.TryConsume(1, 11f, out _), Is.False);
        }
    }
}
