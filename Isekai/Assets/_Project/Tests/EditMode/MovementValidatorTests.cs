using Isekai.Gameplay.Movement;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class MovementValidatorTests
    {
        [Test]
        public void MoveWithinSpeed_IsValid()
        {
            Assert.That(MovementValidator.IsWithinSpeed(Vector3.zero, new Vector3(3f, 0f, 0f), 0.5f, 7f), Is.True);
        }

        [Test]
        public void MoveFasterThanMax_IsInvalid()
        {
            Assert.That(MovementValidator.IsWithinSpeed(Vector3.zero, new Vector3(4f, 0f, 0f), 0.5f, 7f), Is.False);
        }

        [Test]
        public void ExactlyMaxSpeed_IsValid()
        {
            Assert.That(MovementValidator.IsWithinSpeed(Vector3.zero, new Vector3(0f, 0f, 3.5f), 0.5f, 7f), Is.True);
        }

        [Test]
        public void FastVerticalFall_IsIgnored()
        {
            Assert.That(MovementValidator.IsWithinSpeed(Vector3.zero, new Vector3(0f, -20f, 0f), 0.5f, 7f), Is.True);
        }
    }
}
