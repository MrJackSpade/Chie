using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.Samplers
{
    public class RepetitionBlockingSampler : ISimpleSampler
    {
        private readonly int _max;

        public RepetitionBlockingSampler(int max)
        {
            this._max = max;
        }

        public Task SampleNext(InferenceEnumerator enumerator)
        {
            int ilen = enumerator.Enumerated.Count;

            if (ilen >= this._max)
            {
                int len = ilen;
                int skip = ilen - _max;
                List<int> ids = enumerator.Enumerated.Skip(skip).Select(s => s.Id).ToList();
                List<int> dist = ids.Distinct().ToList();

                if (dist.Count == 1)
                {
                    int single = dist[0];

                    enumerator.SetBias(single, float.NegativeInfinity, LogitRuleLifetime.Token, LogitBiasType.Additive);
                }
            }

            return Task.CompletedTask;
        }
    }
}