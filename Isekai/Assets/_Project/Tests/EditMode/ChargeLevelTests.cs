using Isekai.Gameplay.Combat;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class ChargeLevelTests
    {
        private static int Damage(float seconds) => ChargeLevel.Damage(seconds, 0.35f, 0.75f, 15, 25);

        [Test]
        public void BeforeReady_DealsNothing() => Assert.AreEqual(0, Damage(0.34f));

        [Test]
        public void AtReady_DealsTheMinimum() => Assert.AreEqual(15, Damage(0.35f));

        [Test]
        public void Halfway_DealsInBetween() => Assert.AreEqual(20, Damage(0.55f));

        [Test]
        public void PastFull_StaysAtTheMaximum()
        {
            Assert.AreEqual(25, Damage(0.75f));
            Assert.AreEqual(25, Damage(5f));
        }
    }
}
