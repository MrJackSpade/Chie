using ChieApi.Shared.Entities;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.Models
{
    public class LlamaSafeString
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public bool IsSafeChar(char c)
        {
            return true;
            //Remove non-ascii
#pragma warning disable CS0162 // Unreachable code detected
            if (Regex.IsMatch($"{c}", @"[^\u0000-\u007F]"))
            {
                return false;
            }

            return true;
#pragma warning restore CS0162 // Unreachable code detected
        }

        public LlamaSafeString(string message)
        {
            List<char> foundInvalid = new();

            StringBuilder newContent = new();

            foreach (char c in message)
            {
                if (!this.IsSafeChar(c))// || "\r\n|".Contains(c))
                {
                    foundInvalid.Add(c);
                    newContent.Append(' ');
                }
                else
                {
                    _ = newContent.Append(c);
                }
            }

            this.Content = newContent.ToString();

            while (this.Content.Contains("  "))
            {
                this.Content = this.Content.Replace("  ", " ");
            }

            this.InvalidCharacters = foundInvalid.ToArray();
        }

        public string Content { get; private set; }

        public char[] InvalidCharacters { get; }

        public bool IsNullOrWhitespace => string.IsNullOrWhiteSpace(this.Content);

        public static LlamaSafeString Parse(ChatEntry arg1) => new(arg1.Content);
    }
}