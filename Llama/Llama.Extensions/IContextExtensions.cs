using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Extensions;
using Llama.Native;

namespace Llama.Extensions
{
    public static class IContextExtensions
    {
        public static void Ensure(this IContext context)
        {
            //if (context.Buffer[0].Id != LlamaToken.Bos)
            //{
            //    throw new Exception("First buffer token is not BOS");
            //}
        }

        public static float[] GetEmbeddings(this IContext handler) => handler.Handle.GetEmbeddings();

        public static Span<float> GetLogits(this IContext handler)
        {
            int n_vocab = handler.VocabCount();

            Span<float> logits = NativeApi.GetLogits(handler.Handle, n_vocab);

            return logits;
        }

        public static LlamaToken GetToken(this IContext handler, int id) => new(id, NativeApi.TokenToPiece(handler.ModelHandle, id));

        public static void SetBuffer(this IContext context, LlamaTokenCollection llamaTokens)
        {
            context.Clear();

            context.Write(llamaTokens);
        }

        public static void SetBuffer(this IContext context, IEnumerable<LlamaToken> tokens)
        {
            LlamaToken[] toSet = tokens.ToArray();

            if (toSet.Length > context.Size)
            {
                throw new ArgumentOutOfRangeException("Generated context state is larger than context size");
            }

            context.Clear();

            context.Write(toSet);

            context.Ensure();
        }

        public static LlamaTokenCollection Tokenize(this IContext context, string value, bool addBos = false)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in NativeApi.LlamaTokenize(context.ModelHandle, value, addBos))
            {
                tokens.Append(context.GetToken(id));
            }

            return tokens;
        }

        public static LlamaTokenCollection Tokenize(this IContext context, IEnumerable<int> value)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in value)
            {
                tokens.Append(context.GetToken(id));
            }

            return tokens;
        }

        public static int VocabCount(this IContext handler) => NativeApi.NVocab(handler.ModelHandle);

        public static void Write(this IContext context, IEnumerable<LlamaToken> tokens)
        {
            LlamaTokenCollection toWrite = new LlamaTokenCollection(tokens).Trim();

            foreach (LlamaToken token in toWrite)
            {
                context.Write(token);
            }
        }
    }
}