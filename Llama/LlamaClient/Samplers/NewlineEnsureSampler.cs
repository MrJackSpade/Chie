using Llama.Context.Samplers;
using Llama.Context.Samplers.Interfaces;
using Llama.Data;
using Llama.Native;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Samplers
{
    public class NewlineEnsureSampler : ISimpleSampler
    {
        public void SampleNext(SampleContext sampleContext)
        {
            sampleContext.SetProbability(30004, float.NegativeInfinity);

            LlamaToken? lastToken = sampleContext.InferrenceTokens.LastOrDefault();

            if(lastToken is not null && lastToken.Id == 13)
            {
                sampleContext.SetProbability(13, float.NegativeInfinity);
            }

            int[] banTokens = new int[]
            {
                Utils.LlamaTokenize(sampleContext.ContextHandle, "|", false, Encoding.UTF8).Single(),
                Utils.LlamaTokenize(sampleContext.ContextHandle, " |", false, Encoding.UTF8).Single(),
            };

            foreach(int t in banTokens)
            {
                sampleContext.SetProbability(t, float.NegativeInfinity);
            }
        }
    }
}
