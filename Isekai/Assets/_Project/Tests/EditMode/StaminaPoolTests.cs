using Isekai.Gameplay.Movement;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class StaminaPoolTests
    {
        private static StaminaPool Pool() => new StaminaPool(100f, 35f, 0.8f);

        [Test]
        public void StartsFull() => Assert.AreEqual(100f, Pool().Current);

        [Test]
        public void TrySpend_TakesTheWholeCost_OrNothing()
        {
            var pool = Pool();
            Assert.IsTrue(pool.TrySpend(30f));
            Assert.AreEqual(70f, pool.Current, 0.001f);
            pool.Drain(60f);
            Assert.IsFalse(pool.TrySpend(30f));
            Assert.AreEqual(10f, pool.Current, 0.001f);
        }

        [Test]
        public void Drain_StopsAtZero()
        {
            var pool = Pool();
            pool.Drain(150f);
            Assert.AreEqual(0f, pool.Current);
        }

        [Test]
        public void Refill_WaitsForTheDelay_ThenStopsAtMax()
        {
            var pool = Pool();
            pool.Drain(50f);
            pool.Tick(0.5f);
            Assert.AreEqual(50f, pool.Current, 0.001f);   // still inside the 0.8 s pause
            pool.Tick(0.5f);
            Assert.AreEqual(67.5f, pool.Current, 0.001f); // 35/s for 0.5 s
            pool.Tick(10f);
            Assert.AreEqual(100f, pool.Current);
        }
    }
}
