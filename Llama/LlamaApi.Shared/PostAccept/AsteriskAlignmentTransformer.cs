using ChieApi.Interfaces;
using ChieApi.Models;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.TokenTransformers
{
	public class AsteriskAlignmentTransformer : IPostAccept
	{
		private readonly int _cap;

		private readonly LlamaTokenCache _tokenCache;

		private int _comma = 29892;

		private int _endAsterisk = 29930;

		private bool _initialized;

		private int _period;

		private int _startAsterisk = 334;

		public AsteriskAlignmentTransformer(int cap, LlamaTokenCache tokenCache)
		{
			if (cap < 0)
			{
				_cap = int.MaxValue;
			}
			else
			{
				_cap = cap;
			}

			this._tokenCache = tokenCache;
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
			bool endsWith = writtenTrimmed.EndsWith("*");

			//If we have too many asterisks, or we've just placed one, block all asterisks
			if ((asteriskCount >= _cap && asteriskCount % 2 == 0) || endsWith)
			{
				enumerator.SetBias(_startAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
				enumerator.SetBias(_endAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			else if (asteriskCount == 0)
			{
				enumerator.SetBias(_endAsterisk, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			//If we're on odd, try and stretch it out for at least a few words
			else if (asteriskCount % 2 == 1)
			{
				enumerator.SetBias(_comma, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
				enumerator.SetBias(_period, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
			//Otherwise just make sure we block the "wrong" one
			else
			{
				enumerator.SetBias(not, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
			}
		}

		private async Task<int> GetToken(string text)
		{
			IReadOnlyLlamaTokenCollection cacheresult = await this._tokenCache.Get(text);
			return cacheresult.Single().Id;
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