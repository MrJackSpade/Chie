﻿using Llama.Core.Interfaces;
using Llama.Data.Exceptions;
using Llama.Data.Models;
using Llama.Data.Native;
using Llama.Native;
using System.Diagnostics.CodeAnalysis;

namespace Llama.Core.Utils
{
    internal partial class KvCacheShifter : IArrayShifter<LlamaToken>
    {
        private readonly uint _batchSize;

        private readonly SafeLlamaContextHandle _handle;

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private readonly SafeLlamaModelHandle _model;

        private readonly uint _threadCount;

        public KvCacheShifter(uint threadCount, uint batchSize, SafeLlamaContextHandle handle, SafeLlamaModelHandle modelHandle)
        {
            _threadCount = threadCount;
            _handle = handle;
            _model = modelHandle;
            _batchSize = batchSize;
        }

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Decode(BatchDecode<LlamaToken> batch)
        {
            BatchDecode<int> idBatch = new();

            foreach (BatchItem<LlamaToken> oldItem in batch.Items)
            {
                idBatch.AddItem(oldItem.Token.Id, oldItem.Position, oldItem.SequenceIds, oldItem.IncludeLogits);
            }

            NativeApi.Decode(this._handle, idBatch, _batchSize);
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

        public void RemoveCacheToken(uint index)
            => RemoveCacheTokens(index, index + 1);

        public void RemoveCacheTokens(uint startPos, uint endPos)
            => NativeApi.RemoveCacheTokens(this._handle, startPos, endPos);

        public void ShiftCacheToken(uint sequenceId, uint index, int delta)
            => NativeApi.ShiftCacheTokens(this._handle, sequenceId, index, index + 1, delta);

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
            => NativeApi.ShiftCacheTokens(_handle, sequenceId, startPos, endPos, delta);

        public void Validate(KvCacheState<LlamaToken> kvCache)
        {
            var evaluated = NativeApi.GetEvaluated(this._handle, this._model);

            for (int i = 0; i < kvCache.Length; i++)
            {
                if (evaluated[i] != kvCache[(uint)i])
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void Log(BatchDecode<int> idBatch)
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }

            string fName = Path.Combine("Logs", DateTime.Now.Ticks.ToString() + "_batch.json");

            string json = System.Text.Json.JsonSerializer.Serialize(idBatch);

            File.WriteAllText(fName, json);
        }
    }
}