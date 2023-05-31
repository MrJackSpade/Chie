namespace Llama.Shared
{
	public enum InteractiveMode
	{
		None,
		Interactive,
		InteractiveFirst
	}

	public enum MemoryMode
	{
		Float16,
		Float32
	}

	public enum MiroStatMode
	{
		Disabled = 0,
		MiroStat = 1,
		MiroStat2 = 2
	}

	public class LlamaSettings
	{
		public string[] AdditionalReversePrompts { get; set; } = Array.Empty<string>();

		public IEnumerable<string> AllReversePrompts
		{
			get
			{
				if (this.PrimaryReversePrompt != null)
				{
					yield return this.PrimaryReversePrompt;
				}

				foreach (string additionalReversePrompt in this.AdditionalReversePrompts)
				{
					yield return additionalReversePrompt;
				}
			}
		}

		public int? ContextLength { get; set; }

		public int? GpuLayers { get; set; }

		public string? InPrefix { get; set; }

		public virtual string? InSuffix { get; set; }

		public InteractiveMode InteractiveMode { get; set; }

		public int? KeepPromptTokens { get; set; }

		public Dictionary<int, string> LogitBias { get; set; } = new Dictionary<int, string>();

		public string MainPath { get; set; }

		public MemoryMode MemoryMode { get; set; }

		public MiroStatMode MiroStat { get; set; }

		public float? MiroStatEntropy { get; set; }

		public string ModelPath { get; set; }

		public bool NoMemoryMap { get; set; }

		public bool NoPenalizeNewLine { get; set; }

		public string? PrimaryReversePrompt { get; set; }

		public string? Prompt { get; set; }

		public float? RepeatPenalty { get; set; }

		public int? RepeatPenaltyWindow { get; set; }

		public bool ReturnOnNewLine { get; set; }

		public string? Start { get; set; }

		public float? Temp { get; set; }

		public int? Threads { get; set; }

		public int Timeout { get; set; } = 600_000;

		public int? TokensToPredict { get; set; }

		public float? Top_P { get; set; }

		public bool UseSessionData { get; set; }

		public bool VerbosePrompt { get; set; }
	}
}