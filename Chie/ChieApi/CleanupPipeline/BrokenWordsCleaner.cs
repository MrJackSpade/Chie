using ChieApi.Interfaces;
using ChieApi.Shared.Services;
using System.Text.RegularExpressions;

namespace ChieApi.CleanupPipeline
{
    public class BrokenWordsCleaner : IResponseCleaner
    {
        private readonly DictionaryService _dictionaryService;

        private readonly Dictionary<char, string[]> _wordsByLetter = new();

        private readonly int _distance;
        public BrokenWordsCleaner(DictionaryService dictionaryService, int distance)
        {
            _distance = distance;

            _dictionaryService = dictionaryService;
        }

        public string Clean(string content)
        {
            string[] words = Regex.Split(content, @"(\W+)");

            List<string> correctedWords = new();

            int index = 0;

            while (index < words.Length)
            {
                if (!Regex.IsMatch(words[index], @"^\w+$") || _dictionaryService.IsWord(words[index]))
                {
                    correctedWords.Add(words[index]);
                    index++;
                    continue;
                }

                string candidateWord = words[index];
                int joinCount = 0;
                int forwardIndex = index;

                while (joinCount < _distance && forwardIndex < words.Length - 2 && Regex.IsMatch(words[forwardIndex + 1], @"^\w+$"))
                {
                    candidateWord += words[forwardIndex + 1] + words[forwardIndex + 2];
                    if (_dictionaryService.IsWord(candidateWord))
                    {
                        correctedWords.Add(candidateWord);
                        index = forwardIndex + 3;
                        break;
                    }

                    forwardIndex += 2;
                    joinCount++;
                }

                if (index != forwardIndex + 3)
                {
                    correctedWords.Add(words[index]);
                    index++;
                }
            }

            string result = string.Join("", correctedWords);

            return result;
        }
    }
}