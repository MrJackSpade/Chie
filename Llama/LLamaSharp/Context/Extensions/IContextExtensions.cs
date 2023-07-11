using Llama.Collections;
using Llama.Context.Interfaces;
using Llama.Data;
using Llama.Native;
using Llama.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Llama.Context.Extensions
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

        public static float[] GetEmbeddings(this IContext handler)
        {
            unsafe
            {
                int n_embed = NativeApi.NEmbd(handler.Handle);
                float* embeddings = NativeApi.GetEmbeddings(handler.Handle);
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

        public static Span<float> GetLogits(this IContext handler)
        {
            int n_vocab = handler.VocabCount();

            Span<float> logits = Utils.GetLogits(handler.Handle, n_vocab);

            return logits;
        }

        public static LlamaToken GetToken(this IContext handler, int id, string tag) => new(id, NativeApi.TokenToStr(handler.Handle, id), tag);

        /// <summary>
        /// Loads a context state from disk, does not evaluate
        /// </summary>
        /// <param name="fileName"></param>
        public static void Load(this IContext context, string fileName)
        {
            string content = File.ReadAllText(fileName);

            LlamaTokenCollection newBuffer = new();

            foreach (string tokenStr in content.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(tokenStr))
                {
                    continue;
                }

                string id = tokenStr.Split(":")[0];
                string tag = tokenStr.Split(":")[1];

                LlamaToken thisToken = context.GetToken(int.Parse(id), tag);

                newBuffer.Append(thisToken);
            }

            context.SetBuffer(newBuffer);
        }

        public static void Save(this IContext context, string fileName)
        {
            string dir = new FileInfo(fileName).DirectoryName;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string content = string.Join("\n", context.Evaluated.Select(t => $"{t.Id}:{t.Tag}:{t.EscapedValue}"));
            File.WriteAllText(fileName, content);
        }

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

        public static LlamaTokenCollection Tokenize(this IContext context, string value, string tag, bool addBos = false)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in Utils.LlamaTokenize(context.Handle, value, addBos, context.Encoding))
            {
                tokens.Append(context.GetToken(id, tag));
            }

            return tokens;
        }

        public static LlamaTokenCollection Tokenize(this IContext context, IEnumerable<int> value, string tag)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in value)
            {
                tokens.Append(context.GetToken(id, tag));
            }

            return tokens;
        }

        public static int VocabCount(this IContext handler) => NativeApi.NVocab(handler.Handle);

        public static void Write(this IContext context, IEnumerable<LlamaToken> tokens)
        {
            LlamaTokenCollection toWrite = new LlamaTokenCollection(tokens).Trim();

            foreach (Data.LlamaToken token in toWrite)
            {
                context.Write(token);
            }
        }
    }
}