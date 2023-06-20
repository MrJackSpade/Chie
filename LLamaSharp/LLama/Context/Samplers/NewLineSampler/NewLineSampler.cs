using Llama.Context.Samplers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Context.Samplers.NewLineSampler
{
    public class NewLineSampler : ISimpleSampler
    {
        private static void Log(string message)
        {
            string fName = "logits.log";
            message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{System.Environment.NewLine}";
            System.IO.File.AppendAllText(fName, message);
        }

        public void SampleNext(SampleContext sampleContext)
        {
            float nl = sampleContext.Logits[13];
            float eos = sampleContext.Logits[2];

            Log("");
            Log(sampleContext.InferrenceTokens.ToString());
            Log($"nl: {nl}; eos: {eos}");
        }
    }
}
