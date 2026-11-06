using Isekai.Gameplay.Combat;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class ComboChainTests
    {
        [Test]
        public void FirstAttack_IsStepOne() => Assert.AreEqual(1, new ComboChain(3, 1f).Next(10f));

        [Test]
        public void AttacksWithinTheWindow_ChainAndWrap()
        {
            var chain = new ComboChain(3, 1f);
            Assert.AreEqual(1, chain.Next(0f));
            Assert.AreEqual(2, chain.Next(0.6f));
            Assert.AreEqual(3, chain.Next(1.2f));
            Assert.AreEqual(1, chain.Next(1.8f));
        }

        [Test]
        public void APauseLongerThanTheWindow_StartsOver()
        {
            var chain = new ComboChain(3, 1f);
            chain.Next(0f);
            chain.Next(0.5f);
            Assert.AreEqual(1, chain.Next(2f));
        }

        [Test]
        public void Reset_StartsOver()
        {
            var chain = new ComboChain(3, 1f);
            chain.Next(0f);
            chain.Reset();
            Assert.AreEqual(1, chain.Next(0.2f));
        }
    }
}
