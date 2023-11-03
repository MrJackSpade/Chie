using Llama.Core.Interfaces;
using Llama.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Llama.Core.Tests.TestObjects
{
    internal class DummyShifter : IArrayShifter<LlamaToken>
    {
        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {

        }

        public void Decode(BatchDecode<LlamaToken> batch)
        {

        }

        public void Evaluate(LlamaToken[] tokens, uint startPos)
        {

        }

        public int GetCacheTokenCount()
        {
            return 0;
        }

        public void KeepCacheTokens(uint sequenceId)
        {

        }

        public void RemoveCacheToken(uint index)
        {

        }

        public void RemoveCacheTokens(uint start, uint end)
        {

        }

        public void ShiftCacheToken(uint sequenceId, uint index, int delta)
        {

        }

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
        {

        }
    }
}
