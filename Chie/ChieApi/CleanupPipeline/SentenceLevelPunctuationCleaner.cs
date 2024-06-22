using ChieApi.Extensions;
using LlamaApi.Shared.Interfaces;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    public class SentenceLevelPunctuationCleaner : ITextCleaner
    {
        /// <summary>
        /// List of words that identify questions
        /// </summary>
        public static readonly string[] questionIdentifiers = new string[]
        {
            "Is",
            "Are",
            "Can",
            "May",
            "Will",
            "Would",
            "Does",
            "Did",
            "Has",
            "Should",
            "Could",
            "Might",
            "Am",
            "Were",
            "Was",
            "What",
            "Where",
            "When",
            "Why",
            "How",
            "Which",
            "Whose",
            "Who",
            "Isn't",
            "Aren't",
            "Can't",
            "Mayn't",
            "Won't",
            "Wouldn't",
            "Doesn't",
            "Didn't",
            "Haven't",
            "Hasn't",
            "Shouldn't",
            "Couldn't",
            "Mightn't",
            "Ain't",
            "Weren't",
            "Wasn't"
        };

        public SentenceLevelPunctuationCleaner()
        {
        }

        public static IEnumerable<string> CorrectPunctuation(IEnumerable<string> texts)
        {
            foreach (string text in texts)
            {
                // Splitting the text based on common punctuation
                string[] sentences = Regex.Split(text, @"(?<=[.!?…])\s+");

                // Iterate through the sentences and correct the punctuation
                for (int i = 0; i < sentences.Length; i++)
                {
                    string sentence = sentences[i].Trim();

                    string checkSentence = sentence;

                    if (sentence.ContainsAny('*', ','))
                    {
                        int li = sentence.LastIndexOfAny('*', ',');

                        if (li != sentence.Length - 1)
                        {
                            checkSentence = checkSentence[(li + 1)..].Trim();
                        }
                    }

                    string firstWord = checkSentence.Split(' ')[0];

                    // Check if the sentence is a question based on the first word
                    if (Array.Exists(questionIdentifiers, word => word.Replace("'", "").Equals(firstWord.Replace("'", ""), StringComparison.OrdinalIgnoreCase)))
                    {
                        // Replace the last character with a question mark if it's not already one
                        if (sentence[^1] != '?')
                        {
                            if (EndsWithPunctuation(sentences[i]))
                            {
                                sentences[i] = sentence[..^1] + "?";
                            }
                            else
                            {
                                sentences[i] = sentence + "?";
                            }
                        }
                    }
                }

                // Join the corrected sentences back together
                yield return string.Join(" ", sentences);
            }
        }

        public static bool EndsWithPunctuation(string sentence)
        {
            string punctuation = ",.!?";

            if (string.IsNullOrEmpty(sentence))
            {
                return false;
            }

            char c = sentence[^1];

            return punctuation.Contains(c);
        }

        public IEnumerable<string> Clean(IEnumerable<string> content)
        {
            return CorrectPunctuation(content);
        }
    }
}