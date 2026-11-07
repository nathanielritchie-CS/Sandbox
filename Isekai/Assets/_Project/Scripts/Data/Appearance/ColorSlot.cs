using System;
using UnityEngine;

namespace Isekai.Data.Appearance
{
    // One colourable part of the character: the material it tints, its default and the swatches offered.
    [Serializable]
    public class ColorSlot
    {
        [SerializeField] private string _id;
        [SerializeField] private string _label;
        [SerializeField] private Material _material;
        [SerializeField] private Color _default = Color.white;
        [SerializeField] private Color[] _palette;

        public string Id => _id;
        public string Label => _label;
        public Material Material => _material;
        public Color Default => _default;
        public Color[] Palette => _palette;
    }
}
