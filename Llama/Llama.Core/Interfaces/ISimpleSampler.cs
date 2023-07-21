using Llama.Data.Models;

namespace Llama.Core.Interfaces
{
    public interface ISimpleSampler
    {
        void SampleNext(SampleContext sampleContext);
    }
}