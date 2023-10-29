using Llama.Core.Interfaces;
using Llama.Data.Collections;

namespace Llama.Core.Tests.TestObjects
{
    public class ArrayShifter<T> : IArrayShifter<T>
    {
        private readonly List<Operation> _operations = new();

        private readonly T[] _underlyingData;

        public ArrayShifter(PointerArray<T> startingState)
        {
            _underlyingData = new T[startingState.Length];

            for (uint i = 0; i < startingState.Length; i++)
            {
                _underlyingData[i] = startingState[i];
            }
        }

        public IReadOnlyList<T> BackingData => _underlyingData.ToList();

        public IReadOnlyList<Operation> Operations => _operations;

        public void CopyCacheTokens(uint sourceSequenceId, uint destinationSequenceId, uint startPos, uint endPos)
        {
            throw new NotImplementedException();
        }

        public void Evaluate(T[] tokens, uint startPos)
        {
            if (startPos + tokens.Length > _underlyingData.Length)
            {
                throw new ArgumentOutOfRangeException("The provided tokens exceed the array bounds.");
            }

            _operations.Add(new Operation(nameof(Evaluate), tokens.Length, (int)startPos));

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
            _operations.Add(new Operation(nameof(ShiftCacheTokens), (int)sequenceId, (int)startPos, (int)endPos, delta));

            // Adjusting for special conditions
            if (startPos < 0)
            {
                startPos = 0;
            }

            if (endPos < 0)
            {
                endPos = (uint)_underlyingData.Length; // Note: Since endPos is exclusive, it can be set to the length of the array
            }

            if (delta > 0)
            {
                // Shift to the right
                for (uint i = endPos - 1; i >= startPos; i--) // Adjusted to make endPos exclusive
                {
                    if (i + delta < _underlyingData.Length)
                    {
                        _underlyingData[i + delta] = _underlyingData[i];
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Shifting exceeds the array bounds.");
                    }
                }
            }
            else if (delta < 0)
            {
                // Shift to the left
                for (uint i = startPos; i < endPos; i++) // Adjusted to make endPos exclusive
                {
                    if (i + delta >= 0)
                    {
                        _underlyingData[i + delta] = _underlyingData[i];
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("Shifting exceeds the array bounds.");
                    }
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
    }
}