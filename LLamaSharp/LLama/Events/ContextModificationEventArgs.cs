using Llama.Interfaces;
using System;

namespace Llama.Events
{
	public class ContextModificationEventArgs : EventArgs
	{
		public ContextModificationEventArgs(IReadOnlyLlamaTokenCollection evaluated, IReadOnlyLlamaTokenCollection buffer, int matchedCount, int evaluatingIndex, int evaluatingCount)
		{
			this.Evaluated = evaluated;
			this.Buffer = buffer;
			this.EvaluatingIndex = evaluatingIndex;
			this.EvaluatingCount = evaluatingCount;
			this.MatchedCount = matchedCount;
		}

		public IReadOnlyLlamaTokenCollection Buffer { get; private set; }

		public IReadOnlyLlamaTokenCollection Evaluated { get; private set; }

		public int EvaluatingCount { get; private set; }

		public int EvaluatingIndex { get; private set; }

		public int MatchedCount { get; private set; }
	}
}