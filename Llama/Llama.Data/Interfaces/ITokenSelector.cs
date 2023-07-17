using Llama.Data.Models;

namespace Llama.Data.Interfaces
{
    public interface ITokenSelector

    {
        int SampleNext(SampleContext sampleContext);
    }
}