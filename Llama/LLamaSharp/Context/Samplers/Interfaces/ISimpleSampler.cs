namespace Llama.Context.Samplers.Interfaces
{
    public interface ISimpleSampler
    {
        void SampleNext(SampleContext sampleContext);
    }
}