using UnityEngine;

namespace Isekai.Core
{
    // Placeholder art-bible palette (docs/specs/08-forest-environment-and-lighting.md). New colors come from here.
    public static class Palette
    {
        public static readonly Color Meadow = Hex(0x7CC05A);   // ground grass
        public static readonly Color Foliage = Hex(0x3F9A55);  // leaves, bushes
        public static readonly Color Bark = Hex(0x8A5A3B);     // wood, earth
        public static readonly Color Stone = Hex(0xA8A296);    // rocks, blocks
        public static readonly Color Sky = Hex(0x9ED4F2);      // sky, fog
        public static readonly Color Shadow = Hex(0x4A5A86);   // cool indigo shadow
        public static readonly Color Sunlight = Hex(0xFFE8C6); // warm key light
        public static readonly Color Panel = Hex(0x2A2140);    // UI panels (deep plum)
        public static readonly Color Text = Hex(0xFFF6E5);     // UI text (warm white)
        public static readonly Color Accent = Hex(0xFFC94A);   // glow gold: XP, highlights
        public static readonly Color Danger = Hex(0xE0533D);   // damage, enemies, low health
        public static readonly Color Straw = Hex(0xD9B26F);    // training dummy

        private static Color Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
