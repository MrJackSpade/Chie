using Llama.Data.Collections;
using Llama.Data.Interfaces;

namespace Llama.Data.Events
{
    public class ContextModificationEventArgs : EventArgs
    {
        public ContextModificationEventArgs(IReadOnlyLlamaTokenCollection evaluated, IReadOnlyLlamaTokenCollection buffer, int matchedCount, int evaluatingIndex, int evaluatingCount)
        {
            this.Evaluated = new LlamaTokenBuffer(evaluated, evaluated.Count);
            this.Buffer = new LlamaTokenBuffer(buffer, buffer.Count);
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