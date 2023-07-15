using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface ISimpleSampler
    {
        void SampleNext(SampleContext sampleContext);
    }
}