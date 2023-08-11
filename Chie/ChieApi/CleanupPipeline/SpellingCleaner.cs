using ChieApi.Interfaces;
using ChieApi.Shared.Models;
using ChieApi.Shared.Services;

namespace ChieApi.CleanupPipeline
{
    public class SpellingCleaner : IResponseCleaner
    {
        private readonly DictionaryService _dictionaryService;

        public SpellingCleaner(DictionaryService dictionaryService)
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
