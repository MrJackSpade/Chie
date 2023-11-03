using System.Collections;

namespace Llama.Core.Utils
{
    public class KvCacheState<T> : IEnumerable<T>
    {
        private readonly T[] _backingData;

        private readonly T _defaultToken;

        private readonly HashSet<uint> _relocated;

        private readonly KvCacheTransformation<T>?[] _transformations;

        public KvCacheState(T[] backingData, T defaultToken)
        {
            _defaultToken = defaultToken;
            _transformations = new KvCacheTransformation<T>[backingData.Length];
            _backingData = backingData;
            _relocated = new HashSet<uint>(_backingData.Length);
        }

        public KvCacheState(uint size, T defaultToken) : this(new T[size], defaultToken)
        {
            for (int i = 0; i < size; i++)
            {
                _backingData[i] = defaultToken;
            }
        }

        public uint Length => (uint)_transformations.Length;

        public T this[uint index]
        {
            get => _backingData[index];
            set => _backingData[index] = value;
        }

        public void ClearTransformations()
        {
            for(int i = 0; i < _transformations.Length;  i++)
            {
                _transformations[i] = null;
            }

            _relocated.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_backingData).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _backingData.GetEnumerator();
        }

        public IEnumerable<KvCacheTransformation<T>> GetMoves()
        {
            foreach (KvCacheTransformation<T>? transform in _transformations.Where(s => (s?.Delta ?? 0) != 0))
            {
                yield return transform!;
            }
        }

        public bool IsDefault(uint index)
        {
            return Equals(_backingData[index], _defaultToken);
        }

        public bool IsMoved(uint index)
        {
            return _relocated.Contains(index);
        }

        public bool IsSet(uint index)
        {
            return _transformations[index] != null;
        }

        public void Move(uint oldIndex, uint newIndex)
        {
            if(IsSet(newIndex))
            {
                throw new Exception("New location already has token set");
            }

            if (IsMoved(oldIndex))
            {
                throw new Exception("Old location has already been moved");
            }

            _relocated.Add(oldIndex);
            _transformations[newIndex] = new KvCacheTransformation<T>(_backingData[oldIndex], oldIndex, newIndex);
        }

        public void Pin(uint index)
        {
            if (IsSet(index))
            {
                throw new Exception("New location already has token set");
            }

            if (IsMoved(index))
            {
                throw new Exception("Old location has already been moved");
            }

            _relocated.Add(index);
            _transformations[index] = new KvCacheTransformation<T>(_backingData[index], index);
        }
    }
}