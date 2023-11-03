using Llama.Core.Tests.TestObjects;
using Llama.Core.Utils;
using Llama.Data.Collections;

namespace Llama.Core.Tests.Extensions
{
    internal static class PointerArrayExtensions
    {
        public static bool Matches<T>(this KvCacheState<T> source, PointerArray<T> other)
        {
            for (uint i = 0; i < other.Pointer; i++)
            {
                if (!Equals(source[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Matches<T>(this KvCacheState<T> source, ArrayShifter<T> other)
        {
            IReadOnlyList<T> testData = other.BackingData;

            for (uint i = 0; i < source.Length; i++)
            {
                if (!Equals(source[i], testData[(int)i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Matches<T>(this PointerArray<T> source, PointerArray<T> other)
        {
            if (source.Pointer != other.Pointer)
            {
                return false;
            }

            for (uint i = 0; i < source.Pointer; i++)
            {
                if (!Equals(source[i], other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Matches<T>(this PointerArray<T> source, ArrayShifter<T> other)
        {
            IReadOnlyList<T> testData = other.BackingData;

            for (uint i = 0; i < source.Pointer; i++)
            {
                if (!Equals(source[i], testData[(int)i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}