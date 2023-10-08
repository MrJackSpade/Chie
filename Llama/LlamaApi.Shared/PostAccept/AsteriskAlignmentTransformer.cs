using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
	public class AsteriskAlignmentTransformer : IPostAccept
	{
		private const int END_ASTERISK = 29930;

		private const int START_ASTERISK = 334;

		private readonly int _cap;

		public AsteriskAlignmentTransformer(int cap)
		{
			if (cap < 0)
			{
				_cap = int.MaxValue;
			}
			else
			{
				_cap = cap;
			}
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

			//If we have too many asterisks, or we've just placed one, block all asterisks
			if ((asteriskCount >= _cap && asteriskCount % 2 == 0) || endsWith)
			{
				enumerator.SetBias(START_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
				enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			else if (asteriskCount == 0)
			{
				enumerator.SetBias(END_ASTERISK, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			//If we're on odd, try and stretch it out for at least a few words
			else if (asteriskCount % 2 == 1)
			{
				enumerator.SetBias(29892, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			//Otherwise just make sure we block the "wrong" one
			else
			{
				enumerator.SetBias(not, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
		}
	}
}