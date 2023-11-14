using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApi.Shared.Interfaces;
using LlamaApiClient;
using Loxifi.AsyncExtensions;

namespace ChieApi.TokenTransformers
{
    public class RoleplayEnforcingTransformer : IPostAccept, ITokenTransformer
    {
        private const int END_ASTERISK = 29930;

        private const int START_ASTERISK = 334;

        private readonly float _enforceSlope;

        private readonly float _lengthenOffset;

        private readonly float _lengthenSlope;

        public RoleplayEnforcingTransformer(float enforceSlope, float lengthenSlope, float lengthenOffset)
        {
            _enforceSlope = enforceSlope;
            _lengthenSlope = lengthenSlope;
            _lengthenOffset = lengthenOffset;
        }

        public int GetNextAsterisk(string writtenTrimmed)
        {
            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            return asteriskCount % 2 == 0 ? START_ASTERISK : END_ASTERISK;
        }

        public int GetNotNextAsterisk(string writtenTrimmed)
        {
            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            return asteriskCount % 2 == 1 ? START_ASTERISK : END_ASTERISK;
        }


        public void PostAccept(InferenceEnumerator enumerator)
        {
            string writtenTrimmed = enumerator.Enumerated.ToString()?.Trim() ?? string.Empty;

            int next = GetNextAsterisk(writtenTrimmed);
            int not = GetNotNextAsterisk(writtenTrimmed);

            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            bool endsWith = writtenTrimmed.EndsWith("*");

            //If we have zero asterisks, set the start value and block the end
            if (asteriskCount == 0)
            {
                float mod = 1 + (writtenTrimmed.Length * _enforceSlope);
                enumerator.SetBias(START_ASTERISK, mod, LogitRuleLifetime.Token, LogitBiasType.Multiplicative);
                enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
            //If we have too many asterisks, or we've just placed one, block all asterisks
            else if ((asteriskCount >= 4 && asteriskCount % 2 == 0) || endsWith)
            {
                enumerator.SetBias(START_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
                enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
            //If we're on odd, try and stretch it out for at least a few words
            else if (asteriskCount % 2 == 1)
            {
                int a = writtenTrimmed.LastIndexOf('*');
                string chunk = writtenTrimmed[a..];
                string spaceTrimmed = chunk.Replace("  ", " ");
                int spaceCount = chunk.Count(c => c == ' ');

                float baseF = _lengthenOffset;
                float adj = spaceCount * _lengthenSlope;
                float bias = 0 - baseF + adj;

                if (bias < 0)
                {
                    enumerator.SetBias(END_ASTERISK, bias, LogitRuleLifetime.Token, LogitBiasType.Additive);
                }

                enumerator.SetBias(29892, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
            //Otherwise just make sure we block the "wrong" one
            else
            {
                enumerator.SetBias(not, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
        }

        public async IAsyncEnumerable<LlamaToken> TransformToken(InferenceEnumerator enumerator, IAsyncEnumerable<LlamaToken> selectedTokens)
        {
            string writtenTrimmed = enumerator.Enumerated.ToString()?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(writtenTrimmed) && writtenTrimmed[^1] == '*')
            {
                bool inAsterisks = writtenTrimmed.Count(c => c == '*') % 2 == 1;

                List<LlamaToken> lTokens = await selectedTokens.ToList();

                LlamaToken firstToken = lTokens[0];

                if (!string.IsNullOrWhiteSpace(firstToken.Value) && firstToken.Value[0] != ' ')
                {
                    yield break;
                }
                else
                {
                    foreach (LlamaToken lt in lTokens)
                    {
                        yield return lt;
                    }
                }
            }
            else
            {

                await foreach (LlamaToken token in selectedTokens)
                {
                    yield return token;
                }
            }
        }
    }
}