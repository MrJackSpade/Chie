using Llama.Data.Collections;
using Llama.Data.Enums;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using Llama.Extensions;
using Llama.Native;
using System.Text;

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

        public static LlamaToken GetToken(this IContext handler, int id, LlamaTokenType tokenType) => new(id, NativeApi.TokenToStr(handler.Handle, id), tokenType);

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

                LlamaToken thisToken = context.GetToken(int.Parse(id), Enum.Parse<LlamaTokenType>(tag));

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

            string content = string.Join("\n", context.Evaluated.Select(t => $"{t.Id}:{t.TokenType}:{t.EscapedValue}"));
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

        public static LlamaTokenCollection Tokenize(this IContext context, string value, LlamaTokenType tokenType, bool addBos = false)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in NativeApi.LlamaTokenize(context.Handle, value, addBos, Encoding.UTF8))
            {
                tokens.Append(context.GetToken(id, tokenType));
            }

            return tokens;
        }

        public static LlamaTokenCollection Tokenize(this IContext context, IEnumerable<int> value, LlamaTokenType tokenType)
        {
            LlamaTokenCollection tokens = new();

            foreach (int id in value)
            {
                tokens.Append(context.GetToken(id, tokenType));
            }

            return tokens;
        }

        public static int VocabCount(this IContext handler) => NativeApi.NVocab(handler.Handle);

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