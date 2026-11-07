using System.Collections.Generic;
using Isekai.Gameplay.Targeting;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class LockOnPickerTests
    {
        // Camera 5 m behind and 2 m above the player, looking straight ahead (+z).
        private static readonly Vector3 Camera = new(0f, 2f, -5f);

        private static int Pick(params Vector3[] candidates) =>
            LockOnPicker.Pick(Camera, Vector3.forward, Vector3.zero, new List<Vector3>(candidates), 15f, 45f);

        [Test]
        public void PicksTheOneNearestTheMiddleOfTheScreen() =>
            Assert.AreEqual(1, Pick(new Vector3(4f, 1f, 4f), new Vector3(0.5f, 1f, 8f)));

        [Test]
        public void OutOfRange_IsIgnoredEvenInTheMiddle() =>
            Assert.AreEqual(1, Pick(new Vector3(0f, 1f, 20f), new Vector3(4f, 1f, 4f)));

        [Test]
        public void BehindTheCamera_IsIgnored() => Assert.AreEqual(-1, Pick(new Vector3(0f, 1f, -8f)));

        [Test]
        public void NoCandidates_PicksNothing() => Assert.AreEqual(-1, Pick());
    }
}
