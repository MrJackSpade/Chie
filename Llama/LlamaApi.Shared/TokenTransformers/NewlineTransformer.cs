using Ai.Utils.Extensions;
using ChieApi.Models;
using Llama.Data.Models;
using LlamaApi.Shared.Interfaces;
using LlamaApiClient;
using System.Text.RegularExpressions;

namespace ChieApi.TokenTransformers
{
    public class NewlineTransformer : ITokenTransformer
    {
        private const string LIST_REGEX = "^\\(?\\d*[\\.)]\\ ";

        private const string VALID_ENDS = ":,";

        private readonly LlamaTokenCache _tokenCache;

        private readonly SpecialTokens _specialTokens;

        public NewlineTransformer(SpecialTokens specialTokens, LlamaTokenCache tokenCache)
        {
            _specialTokens = specialTokens;
            _tokenCache = tokenCache;
        }

        public bool IsListItem(string lastLine)
        {
            return Regex.IsMatch(lastLine, LIST_REGEX);
        }

        public bool IsValidEnd(string lastLine)
        {
            return VALID_ENDS.Contains(lastLine[^1]);
        }

        private async IAsyncEnumerable<LlamaToken?> Split(IAsyncEnumerable<LlamaToken> source)
        {
            await foreach (LlamaToken token in source)
            {
                if (token.Value?.Contains('\n') ?? false)
                {
                    string[] parts = token.Value.Split('\n');

                    foreach (string p in parts)
                    {
                        if (!string.IsNullOrEmpty(p))
                        {
                            foreach (LlamaToken t in await _tokenCache.Get(p))
                            {
                                yield return t;
                            }
                        }

                        foreach (LlamaToken t in await _tokenCache.Get("\n"))
                        {
                            yield return t;
                        }
                    }
                }
                else
                {
                    yield return token;
                }
            }
        }

        public async IAsyncEnumerable<LlamaToken?> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {

            string writtenTrimmed = enumerator.Enumerated.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(writtenTrimmed))
            {
                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }

                yield break;
            }

            string[] lines = writtenTrimmed.CleanSplit().ToArray();

            string lastLine = lines.Last();

            bool isValidEnd = this.IsValidEnd(lastLine);
            bool isListItem = this.IsListItem(lastLine);

            await foreach (LlamaToken tVal in this.Split(selectedTokens))
            {
                if (tVal.Id == _specialTokens.NewLine)
                {
                    if (isValidEnd || isListItem)
                    {
                        yield return tVal;
                    }
                    else
                    {
                        yield return new LlamaToken(_specialTokens.EOS, null);
                    }
                }
                else
                {
                    yield return tVal;
                }
            }
        }
    }
}