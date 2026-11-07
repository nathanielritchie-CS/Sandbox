using System.Text;

namespace Isekai.Gameplay.Players
{
    public static class PlayerName
    {
        public const int MaxLength = 16;
        public const string Default = "Otherworlder";

        // What a typed name becomes: ASCII letters, digits, spaces, '-' and '_' only (the fonts are ASCII), single spaces,
        // trimmed, at most MaxLength characters; nothing left → Default.
        public static string Clean(string typed)
        {
            var sb = new StringBuilder(MaxLength);
            foreach (var c in typed ?? "")
            {
                if (sb.Length == MaxLength) break;
                var allowed = c < 128 && (char.IsLetterOrDigit(c) || c == '-' || c == '_');
                if (allowed) sb.Append(c);
                else if (c == ' ' && sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' ');
            }
            var name = sb.ToString().Trim();
            return name.Length > 0 ? name : Default;
        }
    }
}
