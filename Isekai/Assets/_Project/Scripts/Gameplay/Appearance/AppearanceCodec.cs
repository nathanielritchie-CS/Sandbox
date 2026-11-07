using System;
using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Gameplay.Appearance
{
    // A look as bytes for the network: [version][colour count][r g b]... Decoding never trusts its input:
    // anything missing or malformed falls back to the defaults and extra data is ignored.
    public static class AppearanceCodec
    {
        public const byte Version = 1;
        public const int MaxBytes = 90; // as Base64 this still fits a FixedString128Bytes

        public static byte[] Encode(IReadOnlyList<Color32> colors)
        {
            var bytes = new byte[2 + colors.Count * 3];
            bytes[0] = Version;
            bytes[1] = (byte)colors.Count;
            for (var i = 0; i < colors.Count; i++)
            {
                bytes[2 + i * 3] = colors[i].r;
                bytes[3 + i * 3] = colors[i].g;
                bytes[4 + i * 3] = colors[i].b;
            }
            return bytes;
        }

        public static Color32[] Decode(byte[] bytes, IReadOnlyList<Color32> defaults)
        {
            var colors = new Color32[defaults.Count];
            for (var i = 0; i < colors.Length; i++) colors[i] = defaults[i];
            if (bytes == null || bytes.Length < 2 || bytes.Length > MaxBytes || bytes[0] != Version) return colors;
            var count = Mathf.Min(bytes[1], Mathf.Min(defaults.Count, (bytes.Length - 2) / 3));
            for (var i = 0; i < count; i++)
                colors[i] = new Color32(bytes[2 + i * 3], bytes[3 + i * 3], bytes[4 + i * 3], 255);
            return colors;
        }

        public static string ToText(byte[] bytes) => Convert.ToBase64String(bytes);

        public static byte[] FromText(string text)
        {
            try
            {
                return Convert.FromBase64String(text ?? "");
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
        }
    }
}
