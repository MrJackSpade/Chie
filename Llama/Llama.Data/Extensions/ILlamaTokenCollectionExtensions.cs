using Llama.Data.Collections;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace Llama.Data.Extensions
{
    public static class ILlamaTokenCollectionExtensions
    {
        public static void Append(this ILlamaTokenCollection target, IEnumerable<LlamaToken> tokens)
        {
            foreach (LlamaToken token in tokens)
            {
                target.Append(token);
            }
        }

        public static async Task Append(this ILlamaTokenCollection target, IAsyncEnumerable<LlamaToken> tokens)
        {
            await foreach (LlamaToken token in tokens)
            {
                target.Append(token);
            }
        }

        public static async Task Append(this ILlamaTokenCollection target, Task<IReadOnlyLlamaTokenCollection> tokens)
        {
            foreach (LlamaToken token in await tokens)
            {
                target.Append(token);
            }
        }

        public static bool Contains(this ILlamaTokenCollection target, int tokenId)
        {
            foreach (LlamaToken token in target)
            {
                if (token.Id == tokenId)
                {
                    return true;
                }
            }

            return false;
        }

        public static int FindIndex<T>(this IEnumerable<T> source, int start, Func<T, bool> func)
        {
            int i = 0;
            foreach (T t in source)
            {
                if (i >= start && func.Invoke(t))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public static LlamaTokenCollection From(this ILlamaTokenCollection target, int startIndex, LlamaToken startToken)
        {
            // Calculate the index to start from
            int start = target.Count - startIndex;

            // Ensure the index is within valid bounds
            if (start < 0)
            {
                start = 0;
            }
            else if (start > target.Count)
            {
                start = target.Count;
            }

            // Find the first instance of startToken
            int index = target.FindIndex(start, token => startToken.Id == token.Id);

            // If startToken was not found, use the original start position
            if (index == -1)
            {
                index = start;
            }

            // Copy from the found position (or the original start position if startToken was not found)
            return new LlamaTokenCollection(target.Skip(index));
        }

        public static LlamaTokenCollection Replace(this ILlamaTokenCollection target, LlamaTokenCollection toFind, LlamaTokenCollection toReplace)
        {
            LlamaTokenCollection toReturn = new();

            for (int i = 0; i < target.Count; i++)
            {
                bool isMatch = false;

                if (i + toFind.Count <= target.Count)
                {
                    for (int ii = 0; ii < toFind.Count; ii++)
                    {
                        LlamaToken tokenA = toFind[ii];
                        LlamaToken tokenB = target[ii + i];

                        if (tokenA.Value == tokenB.Value)
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    i += toFind.Count;
                    foreach (LlamaToken tokenA in toReplace)
                    {
                        toReturn.Append(tokenA);
                    }
                }
                else
                {
                    toReturn.Append(target[i]);
                }
            }

            return toReturn;
        }

        public static void Slide(this LlamaTokenCollection target, IEnumerable<LlamaToken> source)
        {
            foreach (LlamaToken item in source)
            {
                target.Shift(item);
            }
        }

        public static IEnumerable<LlamaTokenCollection> Split(this ILlamaTokenCollection target, int id)
        {
            LlamaTokenCollection toReturn = new();

            foreach (LlamaToken token in target)
            {
                if (token.Id == id)
                {
                    yield return toReturn;
                    toReturn = new LlamaTokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }

        public static IEnumerable<LlamaTokenCollection> Split(this ILlamaTokenCollection target, string value, StringComparison stringComparison = StringComparison.Ordinal)
        {
            LlamaTokenCollection toReturn = new();

            foreach (LlamaToken token in target)
            {
                if (string.Equals(token.Value, value, stringComparison))
                {
                    yield return toReturn;
                    toReturn = new LlamaTokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }
    }
}
