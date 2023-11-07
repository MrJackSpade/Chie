using Llama.Core.Interfaces;
using Llama.Core.Utils;
using Llama.Data.Models;

namespace Llama.Core.Tests.TestObjects
{
    public class ArrayShifter<T> : IArrayShifter<T>
    {
        private readonly IndexedItem[] _underlyingData;

        public ArrayShifter(KvCacheState<T> startingState)
        {
            _underlyingData = new IndexedItem[startingState.Length];

            for (uint i = 0; i < startingState.Length; i++)
            {
                _underlyingData[i] = new IndexedItem() { Value = startingState[i], Pos = (int)i };
            }
        }

        public IReadOnlyList<T> BackingData
        {
            get
            {
                T[] toReturn = new T[_underlyingData.Length];

                HashSet<int> visited = new();

                foreach (IndexedItem item in _underlyingData)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    if (visited.Add(item.Pos))
                    {
                        toReturn[item.Pos] = item.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                return toReturn;
            }
        }

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Decode(BatchDecode<T> batch)
        {
            foreach (BatchItem<T> item in batch.Items)
            {
                for (int i = 0; i < _underlyingData.Length; i++)
                {
                    if (_underlyingData[i] is null)
                    {
                        _underlyingData[i] = new IndexedItem()
                        {
                            Value = item.Token,
                            Pos = (int)item.Position
                        };

                        break;
                    }
                }
            }
        }

        public void Evaluate(T[] tokens, uint startPos)
        {
            if (startPos + tokens.Length > _underlyingData.Length)
            {
                throw new ArgumentOutOfRangeException("The provided tokens exceed the array bounds.");
            }

            Array.Copy(tokens, 0, _underlyingData, startPos, tokens.Length);
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

        public void RemoveCacheTokens(uint start, uint end)
            => RemoveCacheTokens(0, start, end);

        public void RemoveCacheTokens(uint sequenceId, uint startPos, uint endPos)
        {
            for (int i = 0; i < _underlyingData.Length; i++)
            {
                IndexedItem item = _underlyingData[i];

                if (item != null && item.Pos >= startPos && item.Pos < endPos)
                {
                    _underlyingData[i] = null;
                }
            }
        }

        public void ShiftCacheToken(uint sequenceId, uint index, int delta)
            => ShiftCacheTokens(sequenceId, index, index, delta);

        public void ShiftCacheTokens(uint sequenceId, uint startPos, uint endPos, int delta)
        {
            for (int i = 0; i < _underlyingData.Length; i++)
            {
                IndexedItem item = _underlyingData[i];

                if (item != null && item.Pos >= startPos && item.Pos < endPos)
                {
                    item.Pos += delta;
                    item.Delta += delta;
                }
            }
        }

        public class Operation
        {
            public Operation(string name, params int[] args)
            {
                OperationName = name;
                Parameters = args;
            }

            public string OperationName { get; }

            public int[] Parameters { get; }

            public override string ToString()
            {
                return $"{OperationName}({string.Join(", ", Parameters)})";
            }
        }

        private class IndexedItem
        {
            public int Delta { get; set; }

            public int Pos { get; set; }

            public T Value { get; set; }
        }
    }
}