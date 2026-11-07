using Isekai.Gameplay.Players;
using NUnit.Framework;

namespace Isekai.Tests.EditMode
{
    public class PlayerNameTests
    {
        [Test]
        public void PlainName_IsKept() => Assert.AreEqual("Kaito_7", PlayerName.Clean("Kaito_7"));

        [Test]
        public void Spaces_AreTrimmedAndCollapsed() => Assert.AreEqual("Sir Kai", PlayerName.Clean("  Sir    Kai  "));

        [Test]
        public void OddCharacters_AreDropped() => Assert.AreEqual("Kai", PlayerName.Clean("K<a>i!\n✨é"));

        [Test]
        public void LongName_IsCut() => Assert.AreEqual(PlayerName.MaxLength, PlayerName.Clean(new string('a', 40)).Length);

        [Test]
        public void EmptyOrNull_BecomesTheDefault()
        {
            Assert.AreEqual(PlayerName.Default, PlayerName.Clean("   "));
            Assert.AreEqual(PlayerName.Default, PlayerName.Clean(null));
        }
    }
}
