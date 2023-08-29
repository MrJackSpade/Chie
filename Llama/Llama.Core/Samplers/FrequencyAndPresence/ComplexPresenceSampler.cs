using Llama.Core.Extensions;
using Llama.Core.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Models;
using Llama.Data.Models.Settings;
using Llama.Data.Native;

namespace Llama.Core.Samplers.FrequencyAndPresence
{
	public class ComplexPresenceSampler : ISimpleSampler
	{
		private readonly ComplexPresencePenaltySettings _settings;

		public ComplexPresenceSampler(ComplexPresencePenaltySettings settings)
		{
			this._settings = settings;
		}

		public void SampleNext(SampleContext sampleContext)
		{
			LlamaTokenCollection sampleTokens = sampleContext.ContextTokens.Trim();

			int[] lastTokens = this.GetLastTokens(sampleTokens, this._settings.RepeatTokenPenaltyWindow).Ids;

			LlamaTokenDataArray candidates = sampleContext.Candidates;

			int minGroupLength = this._settings.MinGroupLength;
			float scalePerGroup = this._settings.GroupScale;
			float scalePerLength = this._settings.LengthScale;

			if (minGroupLength == 0)
			{
				return;
			}

			int[] test_array = new int[lastTokens.Length + 1];

			for (int i = 0; i < lastTokens.Length; i++)
			{
				test_array[i] = lastTokens[i];
			}

			HashSet<int> candidate_ids = new(test_array);

			int num_threads = Environment.ProcessorCount;

			Range[] ranges = this.GetRanges(num_threads, candidates.data.Length).ToArray();

			Parallel.ForEach(ranges, range => this.ProcessCandidates(candidates, test_array, minGroupLength, candidate_ids, range.Start.Value, range.End.Value, scalePerGroup, scalePerLength));

			candidates.sorted = false;
		}

		private IEnumerable<Range> GetRanges(int chunks, int total)
		{
			int l = total / chunks;
			int r = total % chunks;

			for(int i = 0; i < chunks; i++)
			{
				int s = i * l;
				int e = i * l + l;

				if(i < chunks - 1)
				{
					yield return new Range(s, e);
				} else
				{
					yield return new Range(s, e + r);
				}
			}
		}

		private void ProcessCandidates(LlamaTokenDataArray candidates, int[] test_array, int minGroupLength, HashSet<int> candidate_ids, int start, int end, float scalePerGroup, float scalePerLength)
		{
			Span<LlamaTokenData> candidateSpan = candidates.data.Span;

			int p_test = test_array.Length - 1;

			for (int i = start; i < end; i++)
			{
				int llama_token = candidateSpan[i].id;

				if (!candidate_ids.Contains(llama_token))
				{
					continue;
				}

				test_array[p_test] = llama_token;

				int n_group = 0;
				int n_len = 0;

				for (int p_dynamic = test_array.Length - 2; p_dynamic >= 0; p_dynamic--)
				{
					int p2 = 0;

					while (p_dynamic - p2 >= 0 && test_array[p_dynamic - p2] == test_array[p_test - p2])
					{
						p2++;
					}

					if (p2 >= minGroupLength)
					{
						n_group++;
						n_len += p2;
					}
				}

				if (n_group != 0)
				{
					float g_penalty = (float)Math.Pow(scalePerGroup, n_group);
					float l_penalty = (float)Math.Pow(scalePerLength, n_len);

					if (candidateSpan[i].logit <= 0)
					{
						candidateSpan[i].logit *= g_penalty;
						candidateSpan[i].logit *= l_penalty;
					}
					else
					{
						candidateSpan[i].logit /= g_penalty;
						candidateSpan[i].logit /= l_penalty;
					}
				}
			}
		}
	}
}