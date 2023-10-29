using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Core.Interfaces
{
    public interface IArrayShifter<T>
    {
        /// <summary>
        /// Copy all tokens that belong to the specified source sequence to another destination sequence.
        /// Note that this does not allocate extra KV cache memory - it simply assigns the tokens to the new sequence.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos);

        /// <summary>
        /// Adds relative position "delta" to all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// If the KV cache is RoPEd, the KV data is updated accordingly.
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta);

        /// <summary>
        /// Remove all tokens data of cells in [start, end)
        /// start < 0 : [0,  end]
        /// end < 0 : [start, inf)
        /// </summary>
        void RemoveCacheTokens(uint start, uint end);

        /// <summary>
        /// Removes all tokens that belong to the specified sequence and have positions in [startPos, endPos)
        /// startPos < 0 : [0,  endPos]
        /// endPos < 0 : [startPos, inf)
        /// </summary>
        void RemoveCacheTokens(uint sequenceId, uint startPos, uint endPos);

        /// <summary>
        /// Removes all tokens that do not belong to the specified sequence.
        /// </summary>
        void KeepCacheTokens(uint sequenceId);

        /// <summary>
        /// Returns the number of tokens in the KV cache
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        int GetCacheTokenCount();

        /// <summary>
        /// Evaluates the tokens
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        void Evaluate(T[] tokens, uint startPos);
    }
}
