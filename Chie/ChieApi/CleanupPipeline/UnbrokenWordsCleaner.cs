using ChieApi.Shared.Services;
using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class UnbrokenWordsCleaner : ITextCleaner
    {
        private readonly DictionaryRepository _dictionaryService;

        private readonly int _maxCount;

        private readonly Dictionary<char, string[]> _wordsByLetter = new();

        public UnbrokenWordsCleaner(DictionaryRepository dictionaryService, int maxCount)
        {
            _maxCount = maxCount;

            _dictionaryService = dictionaryService;

            string[] words = dictionaryService.GetWords().Where(w => w.ToUpper() != w).ToArray();

            foreach (IGrouping<char, string> group in words.GroupBy(x => char.ToLower(x[0])))
            {
                this._wordsByLetter.Add(group.Key, group.OrderByDescending(w => w.Length).ToArray());
            }
        }

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string rcontent in contents)
            {
                string content = rcontent;
                string cleanedContent = content;

                foreach (string word in this._dictionaryService.BreakWords(content))
                {
                    if (!string.IsNullOrWhiteSpace(word) && char.IsUpper(word[0]))
                    {
                        continue;
                    }

                    if (!this._dictionaryService.IsWord(word))
                    {
                        List<string> subsequences = this.FindSubsequences(word);

                        if (subsequences.Count > 1)
                        {
                            string replacement = string.Join(" ", subsequences);

                            cleanedContent = cleanedContent.Replace(word, replacement);
                        }
                    }
                }

                yield return cleanedContent;
            }
        }

        public List<string> FindSubsequences(string sequence)
        {
            List<string> result = new();

            if (this.FindSubsequencesHelper(sequence, 0, result))
            {
                return result;
            }

            return new List<string>();
        }

        private bool FindSubsequencesHelper(string sequence, int index, List<string> currentList)
        {
            if (index == sequence.Length)
            {
                return true;
            }

            if (currentList.Count >= _maxCount)
            {
                return false;
            }

            char startChar = char.ToLower(sequence[index]);

            if (_wordsByLetter.TryGetValue(startChar, out string[] words))
            {
                foreach (string subsequence in words)
                {
                    if (sequence[index..].StartsWith(subsequence, StringComparison.OrdinalIgnoreCase))
                    {
                        currentList.Add(subsequence);

                        if (this.FindSubsequencesHelper(sequence, index + subsequence.Length, currentList))
                        {
                            return true;
                        }

                        currentList.RemoveAt(currentList.Count - 1);
                    }
                }
            }

            return false;
        }
    }
}