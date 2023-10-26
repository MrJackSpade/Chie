using LlamaApi.Shared.Interfaces;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    public class DuplicateSentenceRemover : ITextCleaner
    {
        public static string GetFingerprint(string word)
        {
            StringBuilder print = new();

            bool started = false;

            foreach (char ch in word)
            {
                if (started)
                {
                    char l = print[^1];

                    if (ch != l)
                    {
                        print.Append(ch);
                    }
                }
                else
                {
                    print.Append(ch);
                }

                started = true;
            }

            return print.ToString();
        }

        public static string SplitAndRemoveDuplicates(string text)
        {
            // Splitting the text on any punctuation mark contained within ".?!"
            string[] sentences = Regex.Split(text, @"(?<=[\.!\?])\s+");

            // Removing any duplicate sentences (case-insensitive)
            HashSet<string> uniqueSentences = new(StringComparer.OrdinalIgnoreCase);
            List<string> result = new();
            foreach (string sentence in sentences)
            {
                string fingerprint = GetFingerprint(sentence);

                if (uniqueSentences.Add(fingerprint))
                {
                    result.Add(sentence.Trim());
                }
            }

            return string.Join(" ", result);
        }

        public string Clean(string content) => SplitAndRemoveDuplicates(content);
    }
}