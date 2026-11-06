using Isekai.Gameplay.Movement;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class MoveDirectionTests
    {
        [Test]
        public void NoInput_ReturnsZero()
        {
            Assert.That(MoveDirection.FromInput(Vector2.zero, 45f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Forward_WithCameraYawZero_IsWorldForward()
        {
            AssertNear(Vector3.forward, MoveDirection.FromInput(Vector2.up, 0f));
        }

        [Test]
        public void Forward_WithCameraYaw90_IsWorldRight()
        {
            AssertNear(Vector3.right, MoveDirection.FromInput(Vector2.up, 90f));
        }

        [Test]
        public void Diagonal_IsClampedToLengthOne()
        {
            Assert.That(MoveDirection.FromInput(new Vector2(1f, 1f), 0f).magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void PartialStick_KeepsItsLength()
        {
            Assert.That(MoveDirection.FromInput(new Vector2(0f, 0.5f), 30f).magnitude, Is.EqualTo(0.5f).Within(1e-5f));
        }

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.That(Vector3.Distance(expected, actual), Is.LessThan(1e-5f), $"expected {expected}, got {actual}");
        }
    }
}
