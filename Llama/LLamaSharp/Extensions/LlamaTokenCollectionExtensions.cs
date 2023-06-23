using Llama.Collections;
using Llama.Data;
using System.Collections.Generic;

namespace Llama.Extensions
{
    public static class LlamaTokenCollectionExtensions
    {
        public static void Append(this LlamaTokenCollection target, IEnumerable<LlamaToken> source)
        {
            foreach (LlamaToken item in source)
            {
                target.Append(item);
            }
        }

        public static void AppendControl(this LlamaTokenCollection target, IEnumerable<int> source)
        {
            foreach (int item in source)
            {
                target.AppendControl(item);
            }
        }

        public static void Slide(this LlamaTokenCollection target, IEnumerable<LlamaToken> source)
        {
            foreach (LlamaToken item in source)
            {
                target.Shift(item);
            }
        }
    }
}