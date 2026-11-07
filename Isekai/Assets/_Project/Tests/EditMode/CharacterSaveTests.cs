using System.IO;
using Isekai.Gameplay.Appearance;
using Isekai.Gameplay.Players;
using NUnit.Framework;
using UnityEngine;

namespace Isekai.Tests.EditMode
{
    public class CharacterSaveTests
    {
        private static readonly string[] Ids = { "skin", "hair" };
        private static readonly Color32[] Defaults = { new(10, 10, 10, 255), new(20, 20, 20, 255) };
        private string _path;

        [SetUp]
        public void SetUp() => _path = Path.Combine(Path.GetTempPath(), "otherworlder_test_" + System.Guid.NewGuid() + ".json");

        [TearDown]
        public void TearDown() => CharacterSave.Delete(_path);

        [Test]
        public void SaveThenLoad_KeepsNameAndColours()
        {
            var look = new Color32[] { new(250, 200, 150, 255), new(30, 40, 50, 255) };
            CharacterSave.Create("  Kai  ", Ids, look).Save(_path);
            var loaded = CharacterSave.Load(_path);
            Assert.AreEqual("Kai", loaded.Name);
            Assert.AreEqual(look, loaded.ColorsFor(Ids, Defaults));
        }

        [Test]
        public void SlotsAddedLater_GetTheirDefaults_UnknownSlotsAreIgnored()
        {
            CharacterSave.Create("Kai", new[] { "hair", "wings" }, new Color32[] { new(1, 2, 3, 255), new(9, 9, 9, 255) }).Save(_path);
            var colors = CharacterSave.Load(_path).ColorsFor(Ids, Defaults);
            Assert.AreEqual(Defaults[0], colors[0]);
            Assert.AreEqual(new Color32(1, 2, 3, 255), colors[1]);
        }

        [Test]
        public void MissingOrDamagedFile_IsNoCharacter()
        {
            Assert.IsNull(CharacterSave.Load(_path));
            File.WriteAllText(_path, "{ this is not json");
            Assert.IsNull(CharacterSave.Load(_path));
        }

        [Test]
        public void EmptyName_BecomesTheDefault() => Assert.AreEqual(PlayerName.Default, CharacterSave.Create("", Ids, Defaults).Name);
    }
}
