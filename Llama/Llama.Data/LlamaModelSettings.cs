namespace Llama.Data
{
    public class LlamaModelSettings
    {
        public int GpuLayerCount { get; set; } = 0;

        public string Model { get; set; }

        public bool UseMemoryLock { get; set; } = false;

        public bool UseMemoryMap { get; set; } = true;
    }
}