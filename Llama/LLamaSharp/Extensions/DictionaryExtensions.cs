using System.Collections.Generic;

namespace Llama.Extensions
{
    public static class DictionaryExtension
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> pair, out T1 first, out T2 second)
        {
            first = pair.Key;
            second = pair.Value;
        }

        public static T2 GetOrDefault<T1, T2>(this Dictionary<T1, T2> dic, T1 key, T2 defaultValue)
        {
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }

            return defaultValue;
        }

        public static void Update<T1, T2>(this Dictionary<T1, T2> dic, IDictionary<T1, T2> other)
        {
            foreach ((T1 key, T2 value) in other)
            {
                dic[key] = value;
            }
        }
    }
}