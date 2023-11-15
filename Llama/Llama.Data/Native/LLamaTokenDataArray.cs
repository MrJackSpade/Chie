namespace Llama.Data.Native
{
    public class LlamaTokenDataArray
    {
        public LlamaTokenDataArray(LlamaTokenData[] data, ulong size, bool sorted)
        {
            this.Data = data;
            this.Size = size;
            this.Sorted = sorted;
        }

        public LlamaTokenDataArray(Span<float> logits)
        {
            List<LlamaTokenData> candidates = new(logits.Length);

            for (int token_id = 0; token_id < logits.Length; token_id++)
            {
                candidates.Add(new LlamaTokenData(token_id, logits[token_id], 0.0f));
            }

            this.Data = candidates.ToArray();
            this.Size = (ulong)this.Data.Length;
            this.Sorted = false;
        }

        public Memory<LlamaTokenData> Data { get; set; }

        public ulong Size { get; set; }

        public bool Sorted { get; set; }
    }
}