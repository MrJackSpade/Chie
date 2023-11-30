using LlamaApi.Shared.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for method", Justification = "<Pending>")]
    public class PunctuationCleaner : ITextCleaner
    {
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

            s = RemoveTrailingAsterisk(s);

            return s;
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

        public static string OrderedPunctuation(string input)
        {
            StringBuilder output = new();

            char lastChar = '\0';

            foreach (char c in input)
            {
                if(lastChar != '\0')
                {
                    if(punctuationOrdering.TryGetValue(lastChar, out char[]? valueFollows) && punctuationOrdering.TryGetValue(c, out _))
                    {
                        if(!valueFollows.Contains(c))
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

        static Dictionary<char, char[]> punctuationOrdering = new()
        {
            ['.'] = new char[] { '.' },
            ['!'] = new char[] { '!' },
            ['?'] = new char[] { '?', '!' },
        };

        public string Clean(string content)
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

            return s;
        }
    }
}