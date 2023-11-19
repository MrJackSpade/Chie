using System.Text;

namespace DiscordGpt.Extensions
{
    public static class StringExtensions
    {
        private static readonly Dictionary<char, string> _escapeSequences = new()
        {
            ['*'] = "**",
            ['\\'] = "\\",
            ['~'] = "\\",
            ['`'] = "\\",
            ['_'] = "\\"
        };

        public static string DiscordEscape(this string str)
        {
            StringBuilder sb = new();

            foreach (char c in str)
            {
                if (_escapeSequences.TryGetValue(c, out string sq))
                {
                    _ = sb.Append(sq);
                }

                _ = sb.Append(c);
            }

            return sb.ToString().Trim();
        }
    }
}