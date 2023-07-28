using ChieApi.Interfaces;
using ChieApi.Models;
using Loxifi.Extensions;
using System.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace ChieApi.Services
{
    public class DictionaryService
    {
        private readonly StringComparer _stringComparer = StringComparer.OrdinalIgnoreCase;
        private string ConnectionString { get; set; }
        public DictionaryService(IHasConnectionString hasConnectionString)
        {
            this.ConnectionString = hasConnectionString.ConnectionString;
        }

        const string WORD_PATTERN = @"\b\w+\b";

        public IEnumerable<string> BreakWords(string sentence)
        {
            Regex rx = new(WORD_PATTERN);

            foreach (Match match in rx.Matches(sentence))
            {
                yield return match.Value;
            }
        }

        public string Replace(string sentence, string word, string replacement, bool caseSensitive = true) => Regex.Replace(sentence, $@"(?<=\W|^){Regex.Escape(word)}(?=\W|$)", replacement, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

        public bool IsWord(string word)
        {
            SqlConnection sqlConnection = new(this.ConnectionString);

            string pWord = word.Replace("'", "''");

            return sqlConnection.Query<DictionaryEntry>($"select * from Dictionary where word = '{pWord}'").Any();
        }

        public IEnumerable<DictionaryEntry> GetByFingerprint(string fingerprint)
        {
            SqlConnection sqlConnection = new(this.ConnectionString);

            string pWord = fingerprint.Replace("'", "''");

            return sqlConnection.Query<DictionaryEntry>($"select * from Dictionary where fingerprint = '{pWord}'").ToList();
        }

        public WordDrift GetDrift(string wordA, string wordB)
        {
            int drift = this.CompareLetterRepetitions(wordA, wordB);

            return new WordDrift(wordA, wordB, drift);
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

                if (!_stringComparer.Equals(char1Str, char2Str))
                {
                    throw new ArgumentException("The words don't contain the same letters in the same order.");
                }

                int count1 = 0;
                while (index1 < word1.Length && _stringComparer.Equals($"{word1[index1]}", char1Str))
                {
                    count1++;
                    index1++;
                }

                int count2 = 0;
                while (index2 < word2.Length && _stringComparer.Equals($"{word2[index2]}", char2Str))
                {
                    count2++;
                    index2++;
                }

                difference += Math.Abs(count1 - count2);
            }

            return difference;
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
    }
}
