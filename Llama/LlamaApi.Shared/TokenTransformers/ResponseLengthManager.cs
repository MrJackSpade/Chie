﻿using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApi.Shared.Interfaces;
using LlamaApiClient;
using Loxifi.AsyncExtensions;

namespace ChieApi.TokenTransformers
{
    public class ResponseLengthManager : ITokenTransformer, ITextCleaner
    {
        private readonly int _base;

        private readonly IDictionaryService _dictionaryService;

        private readonly string _endChars;

        private readonly int _hardmax;

        private readonly int _max;

        private readonly int _min;

        private readonly Random _random = new();

        private readonly SpecialTokens _specialTokens;

        /// <summary>
        ///
        /// </summary>
        /// <param name="hardmax">The point at which text will be cut regardless of punctuation</param>
        /// <param name="max">The point at which text will be cut at the next punctuation</param>
        /// <param name="min">The point at which random rolls will begin occurring to truncate the text, also the target for lengthening</param>
        /// <param name="b">The point used as the base for determining the truncation percentage</param>
        /// <param name="endChars">Characters that are safe to truncate after</param>
        /// <param name="dictionaryService"></param>
        public ResponseLengthManager(int hardmax, int max, int min, int b, string endChars, IDictionaryService dictionaryService, SpecialTokens specialTokens)
        {
            _min = min;
            _max = max;
            _hardmax = hardmax;
            _endChars = endChars;
            _dictionaryService = dictionaryService;
            _base = b;
            _specialTokens = specialTokens;
        }

        public IEnumerable<string> Clean(IEnumerable<string> contents)
        {
            foreach (string content in contents)
            {
                if (content.Length <= _max)
                {
                    yield return content;
                    continue;
                }

                int lastGoodEnd = 0;

                for (int i = 0; i < content.Length; i++)
                {
                    if (this.GoodEndPos(i, content))
                    {
                        lastGoodEnd = i;
                    }
                }

                if (lastGoodEnd <= _min)
                {
                    yield return content;
                    continue;
                }

                yield return content[..(lastGoodEnd + 1)];
            }
        }

        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            string written = enumerator.Enumerated.ToString();

            if (written.Length > _hardmax)
            {
                yield return new LlamaToken(_specialTokens.EOS, null);
                yield break;
            }

            List<LlamaToken> tokens = await selectedTokens.ToList();

            int characterCount = written.Length;

            string? nextT = tokens.FirstOrDefault()?.ToString();

            if (nextT == null)
            {
                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            float chance = 0;

            if (characterCount >= _min)
            {
                chance = ((characterCount - _base) / (float)(_max - _base));
            }

            bool truncate = _random.NextDouble() < chance;

            if (!truncate || !this.GoodEndChar(written) || !nextT.StartsWith(" ") || this.EndsWithWord(written))
            {
                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            yield return new LlamaToken(_specialTokens.EOS, null);
        }

        private bool EndsWithWord(string toTest)
        {
            if (string.IsNullOrWhiteSpace(toTest))
            {
                return false;
            }

            string[] words = toTest.Split(' ').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            string lastWord = words[^1];

            return _dictionaryService.IsWord(lastWord);
        }

        private bool GoodEndChar(string toTest)
        {
            string t = toTest.Trim();

            if (t.Length == 0)
            {
                return false;
            }

            char c = t[^1];

            return this.GoodEndChar(c);
        }

        private bool GoodEndChar(char toTest)
        {
            foreach (char c in _endChars)
            {
                if (toTest == c)
                {
                    return true;
                }
            }

            return false;
        }

        private bool GoodEndPos(int index, string toTest)
        {
            return this.GoodEndChar(toTest[index]) && (toTest.Length == index + 1 || toTest[index + 1] == ' ');
        }
    }
}