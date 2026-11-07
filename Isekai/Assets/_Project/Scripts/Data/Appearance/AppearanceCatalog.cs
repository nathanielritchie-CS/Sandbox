using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Data.Appearance
{
    // Everything a player can customize about their Otherworlder. Phase A: a colour per material slot.
    [CreateAssetMenu(menuName = "Isekai/Appearance/Appearance Catalog")]
    public class AppearanceCatalog : ScriptableObject
    {
        [SerializeField] private ColorSlot[] _colors;

        public IReadOnlyList<ColorSlot> Colors => _colors;

        public int SlotOf(Material material)
        {
            for (var i = 0; i < _colors.Length; i++)
                if (_colors[i].Material == material) return i;
            return -1;
        }

        public Color32[] Defaults()
        {
            var defaults = new Color32[_colors.Length];
            for (var i = 0; i < defaults.Length; i++) defaults[i] = _colors[i].Default;
            return defaults;
        }

        public string[] Ids()
        {
            var ids = new string[_colors.Length];
            for (var i = 0; i < ids.Length; i++) ids[i] = _colors[i].Id;
            return ids;
        }
    }
}
