﻿using Llama.Data.Enums;

namespace Llama.Data
{
    public class LlamaModelSettings
    {
        public int BatchSize { get; set; }

        public int ContextSize { get; set; }

        public bool GenerateEmbedding { get; set; }

        public int GpuLayerCount { get; set; } = 0;

        public string LoraAdapter { get; set; } = string.Empty;

        public string LoraBase { get; set; } = string.Empty;

        public MemoryMode MemoryMode { get; set; } = MemoryMode.Float16;

        public bool MemTest { get; set; } = false;

        public string Model { get; set; }

        public bool Perplexity { get; set; }

        public int Seed { get; set; } = new Random().Next();

        public int ThreadCount { get; set; } = Math.Max(Environment.ProcessorCount / 2, 1);

        public bool UseMemoryLock { get; set; } = false;

        public bool UseMemoryMap { get; set; } = true;
    }
}