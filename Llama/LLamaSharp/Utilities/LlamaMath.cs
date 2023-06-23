using System;
using System.Collections.Generic;

namespace Llama.Utilities.Utilities
{
    public static class LlamaMath
    {
        public static int Min(int v1, int v2, params int[] values)
        {
            List<int> othervalues = new()
            {
                v2
            };

            if (values != null)
            {
                othervalues.AddRange(values);
            }

            int returnValue = v1;

            foreach (int v in othervalues)
            {
                returnValue = Math.Min(returnValue, v);
            }

            return returnValue;
        }
    }
}