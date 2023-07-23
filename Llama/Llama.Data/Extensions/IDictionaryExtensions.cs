namespace Llama.Data.Extensions
{
    public static class IDictionaryExtensions
    {
        public static void Add<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in source)
            {
                target.Add(kvp.Key, kvp.Value);
            }
        }

        public static void Add(this IDictionary<int, float> target, IDictionary<int, string> source)
        {
            foreach (KeyValuePair<int, string> kvb in source)
            {
                float v;

                if (string.Equals("-inf", kvb.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.NegativeInfinity;
                }
                else if (string.Equals("+inf", kvb.Value, StringComparison.OrdinalIgnoreCase))
                {
                    v = float.PositiveInfinity;
                }
                else
                {
                    v = float.Parse(kvb.Value);
                }

                target.Add(kvb.Key, v);
            }
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in source)
            {
                target.AddOrUpdate(kvp);
            }
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> target, KeyValuePair<TKey, TValue> kvp)
        {
            if (target.ContainsKey(kvp.Key))
            {
                target[kvp.Key] = kvp.Value;
            }
            else
            {
                target.Add(kvp.Key, kvp.Value);
            }
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> target, TKey key, TValue value)
        {
            if (target.ContainsKey(key))
            {
                target[key] = value;
            }
            else
            {
                target.Add(key, value);
            }
        }
    }
}