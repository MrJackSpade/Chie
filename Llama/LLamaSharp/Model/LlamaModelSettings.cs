using System;

namespace Llama.Model
{
    public class LlamaModelSettings
    {
        public bool GenerateEmbedding { get; set; }

        public int GpuLayerCount { get; set; } = 0;

        public string LoraAdapter { get; set; } = string.Empty;

        public string LoraBase { get; set; } = string.Empty;

        public bool MemoryFloat16 { get; set; } = true;

        public bool MemTest { get; set; } = false;

        public string Model { get; set; } = "models/lamma-7B/ggml-model.bin";

        public bool Perplexity { get; set; }

        public int Seed { get; set; } = new Random().Next();

        public int ThreadCount { get; set; } = Math.Max(Environment.ProcessorCount / 2, 1);

        public bool UseMemoryLock { get; set; } = false;

        public bool UseMemoryMap { get; set; } = true;
    }
}