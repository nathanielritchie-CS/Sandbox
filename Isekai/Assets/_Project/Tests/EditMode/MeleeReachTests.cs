using Isekai.Gameplay.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class MeleeReachTests
    {
        private static bool Reach(Vector3 target) => MeleeReach.IsInReach(Vector3.zero, Vector3.forward, target, 2f, 90f);

        [Test]
        public void TargetInFront_IsInReach() => Assert.That(Reach(new Vector3(0f, 0f, 1.5f)), Is.True);

        [Test]
        public void TargetTooFar_IsNotInReach() => Assert.That(Reach(new Vector3(0f, 0f, 2.5f)), Is.False);

        [Test]
        public void TargetBehind_IsNotInReach() => Assert.That(Reach(new Vector3(0f, 0f, -1f)), Is.False);

        [Test]
        public void TargetJustInsideArc_IsInReach() => Assert.That(Reach(new Vector3(0.9f, 0f, 1f)), Is.True);

        [Test]
        public void TargetJustOutsideArc_IsNotInReach() => Assert.That(Reach(new Vector3(1f, 0f, 0.9f)), Is.False);

        [Test]
        public void TargetHeight_IsIgnored() => Assert.That(Reach(new Vector3(0f, 5f, 1.5f)), Is.True);
    }
}
