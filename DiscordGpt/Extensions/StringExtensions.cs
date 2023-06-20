using System.Text;

namespace DiscordGpt.Extensions
{
    public static class StringExtensions
    {
        private const string ESCAPE_CHARS = @"*\~";

        public static string DiscordEscape(this string str)
        {
            StringBuilder sb = new();

            foreach (char c in str)
            {
                if (ESCAPE_CHARS.Contains(c))
                {
                    _ = sb.Append('\\');
                }

                _ = sb.Append(c);
            }

            return sb.ToString();
        }
    }
}