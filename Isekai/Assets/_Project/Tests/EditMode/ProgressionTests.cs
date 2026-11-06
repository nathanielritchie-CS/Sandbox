using Isekai.Gameplay.Stats;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class ProgressionTests
    {
        [Test]
        public void NoXp_IsLevelOne()
        {
            var p = Progression.FromTotalXp(0, 100);
            Assert.That((p.Level, p.XpIntoLevel, p.XpToNext), Is.EqualTo((1, 0, 100)));
        }

        [Test]
        public void JustBelowThreshold_StaysAtLevelOne()
        {
            var p = Progression.FromTotalXp(99, 100);
            Assert.That((p.Level, p.XpIntoLevel, p.XpToNext), Is.EqualTo((1, 99, 100)));
        }

        [Test]
        public void ExactThreshold_LevelsUpAndResetsProgress()
        {
            var p = Progression.FromTotalXp(100, 100);
            Assert.That((p.Level, p.XpIntoLevel, p.XpToNext), Is.EqualTo((2, 0, 200)));
        }

        [Test]
        public void CostGrowsWithLevel()
        {
            Assert.That(Progression.FromTotalXp(299, 100).Level, Is.EqualTo(2));
            Assert.That(Progression.FromTotalXp(300, 100).Level, Is.EqualTo(3));
            Assert.That(Progression.FromTotalXp(600, 100).Level, Is.EqualTo(4));
        }

        [Test]
        public void NegativeXp_IsTreatedAsZero() => Assert.That(Progression.FromTotalXp(-50, 100).Level, Is.EqualTo(1));

        [Test]
        public void ZeroXpPerLevel_DoesNotHang() => Assert.That(Progression.FromTotalXp(10, 0).Level, Is.EqualTo(5));

        [Test]
        public void StatAtLevelOne_IsBase() => Assert.That(Progression.StatAtLevel(5, 2, 1), Is.EqualTo(5));

        [Test]
        public void StatGrowsPerLevel() => Assert.That(Progression.StatAtLevel(5, 2, 4), Is.EqualTo(11));
    }
}
