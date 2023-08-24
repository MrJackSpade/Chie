namespace ChieApi.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsAny(this string value, params char[] chars)
        {
            foreach (char c in value)
            {
                if (chars.Contains(c))
                {
                    return true;
                }
            }

            return false;
        }

        public static int LastIndexOfAny(this string value, params char[] chars)
        {
            int index = -1;

            foreach (char c in chars)
            {
                index = Math.Max(value.LastIndexOf(c), index);
            }

            return index;
        }
    }
}