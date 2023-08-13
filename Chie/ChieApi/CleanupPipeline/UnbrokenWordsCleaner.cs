using ChieApi.Interfaces;
using ChieApi.Shared.Services;

namespace ChieApi.CleanupPipeline
{
    public class UnbrokenWordsCleaner : IResponseCleaner
    {
        private readonly DictionaryService _dictionaryService;

        private readonly Dictionary<char, string[]> _wordsByLetter = new();

        public UnbrokenWordsCleaner(DictionaryService dictionaryService)
        {
            _dictionaryService = dictionaryService;

            string[] words = dictionaryService.GetWords().Where(w => w.ToUpper() != w).ToArray();

            foreach (IGrouping<char, string> group in words.GroupBy(x => char.ToLower(x[0])))
            {
                this._wordsByLetter.Add(group.Key, group.OrderByDescending(w => w.Length).ToArray());
            }
        }

        public string Clean(string content)
        {
            string cleanedContent = content;

            foreach (string word in this._dictionaryService.BreakWords(content))
            {
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

            return cleanedContent;
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

            char startChar = char.ToLower(sequence[index]);

            foreach (string subsequence in _wordsByLetter[startChar])
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

            return false;
        }
    }
}