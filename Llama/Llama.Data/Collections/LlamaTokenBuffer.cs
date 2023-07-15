using Llama.Data.Models;

namespace Llama.Data.Collections
{
    public class LlamaTokenBuffer : LlamaTokenCollection
    {
        public LlamaTokenBuffer(int fixedSize)
        {
            this.FixedSize = fixedSize;
            this.Resize();
        }

        public LlamaTokenBuffer(IEnumerable<LlamaToken> tokens, int fixedSize) : base(tokens)
        {
            this.FixedSize = fixedSize;
            this.Resize();
        }

        public int FixedSize { get; private set; } = -1;

        public override void Append(LlamaToken token)
        {
            base.Append(token);

            if (this.FixedSize != -1 && this._tokens.Count > this.FixedSize)
            {
                this._tokens.RemoveAt(0);
            }
        }

        public override void Clear()
        {
            base.Clear();
            this.Resize();
        }

        public void Resize()
        {
            while (this._tokens.Count < this.FixedSize)
            {
                this._tokens.Add(LlamaToken.Null);
            }
        }
    }
}