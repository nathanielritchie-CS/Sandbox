using System;
using System.Collections.Generic;
using System.IO;
using Isekai.Gameplay.Players;
using UnityEngine;

namespace Isekai.Gameplay.Appearance
{
    // The player's Otherworlder on this PC: their name and look. Colours are stored by slot id, not position,
    // so options added later never scramble an existing character.
    [Serializable]
    public class CharacterSave
    {
        [Serializable]
        private class SlotColor
        {
            public string Id;
            public string Hex;
        }

        [SerializeField] private string _name;
        [SerializeField] private List<SlotColor> _colors = new();

        public string Name => _name;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "otherworlder.json");

        public static CharacterSave Create(string typedName, IReadOnlyList<string> ids, IReadOnlyList<Color32> colors)
        {
            var save = new CharacterSave { _name = PlayerName.Clean(typedName) };
            for (var i = 0; i < ids.Count && i < colors.Count; i++)
                save._colors.Add(new SlotColor { Id = ids[i], Hex = ColorUtility.ToHtmlStringRGB(colors[i]) });
            return save;
        }

        // A missing or damaged file counts as no character.
        public static CharacterSave Load(string path = null)
        {
            path ??= DefaultPath;
            if (!File.Exists(path)) return null;
            try
            {
                var save = JsonUtility.FromJson<CharacterSave>(File.ReadAllText(path));
                return save != null && !string.IsNullOrEmpty(save._name) ? save : null;
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                return null;
            }
        }

        public void Save(string path = null) => File.WriteAllText(path ?? DefaultPath, JsonUtility.ToJson(this, true));

        public static void Delete(string path = null)
        {
            path ??= DefaultPath;
            if (File.Exists(path)) File.Delete(path);
        }

        // The look in catalog order: each slot's saved colour, or its default when missing or unreadable.
        public Color32[] ColorsFor(IReadOnlyList<string> ids, IReadOnlyList<Color32> defaults)
        {
            var colors = new Color32[ids.Count];
            for (var i = 0; i < ids.Count; i++)
            {
                colors[i] = defaults[i];
                foreach (var entry in _colors)
                    if (entry.Id == ids[i] && ColorUtility.TryParseHtmlString("#" + entry.Hex, out var parsed))
                        colors[i] = parsed;
            }
            return colors;
        }
    }
}
