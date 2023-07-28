using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Extensions
{
    public static class IReadOnlyLlamaTokenCollectionExtensions
    {
        public static IReadOnlyLlamaTokenCollection TrimWhiteSpace(this IReadOnlyLlamaTokenCollection tokens)
        {
            LlamaTokenCollection llamaTokens = new();

            List<LlamaToken> tokenBuffer = new();

            bool isStarted = false;

            foreach (LlamaToken token in tokens)
            {
                bool isWhite = string.IsNullOrWhiteSpace(token.Value);

                if (!isWhite)
                {
                    isStarted = true;
                }

                if (isStarted)
                {
                    tokenBuffer.Add(token);
                }

                if (!isWhite)
                {
                    foreach (LlamaToken lToken in tokenBuffer)
                    {
                        llamaTokens.Append(lToken);
                    }

                    tokenBuffer.Clear();
                }
            }

            return llamaTokens;
        }
    }
}