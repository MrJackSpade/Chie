using Llama.Core.Interfaces;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;

namespace Llama.Core.Utils
{
    internal class KvCacheShifter : IArrayShifter<LlamaToken>
    {
        private SafeLlamaContextHandle _handle;

        private uint _threadCount;

        public KvCacheShifter(uint threadCount, SafeLlamaContextHandle handle)
        {
            _threadCount = threadCount;
            _handle = handle;
        }

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Evaluate(LlamaToken[] tokens, uint pos)
        {
            if (this._threadCount == 0)
            {
                throw new LlamaCppRuntimeError("Evaluation thread count can not be zero");
            }

            if (NativeApi.Eval(this._handle, tokens.Select(l => l.Id).ToArray(), tokens.Length, pos, (int)this._threadCount) != 0)
            {
                throw new LlamaCppRuntimeError("Failed to eval.");
            }
        }

        public int GetCacheTokenCount()
        {
            throw new NotImplementedException();
        }

        public void KeepCacheTokens(uint sequenceId)
        {
            throw new NotImplementedException();
        }

        public void RemoveCacheTokens(uint start, uint end)
        {
            throw new NotImplementedException();
        }

        public void RemoveCacheTokens(uint sequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
        {
            NativeApi.ShiftCacheTokens(_handle, sequenceId, startPos, endPos, delta);
        }
    }
}