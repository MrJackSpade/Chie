using ChieApi.Interfaces;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class RepetitionBlockingTransformer : ITokenTransformer
    {
        private readonly int _max;

        private readonly StringComparer _stringComparer;

        public RepetitionBlockingTransformer(int max, StringComparer stringComparer)
        {
            this._max = max;
            this._stringComparer = stringComparer;
        }

        public RepetitionBlockingTransformer(int max)
        {
            this._max = max;
            this._stringComparer = StringComparer.OrdinalIgnoreCase;
        }

        public async IAsyncEnumerable<LlamaToken?> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            string written = enumerator.Enumerated.ToString();

            if (string.IsNullOrEmpty(written))
            {
                await foreach (LlamaToken lt in selectedTokens)
                {
                    yield return lt;
                }

                yield break;
            }

            //find the last char
            string endCharWritten = $"{written[^1]}";

            //count how many times it appears at the end
            int endCountWritten = 0;
            for (int i = written.Length - 1; i >= 0; i--)
            {
                if (this._stringComparer.Equals($"{written[i]}", endCharWritten))
                {
                    endCountWritten++;
                }
                else
                {
                    break;
                }
            }

            await foreach (LlamaToken tVal in selectedTokens)
            {
                string? newTokenString = tVal?.ToString();

                if (newTokenString is null)
                {
                    yield return tVal;
                    continue;
                }

                //find the first char
                string firstCharNew = $"{newTokenString[0]}";

                //count how many times it appears at the beginning
                int firstCountNew = 0;
                for (int i = 0; i < newTokenString.Length; i++)
                {
                    if (this._stringComparer.Equals($"{newTokenString[i]}", firstCharNew))
                    {
                        firstCountNew++;
                    }
                    else
                    {
                        break;
                    }
                }

                //if this one token alone exceeds the count, ban it for the entire inference
                if (firstCountNew > this._max)
                {
                    enumerator.SetLogit(tVal.Id, 0, LogitBiasLifeTime.Inferrence);
                    continue;
                }

                //if the end of the existing string plus the beginning
                //of the new string exceed the max, its banned
                if (firstCountNew + endCountWritten > this._max && _stringComparer.Equals(firstCharNew, endCharWritten))
                {
                    enumerator.SetLogit(tVal.Id, 0, LogitBiasLifeTime.Temporary);
                    continue;
                }

                yield return tVal;
            }
        }
    }
}