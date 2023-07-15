using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface IFinalSampler

    {
        int SampleNext(SampleContext sampleContext);
    }
}