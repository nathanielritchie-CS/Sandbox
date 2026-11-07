using Isekai.Gameplay.Appearance;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class AppearanceCodecTests
    {
        private static readonly Color32[] Defaults = { new(10, 20, 30, 255), new(40, 50, 60, 255), new(70, 80, 90, 255) };

        [Test]
        public void EncodeThenDecode_GivesTheSameColours()
        {
            var look = new Color32[] { new(255, 0, 0, 255), new(0, 255, 0, 255), new(1, 2, 3, 255) };
            Assert.AreEqual(look, AppearanceCodec.Decode(AppearanceCodec.Encode(look), Defaults));
        }

        [Test]
        public void Garbage_GivesTheDefaults()
        {
            Assert.AreEqual(Defaults, AppearanceCodec.Decode(null, Defaults));
            Assert.AreEqual(Defaults, AppearanceCodec.Decode(new byte[] { 99, 3, 1, 2, 3 }, Defaults));      // unknown version
            Assert.AreEqual(Defaults, AppearanceCodec.Decode(new byte[AppearanceCodec.MaxBytes + 1], Defaults)); // too long
        }

        [Test]
        public void ShortData_FillsTheRestWithDefaults()
        {
            var decoded = AppearanceCodec.Decode(new byte[] { AppearanceCodec.Version, 3, 200, 201, 202 }, Defaults);
            Assert.AreEqual(new Color32(200, 201, 202, 255), decoded[0]);
            Assert.AreEqual(Defaults[1], decoded[1]);
            Assert.AreEqual(Defaults[2], decoded[2]);
        }

        [Test]
        public void ExtraColours_AreIgnored()
        {
            var look = new Color32[] { new(1, 1, 1, 255), new(2, 2, 2, 255), new(3, 3, 3, 255), new(4, 4, 4, 255) };
            Assert.AreEqual(3, AppearanceCodec.Decode(AppearanceCodec.Encode(look), Defaults).Length);
        }

        [Test]
        public void Text_RoundTripsAndRejectsNonsense()
        {
            var bytes = AppearanceCodec.Encode(Defaults);
            Assert.AreEqual(bytes, AppearanceCodec.FromText(AppearanceCodec.ToText(bytes)));
            Assert.IsEmpty(AppearanceCodec.FromText("not base64 !!"));
        }
    }
}
