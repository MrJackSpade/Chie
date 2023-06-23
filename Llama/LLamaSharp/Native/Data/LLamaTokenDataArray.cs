using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Llama.Native.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaTokenDataArray
    {
        public Memory<LLamaTokenData> data;

        public ulong size;

        [MarshalAs(UnmanagedType.I1)]
        public bool sorted;

        public LlamaTokenDataArray(LLamaTokenData[] data, ulong size, bool sorted)
        {
            this.data = data;
            this.size = size;
            this.sorted = sorted;
        }

        public LlamaTokenDataArray(Span<float> logits)
        {
            List<LLamaTokenData> candidates = new(logits.Length);

            for (int token_id = 0; token_id < logits.Length; token_id++)
            {
                candidates.Add(new LLamaTokenData(token_id, logits[token_id], 0.0f));
            }

            this.data = candidates.ToArray();
            this.size = (ulong)this.data.Length;
            this.sorted = false;
        }
    }
}