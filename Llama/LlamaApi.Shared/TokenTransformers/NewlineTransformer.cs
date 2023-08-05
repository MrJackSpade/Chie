using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using Llama.Data.Models;
using LlamaApiClient;
using System.Text.RegularExpressions;

namespace ChieApi.TokenTransformers
{
    public class NewlineTransformer : ITokenTransformer
    {
        private const string LIST_REGEX = "^\\(?\\d*[\\.)]\\ ";

        private const string VALID_ENDS = ":,";

        public NewlineTransformer()
        {
        }

        public bool IsListItem(string lastLine) => Regex.IsMatch(lastLine, LIST_REGEX);

        public bool IsValidEnd(string lastLine) => VALID_ENDS.Contains(lastLine[^1]);

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

            await foreach (LlamaToken tVal in selectedTokens)
            {
                if (tVal.Id == LlamaToken.NewLine.Id)
                {
                    if (isValidEnd || isListItem)
                    {
                        yield return tVal;
                    }
                    else
                    {
                        yield return LlamaToken.EOS;
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