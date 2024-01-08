using Llama.Core.Interfaces;
using Llama.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Llama.Core.Extensions
{
    public static class ISimpleSamplerExtensions
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyLlamaTokenCollection collection, int tryTake, HashSet<int>? exclude = null) => new(collection, tryTake, exclude);
    }

    public class LastTokens
    {
        public LastTokens(IReadOnlyLlamaTokenCollection collection, int tryTake, HashSet<int>? exclude = null)
        {
            IEnumerable<int> availableEnumerable = collection.Trim().Ids;

            if(exclude!=null)
            {
                availableEnumerable = availableEnumerable.Where(t => !exclude.Contains(t));
            }

            int[] available = availableEnumerable.ToArray();

            if (tryTake == -1)
            {
                tryTake = available.Length;
            }

            int canTake = Math.Min(available.Length, tryTake);
            this.Ids = available[^canTake..];
            this.Length = this.Ids.Length;
        }

        public int[] Ids { get; set; }

        public int Length { get; set; }
    }
}