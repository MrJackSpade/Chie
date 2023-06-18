namespace Llama.Context.Samplers.Interfaces
{
    public interface IFinalSampler

    {
        int SampleNext(SampleContext sampleContext);
    }
}