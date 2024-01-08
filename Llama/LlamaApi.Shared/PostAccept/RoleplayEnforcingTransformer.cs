using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApi.Shared.Interfaces;
using LlamaApiClient;
using Loxifi.AsyncExtensions;

namespace ChieApi.TokenTransformers
{
    public class RoleplayEnforcingTransformer : IPostAccept, ITokenTransformer
    {
        private readonly float _enforceSlope;

        private readonly float _lengthenOffset;

        private readonly float _lengthenSlope;

        private readonly LlamaTokenCache _tokenCache;

        private int _comma = 29892;

        private int _endAsterisk = 29930;

        private bool _initialized;

        private int _period;

        private int _startAsterisk = 334;

        public RoleplayEnforcingTransformer(float enforceSlope, float lengthenSlope, float lengthenOffset, LlamaTokenCache cache)
        {
            _enforceSlope = enforceSlope;
            _lengthenSlope = lengthenSlope;
            _lengthenOffset = lengthenOffset;
            _tokenCache = cache;
        }

        public int GetNextAsterisk(string writtenTrimmed)
        {
            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            return asteriskCount % 2 == 0 ? _startAsterisk : _endAsterisk;
        }

        public int GetNotNextAsterisk(string writtenTrimmed)
        {
            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            return asteriskCount % 2 == 1 ? _startAsterisk : _endAsterisk;
        }

        public async Task PostAccept(InferenceEnumerator enumerator)
        {
            await this.TryInititalize();

            string writtenTrimmed = enumerator.Enumerated.ToString()?.Trim() ?? string.Empty;

            int next = this.GetNextAsterisk(writtenTrimmed);
            int not = this.GetNotNextAsterisk(writtenTrimmed);

            int asteriskCount = writtenTrimmed.Count(c => c == '*');

            bool inAction = asteriskCount % 2 == 1;

            bool endsWith = writtenTrimmed.EndsWith("*");

            //If we have zero asterisks, set the start value and block the end
            if (asteriskCount == 0)
            {
                float mod = 1 + (writtenTrimmed.Length * _enforceSlope);
                enumerator.SetBias(_startAsterisk, mod, LogitRuleLifetime.Token, LogitBiasType.Multiplicative);
                enumerator.SetBias(_endAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
            //If we have too many asterisks, or we've just placed one, block all asterisks
            else if ((asteriskCount >= 4 && !inAction) || endsWith)
            {
                enumerator.SetBias(_startAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
                enumerator.SetBias(_endAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
            }
            //If we're on odd, try and stretch it out for at least a few words
            else if (inAction)
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
                    enumerator.SetBias(_endAsterisk, bias, LogitRuleLifetime.Token, LogitBiasType.Additive);
                }

                enumerator.SetBias(_comma, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
                enumerator.SetBias(_period, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
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

                if (!inAsterisks && !string.IsNullOrWhiteSpace(firstToken.Value) && firstToken.Value[0] != ' ')
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

        private async Task<int> GetToken(string text)
        {
            return (await this._tokenCache.Get(text)).Single().Id;
        }

        private async Task TryInititalize()
        {
            if (!this._initialized)
            {
                this._initialized = true;

                this._startAsterisk = await this.GetToken(" *");
                this._endAsterisk = await this.GetToken("*");
                this._comma = await this.GetToken(",");
                this._period = await this.GetToken(".");
            }
        }
    }
}