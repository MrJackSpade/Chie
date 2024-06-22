using ChieApi.Shared.Services;
using LlamaApi.Shared.Interfaces;

namespace ChieApi.CleanupPipeline
{
    public class BrokenWordsCleaner : ITextCleaner
    {
        private const string WORD_PUNCTUATION = ".'-";

        private readonly DictionaryRepository _dictionaryService;

        private readonly int _distance;

        public BrokenWordsCleaner(DictionaryRepository dictionaryService, int distance)
        {
            _distance = distance;

            _dictionaryService = dictionaryService;
        }

        public IEnumerable<string> Clean(IEnumerable<string> sentence)
        {
            return this.MergeWords(sentence);
        }

        /// <summary>
        /// Finds the first letter or digit. Some non-alpha chars are optional for determining a word
        /// however digits are not
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public int FindFirstLetterOrDigit(string word)
        {
            for (int i = 0; i < word.Length; i++)
            {
                char test = word[i];

                if (char.IsLetter(test) || char.IsDigit(test))
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindLastLetterOrDigit(string word)
        {
            for (int i = word.Length - 1; i >= 0; i--)
            {
                char test = word[i];

                if (char.IsLetter(test) || char.IsDigit(test))
                {
                    return i;
                }
            }

            return -1;
        }

        public IEnumerable<FoundWord> FindWords(string sentence)
        {
            int startP = 0;

            while (startP >= 0 && startP < sentence.Length)
            {
                int endP = this.GetEndWord(sentence, startP);

                if (endP == -1)
                {
                    continue;
                }

                yield return new FoundWord()
                {
                    Index = startP,
                    Length = endP - startP
                };

                startP = this.GetNextValidWordCharacter(sentence, endP + 1);
            }
        }

        public int GetEndWord(string sentence, int start)
        {
            int end = start + 1;

            while (end < sentence.Length && this.IsValidWordCharacter(sentence[end]))
            {
                end++;
            }

            if (end <= start)
            {
                return -1;
            }

            return end;
        }

        public List<List<FoundWord>> GetMergeCandidates(List<FoundWord> collection, int currentIndex, int distance)
        {
            List<List<FoundWord>> toReturn = new();

            for (int before = 0; before < distance + 1; before++)
            {
                for (int after = 0; after < distance + 1; after++)
                {
                    if (before + after == 0)
                    {
                        continue;
                    }

                    if (currentIndex - before < 0)
                    {
                        continue;
                    }

                    if (currentIndex + after >= collection.Count)
                    {
                        continue;
                    }

                    List<FoundWord> thisSet = new();

                    for (int s = currentIndex - before; s <= currentIndex + after; s++)
                    {
                        thisSet.Add(collection[s]);
                    }

                    if (thisSet.Count > 0)
                    {
                        toReturn.Add(thisSet);
                    }
                }
            }

            return toReturn.OrderBy(c => c.Count).ToList();
        }

        public int GetNextValidWordCharacter(string sentence, int start)
        {
            while (start < sentence.Length)
            {
                if (this.IsValidWordCharacter(sentence[start]))
                {
                    return start;
                }

                start++;
            }

            return -1;
        }

        public string GetRange(string sentence, List<FoundWord> foundWords)
        {
            int start = foundWords.Min(f => f.Index);

            int end = foundWords.Max(f => f.Index + f.Length);

            int length = end - start;

            return sentence.Substring(start, length);
        }

        public IEnumerable<string> GetWordVariations(string word)
        {
            yield return word;

            if (word.Length > 1)
            {
                int firstLetter = this.FindFirstLetterOrDigit(word);
                int lastLetter = this.FindLastLetterOrDigit(word);

                bool canTrimStart = firstLetter > 0;
                bool canTrimEnd = lastLetter < word.Length - 1 && lastLetter >= 0;

                if (canTrimStart)
                {
                    yield return word[firstLetter..];
                }

                if (canTrimEnd)
                {
                    yield return word[0..(lastLetter + 1)];
                }

                if (canTrimStart && canTrimEnd)
                {
                    int l = lastLetter - firstLetter;

                    yield return word[..l];
                }
            }
        }

        public bool IsProbablyValidWord(string sentence, FoundWord foundWord)
        {
            string test = sentence.Substring(foundWord.Index, foundWord.Length);

            return this.IsProbablyValidWord(test);
        }

        public bool IsProbablyValidWord(string test)
        {
            if (test == "-")
            {
                return true;
            }

            foreach (string variation in this.GetWordVariations(test))
            {
                if (this._dictionaryService.IsWord(variation))
                {
                    return true;
                }
            }

            if (float.TryParse(test, out _))
            {
                return true;
            }

            return false;
        }

        public bool IsValidWordCharacter(char c)
        {
            if (char.IsLetter(c))
            {
                return true;
            }

            if (char.IsDigit(c))
            {
                return true;
            }

            if (WORD_PUNCTUATION.Contains(c))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<string> MergeWords(IEnumerable<string> sentences)
        {
            foreach (string sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                {
                    yield return sentence;
                    continue;
                }

                List<FoundWord> foundWords = this.FindWords(sentence).ToList();
                List<FoundWord> healedWords = new();
                //TODO: RemoveMe
                List<string> words = this.GetWords(sentence, foundWords).ToList();

                Dictionary<string, string> replacements = new();

                for (int f = 0; f < foundWords.Count; f++)
                {
                    FoundWord foundWord = foundWords[f];

                    //Skip if its probably already a word
                    if (this.IsProbablyValidWord(sentence, foundWord))
                    {
                        continue;
                    }

                    //skip if we've identified it as part of another word
                    if (healedWords.Contains(foundWord))
                    {
                        continue;
                    }

                    List<List<FoundWord>> mergeCandidates = this.GetMergeCandidates(foundWords, f, this._distance);

                    mergeCandidates = mergeCandidates.Where(m => !m.Any(w => healedWords.Contains(w))).ToList();

                    List<FoundWord> bestMatch = this.FindFunctionalGroupings(sentence, mergeCandidates);

                    if (bestMatch != null)
                    {
                        List<string> matchedWords = this.GetWords(sentence, bestMatch).ToList();

                        string wordGroup = this.GetRange(sentence, bestMatch);

                        string replacement = string.Join("", matchedWords);

                        if (!replacements.ContainsKey(wordGroup))
                        {
                            replacements.Add(wordGroup, replacement);
                        }

                        healedWords.AddRange(bestMatch);
                    }

                    //TODO: RemoveMe
                    List<List<string>> wordCandidates = this.GetWords(sentence, mergeCandidates).ToList();
                }

                string toReturn = sentence;

                //Replace longest to shortest so that subsequences don't break sequence replacement
                foreach (KeyValuePair<string, string> pair in replacements.OrderByDescending(k => k.Value.Length))
                {
                    toReturn = toReturn.Replace(pair.Key, pair.Value);
                }

                yield return toReturn;
            }
        }

        private List<FoundWord>? FindFunctionalGroupings(string sentence, List<List<FoundWord>> mergeCandidates)
        {
            foreach (List<FoundWord> candidate in mergeCandidates.OrderByDescending(l => l.Count))
            {
                string joinedWord = string.Join("", this.GetWords(sentence, candidate));

                if (this.IsProbablyValidWord(joinedWord))
                {
                    return candidate;
                }
            }

            return null;
        }

        private IEnumerable<string> GetWords(string sentence, IEnumerable<FoundWord> foundWords)
        {
            foreach (FoundWord foundWord in foundWords)
            {
                yield return sentence.Substring(foundWord.Index, foundWord.Length);
            }
        }

        private IEnumerable<List<string>> GetWords(string sentence, IEnumerable<List<FoundWord>> foundWords)
        {
            foreach (List<FoundWord> words in foundWords)
            {
                yield return this.GetWords(sentence, words).ToList();
            }
        }

        public class FoundWord
        {
            public int Index { get; set; }

            public int Length { get; set; }
        }
    }
}