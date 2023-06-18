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
            if (context.Buffer[0].Id != NativeApi.llama_token_bos())
            {
                throw new Exception("First buffer token is not BOS");
            }
        }

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

            foreach (int id in Utils.Llama_tokenize(context.SafeHandle, value, addBos, context.Encoding))
            {
                tokens.Append(context.GetToken(id, tag));
            }

            return tokens;
        }

        public static void Write(this IContext context, IEnumerable<LlamaToken> tokens)
        {
            LlamaTokenCollection toWrite = new LlamaTokenCollection(tokens).Trim();

            if (toWrite.Count > context.AvailableBuffer)
            {
                throw new Exception("Write content too large for buffer");
            }

            foreach (Data.LlamaToken token in toWrite)
            {
                context.Write(token);
            }
        }
    }
}