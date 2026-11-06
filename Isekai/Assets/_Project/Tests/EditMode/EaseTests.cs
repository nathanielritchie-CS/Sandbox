using Isekai.Core;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class EaseTests
    {
        [Test]
        public void OutCubic_StartsAtZero_EndsAtOne()
        {
            Assert.That(Ease.OutCubic(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Ease.OutCubic(1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void OutCubic_IsAheadOfLinearMidway() => Assert.That(Ease.OutCubic(0.5f), Is.EqualTo(0.875f).Within(1e-5f));

        [Test]
        public void OutBack_StartsAtZero_EndsAtOne()
        {
            Assert.That(Ease.OutBack(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(Ease.OutBack(1f), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void OutBack_OvershootsAboutTenPercent()
        {
            var peak = 0f;
            for (var i = 0; i <= 100; i++) peak = System.Math.Max(peak, Ease.OutBack(i / 100f));
            Assert.That(peak, Is.InRange(1.05f, 1.15f));
        }
    }
}
