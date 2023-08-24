using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
    public class RoleplayEnforcingTransformer : IPostAccept
    {
        private const int END_ASTERISK = 29930;

        private const int START_ASTERISK = 334;

        private readonly float _enforceSlope;

        private readonly float _lengthenOffset;

        private readonly float _lengthenSlope;

        public RoleplayEnforcingTransformer(float enforceSlope, float lengthenSlope, float lengthenOffset)
        {
            this._enforceSlope = enforceSlope;
            this._lengthenSlope = lengthenSlope;
            this._lengthenOffset = lengthenOffset;
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

            int next = this.GetNextAsterisk(writtenTrimmed);
            int not = this.GetNotNextAsterisk(writtenTrimmed);

            int asteriskCount = writtenTrimmed.Count(c => c == '*');
            bool endsWith = writtenTrimmed.EndsWith("*");

            //If we have zero asterisks, set the start value and block the end
            if (asteriskCount == 0)
            {
                float mod = writtenTrimmed.Length * this._enforceSlope;
                enumerator.SetBias(START_ASTERISK, mod, LogitRuleLifetime.Token);
                enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token);
            }
            //If we have too many asterisks, or we've just placed one, block all asterisks
            else if ((asteriskCount >= 4 && asteriskCount % 2 == 0) || endsWith)
            {
                enumerator.SetBias(START_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token);
                enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token);
            }
            //If we're on odd, try and stretch it out for at least a few words
            else if (asteriskCount % 2 == 1)
            {
                int a = writtenTrimmed.LastIndexOf('*');
                string chunk = writtenTrimmed[a..];
                string spaceTrimmed = chunk.Replace("  ", " ");
                int spaceCount = chunk.Count(c => c == ' ');

                float baseF = this._lengthenOffset;
                float adj = spaceCount * _lengthenSlope;
                float bias = 0 - baseF + adj;

                if (bias < 0) 
                {
                    enumerator.SetBias(END_ASTERISK, bias, LogitRuleLifetime.Token);
                }

                enumerator.SetBias(29892, float.NegativeInfinity, LogitRuleLifetime.Token);
            }
            //Otherwise just make sure we block the "wrong" one
            else
            {
                enumerator.SetBias(not, float.NegativeInfinity, LogitRuleLifetime.Token);
            }
        }
    }
}