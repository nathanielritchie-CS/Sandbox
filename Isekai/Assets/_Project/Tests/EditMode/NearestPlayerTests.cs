using System.Collections.Generic;
using Isekai.Gameplay.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class NearestPlayerTests
    {
        private static PlayerCandidate At(ulong id, float x, float z, float y = 0f) => new(id, new Vector3(x, y, z));

        [Test]
        public void PicksTheNearestWithinRange()
        {
            var candidates = new List<PlayerCandidate> { At(1, 4f, 0f), At(2, 1f, 1f), At(3, 3f, 0f) };
            Assert.That(NearestPlayer.Pick(Vector3.zero, candidates, 5f), Is.EqualTo(2ul));
        }

        [Test]
        public void NobodyInRange_ReturnsNull()
        {
            var candidates = new List<PlayerCandidate> { At(1, 6f, 0f) };
            Assert.That(NearestPlayer.Pick(Vector3.zero, candidates, 5f), Is.Null);
        }

        [Test]
        public void NoCandidates_ReturnsNull() => Assert.That(NearestPlayer.Pick(Vector3.zero, new List<PlayerCandidate>(), 5f), Is.Null);

        [Test]
        public void ExactlyAtRange_IsIncluded() => Assert.That(NearestPlayer.Pick(Vector3.zero, new List<PlayerCandidate> { At(1, 5f, 0f) }, 5f), Is.EqualTo(1ul));

        [Test]
        public void Height_IsIgnored() => Assert.That(NearestPlayer.Pick(Vector3.zero, new List<PlayerCandidate> { At(1, 2f, 0f, y: 40f) }, 5f), Is.EqualTo(1ul));

        [Test]
        public void Tie_KeepsTheFirst()
        {
            var candidates = new List<PlayerCandidate> { At(1, 2f, 0f), At(2, 0f, 2f) };
            Assert.That(NearestPlayer.Pick(Vector3.zero, candidates, 5f), Is.EqualTo(1ul));
        }
    }
}
