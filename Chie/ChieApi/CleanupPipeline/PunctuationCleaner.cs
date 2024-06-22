using LlamaApi.Shared.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    public class PunctuationCleaner : ITextCleaner
    {
        private static readonly Dictionary<char, char[]> punctuationOrdering = new()
        {
            ['.'] = new char[] { '.' },
            ['!'] = new char[] { '!' },
            ['?'] = new char[] { '?', '!' },
            [','] = Array.Empty<char>()
        };

        public static string IncreaseAndSpacePeriod(string input)
        {
            return Regex.Replace(input, @"([a-zA-Z]{2})\.{2,3} ?([a-zA-Z]|$)", "$1... $2");
        }

        public static string Misc(string input)
        {
            string s = input;

            s = s.Replace(" ?", "?");
            s = s.Replace(" !", "!");
            //s = s.Replace("...", "…");
            s = s.Replace('’', '\''); // Right single quotation mark
            s = s.Replace('‘', '\''); // Left single quotation mark
            s = s.Replace('“', '"');  // Left double quotation mark
            s = s.Replace('”', '"');  // Right double quotation mark
            s = s.Replace('–', '-');  // En dash
            s = s.Replace('—', '-');  // Em dash
            s = s.Replace('«', '"');  // Left-pointing double angle quotation mark
            s = s.Replace('»', '"');  // Right-pointing double angle quotation mark
            s = s.Replace('„', '"');  // Double low-9 quotation mark
            s = s.Replace('‚', '\''); // Single low-9 quotation mark
            s = s.Replace('‹', '<');  // Single left-pointing angle quotation mark
            s = s.Replace('›', '>');  // Single right-pointing angle quotation mark
            s = OrderedPunctuation(s);

            while (s.Contains("\"\""))
            {
                s = s.Replace("\"\"", "\"");
            }

            while (s.Contains("* *"))
            {
                s = s.Replace("* *", "*");
            }

            while (s.Contains("\n\n\n"))
            {
                s = s.Replace("\n\n\n", "\n\n");
            }

            s = RemoveTrailingAsterisk(s);

            s = s.TrimStart('?');
            s = s.TrimStart('!');
            s = s.TrimStart('.');

            return s;
        }

        public static string OrderedPunctuation(string input)
        {
            StringBuilder output = new();

            char lastChar = '\0';

            foreach (char c in input)
            {
                if (lastChar != '\0')
                {
                    if (punctuationOrdering.TryGetValue(lastChar, out char[]? valueFollows) && punctuationOrdering.TryGetValue(c, out _))
                    {
                        if (!valueFollows.Contains(c))
                        {
                            continue;
                        }
                    }
                }

                lastChar = c;
                output.Append(c);
            }

            return output.ToString();
        }

        public static string PadCommas(string input)
        {
            return Regex.Replace(input, @"([a-zA-Z]{2}),([a-zA-Z])", "$1, $2");
        }

        public static string ReduceAndSpace(string input, char c)
        {
            return Regex.Replace(input, $@"([a-zA-Z]{{2}})\{c}\{c} ?([a-zA-Z])", $"$1{c} $2");
        }

        public static string ReduceApostrophe(string input)
        {
            return Regex.Replace(input, @"([a-zA-Z]{2})''([a-zA-Z])", "$1'$2");
        }

        public static string RemoveTrailingAsterisk(string input)
        {
            if (input.Count(c => c == '*') % 2 == 1)
            {
                return input.TrimEnd('*').Trim();
            }

            return input;
        }

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string content in contents)
            {
                string s = content;

                s = PadCommas(s);
                s = IncreaseAndSpacePeriod(s);
                s = ReduceApostrophe(s);
                s = ReduceAndSpace(s, '?');
                s = ReduceAndSpace(s, '!');
                s = ReduceAndSpace(s, ',');
                s = Misc(s);
                s = s.Trim();

                yield return s;
            }
        }
    }
}