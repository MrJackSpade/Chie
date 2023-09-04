using ChieApi.Shared.Models;
using Llama.Data.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.Shared.Services
{   
    public class DictionaryRepository : IDictionaryService
    {
        private const string WORD_PATTERN = @"\b\w+\b";

        private readonly DictionaryCache _dictionaryCache;

        private readonly StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;

        public DictionaryRepository(DictionaryCache dictionaryCache)
        {
            this._dictionaryCache = dictionaryCache;
        }

        public IEnumerable<string> BreakWords(string sentence)
        {
            Regex rx = new(WORD_PATTERN);

            foreach (Match match in rx.Matches(sentence))
            {
                yield return match.Value;
            }
        }

        public int CompareLetterRepetitions(string word1, string word2)
        {
            int difference = 0;
            int index1 = 0;
            int index2 = 0;

            while (index1 < word1.Length && index2 < word2.Length)
            {
                string char1Str = $"{word1[index1]}";
                string char2Str = $"{word2[index2]}";

                if (!this._stringComparer.Equals(char1Str, char2Str))
                {
                    throw new ArgumentException("The words don't contain the same letters in the same order.");
                }

                int count1 = 0;
                while (index1 < word1.Length && this._stringComparer.Equals($"{word1[index1]}", char1Str))
                {
                    count1++;
                    index1++;
                }

                int count2 = 0;
                while (index2 < word2.Length && this._stringComparer.Equals($"{word2[index2]}", char2Str))
                {
                    count2++;
                    index2++;
                }

                difference += Math.Abs(count1 - count2);
            }

            return difference;
        }

        public IEnumerable<DictionaryEntry> GetByFingerprint(string fingerprint)
        {
            do
            {
                try
                {
                    return this._dictionaryCache.GetWordsByFingerprint(fingerprint);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(1000);
                }
            } while (true);
        }

        public WordDrift GetDrift(string wordA, string wordB)
        {
            int drift = this.CompareLetterRepetitions(wordA, wordB);

            return new WordDrift(wordA, wordB, drift);
        }

        public string GetFingerprint(string word)
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

        public string[] GetWords() => this._dictionaryCache.GetWords();

        public bool IsWord(string word)
        {
            do
            {
                try
                {
                    return this._dictionaryCache.IsWord(word);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    Thread.Sleep(1000);
                }
            } while (true);
        }

        public string Replace(string sentence, string word, string replacement, bool caseSensitive = true) => Regex.Replace(sentence, $@"(?<=\W|^){Regex.Escape(word)}(?=\W|$)", replacement, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
    }
}