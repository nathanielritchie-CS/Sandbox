using Isekai.Gameplay.Combat;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class DamageTests
    {
        [Test]
        public void Damage_SubtractsFromHealth() => Assert.That(Damage.Apply(100, 10), Is.EqualTo(90));

        [Test]
        public void Overkill_ClampsToZero() => Assert.That(Damage.Apply(5, 10), Is.EqualTo(0));

        [Test]
        public void NegativeDamage_DoesNotHeal() => Assert.That(Damage.Apply(50, -10), Is.EqualTo(50));

        [Test]
        public void Defense_RemovesFlatDamage() => Assert.That(Damage.Mitigate(8, 3), Is.EqualTo(5));

        [Test]
        public void HugeDefense_StillDealsOne() => Assert.That(Damage.Mitigate(8, 50), Is.EqualTo(1));

        [Test]
        public void NegativeDefense_IsIgnored() => Assert.That(Damage.Mitigate(8, -4), Is.EqualTo(8));
    }
}
