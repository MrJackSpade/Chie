using ChieApi.Interfaces;
using Llama.Data.Extensions;
using Llama.Data.Models;
using LlamaApiClient;

namespace ChieApi.Samplers
{
    public class RepetitionBlockingSampler : IBiasAdjustor
    {
        private readonly uint _max;

        public RepetitionBlockingSampler(uint max)
        {
            this._max = max;
        }

        public Task AdjustNext(InferenceEnumerator enumerator)
        {
            uint ilen = enumerator.Enumerated.Count;

            if (ilen >= this._max)
            {
                uint len = ilen;
                uint skip = ilen - _max;
                List<int> ids = enumerator.Enumerated.Skip((int)skip).Select(s => s.Id).ToList();
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