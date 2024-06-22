using Llama.Core.Interfaces;
using Llama.Data.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Llama.Core.Extensions
{
    public static class ISimpleSamplerExtensions
    {
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyLlamaTokenCollection collection, int tryTake) => new(collection, tryTake, new HashSet<int>(), new HashSet<int>());

        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public static LastTokens GetLastTokens(this ISimpleSampler sampler, IReadOnlyLlamaTokenCollection collection, int tryTake, HashSet<int> include, HashSet<int> exclude) => new(collection, tryTake, include, exclude);
    }

    public class LastTokens
    {
        public LastTokens(IReadOnlyLlamaTokenCollection collection, int tryTake, HashSet<int> include, HashSet<int> exclude)
        {
            int availableCount = collection.Trim().Ids.Count();

            if (tryTake == -1)
            {
                tryTake = availableCount;
            }

            int canTake = Math.Min(availableCount, tryTake);

            int skip = availableCount - canTake;

            IEnumerable<int> availableEnumerable = collection.Trim().Ids.Skip(skip).Take(canTake);

            if (exclude.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(t => !exclude.Contains(t));
            } else if(include.Count > 0)
            {
                availableEnumerable = availableEnumerable.Where(t => include.Contains(t));
            } 
     
            this.Ids = availableEnumerable.ToArray();
            this.Length = this.Ids.Length;
        }

        public int[] Ids { get; set; }

        public int Length { get; set; }
    }
}