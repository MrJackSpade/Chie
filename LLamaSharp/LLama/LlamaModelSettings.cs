using Llama.ContextRollers;
using Llama.Interfaces;
using Llama.PostResponseContextTransformers;
using Llama.TokenTransformers;
using System;
using System.Collections.Generic;
using Llama_token = System.Int32;

namespace Llama
{
	public class LlamaModelSettings
	{
		public List<string> Antiprompt { get; set; } = new();

		public int BatchSize { get; set; } = 512;

		public IContextRoller ContextRoller { get; set; } = new ChatContextRoller();

		public List<IPostResponseContextTransformer> PostResponseConstextTransformers { get; set; } = new List<IPostResponseContextTransformer> 
		{
			new RemoveTemporaryTokens(),
			new StripNullTokens()
		};

		public int ContextSize { get; set; } = 512;

		public float FrequencyPenalty { get; set; } = 0.00f;

		public bool GenerateEmbedding { get; set; } = false;

		public int GpuLayerCount { get; set; } = 0;

		public string InputPrefix { get; set; } = string.Empty;

		public string InputSuffix { get; set; } = string.Empty;

		public bool Instruct { get; set; } = false;

		public bool Interactive { get; set; } = false;

		public bool InteractiveFirst { get; set; } = false;

		public int KeepContextTokenCount { get; set; } = 0;

		public Dictionary<Llama_token, float> LogitBias { get; set; } = new();

		public string LoraAdapter { get; set; } = string.Empty;

		public string LoraBase { get; set; } = string.Empty;

		public bool MemoryFloat16 { get; set; } = true;

		public bool MemTest { get; set; } = false;

		public int Mirostat { get; set; } = 0;

		public float MirostatEta { get; set; } = 0.10f;

		public float MirostatTau { get; set; } = 5.00f;

		public string Model { get; set; } = "models/lamma-7B/ggml-model.bin";

		public bool PenalizeNewlines { get; set; } = true;

		public bool Perplexity { get; set; } = false;

		public int PredictCount { get; set; } = -1;

		public float PresencePenalty { get; set; } = 0.00f;

		public string Prompt { get; set; } = string.Empty;

		public bool PromptCacheAll { get; set; } = false;

		public float RepeatPenalty { get; set; } = 1.10f;

		public int RepeatTokenPenaltyWindow { get; set; } = 64;

		public int Seed { get; set; } = new Random().Next();

		public string SessionPath { get; set; } = string.Empty;

		public float Temp { get; set; } = 0.80f;

		public float TfsZ { get; set; } = 1.00f;

		public int ThreadCount { get; set; } = Math.Max(Environment.ProcessorCount / 2, 1);

		public IList<ITokenTransformer> TokenTransformers { get; } = new List<ITokenTransformer>() {
			new InteractiveEosReplace(),
			new InvalidCharacterBlockingTransformer()
		};

		public int TopK { get; set; } = 40;

		public float TopP { get; set; } = 0.95f;

		public float TypicalP { get; set; } = 1.00f;

		public bool UseColor { get; set; } = false;

		public bool UseMemoryLock { get; set; } = false;

		public bool UseMemoryMap { get; set; } = true;

		public bool UseRandomPrompt { get; set; } = false;

		public bool VerbosePrompt { get; set; } = false;
	}
}