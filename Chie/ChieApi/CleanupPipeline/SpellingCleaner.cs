using ChieApi.Shared.Models;
using ChieApi.Shared.Services;
using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class SpellingCleaner : ITextCleaner
    {
        private readonly DictionaryRepository _dictionaryService;

        public SpellingCleaner(DictionaryRepository dictionaryService)
        {
            this._dictionaryService = dictionaryService;
        }

        public string Clean(string content)
        {
            foreach (string word in this._dictionaryService.BreakWords(content))
            {
                if (!this._dictionaryService.IsWord(word))
                {
                    string fingerprint = this._dictionaryService.GetFingerprint(word);

                    List<DictionaryEntry> possibleCorrections = this._dictionaryService.GetByFingerprint(fingerprint).Where(c => c.Word.Length < word.Length).ToList();

                    if (!possibleCorrections.Any())
                    {
                        continue;
                    }

                    if (possibleCorrections.Count > 1)
                    {
                        List<WordDrift> drifts = possibleCorrections.Select(b => this._dictionaryService.GetDrift(word, b.Word)).ToList();

                        WordDrift bestMatch = drifts.OrderBy(d => d.Drift).First();

                        content = this._dictionaryService.Replace(content, word, bestMatch.WordB);
                    }
                    else
                    {
                        DictionaryEntry correction = possibleCorrections.Single();

                        content = this._dictionaryService.Replace(content, word, correction.Word);
                    }
                }
            }

            return content;
        }
    }
}