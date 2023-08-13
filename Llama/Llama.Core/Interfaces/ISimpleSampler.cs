using Llama.Data.Models;

namespace Llama.Core.Interfaces
{
    public interface ISimpleSampler
    {
        public void SampleNext(SampleContext context);
    }
}