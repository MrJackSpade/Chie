using Llama.Collections;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Native;
using Llama.Utilities;
using System;
using System.Collections.Generic;

namespace Llama.Context.Extensions
{
    public static class IHasNativeContextHandleExtensions
    {
        public static float[] GetEmbeddings(this IHasNativeContextHandle handler)
        {
            unsafe
            {
                int n_embed = NativeApi.llama_n_embd(handler.SafeHandle);
                float* embeddings = NativeApi.llama_get_embeddings(handler.SafeHandle);
                if (embeddings == null)
                {
                    return Array.Empty<float>();
                }

                Span<float> span = new(embeddings, n_embed);
                float[] res = new float[n_embed];
                span.CopyTo(res.AsSpan());
                return res;
            }
        }

        public static Span<float> GetLogits(this IHasNativeContextHandle handler)
        {
            int n_vocab = handler.VocabCount();

            Span<float> logits = Utils.GetLogits(handler, n_vocab);

            return logits;
        }

        public static LlamaToken GetToken(this IHasNativeContextHandle handler, int id, string tag) => new(id, NativeApi.llama_token_to_str(handler.SafeHandle, id), tag);

        public static bool ReleaseHandle(this IHasNativeContextHandle handler)
        {
            NativeApi.llama_free(handler.Pointer);
            handler.SetHandle(IntPtr.Zero);
            return true;
        }

        public static LlamaTokenCollection Tokenize(this IHasNativeContextHandle context, IEnumerable<int> value, string tag)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in value)
            {
                tokens.Append(context.GetToken(id, tag));
            }

            return tokens;
        }

        public static int VocabCount(this IHasNativeContextHandle handler) => NativeApi.llama_n_vocab(handler.SafeHandle);
    }
}