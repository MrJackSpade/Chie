using System.Collections;

namespace Llama.Data.Collections
{
    public class PointerArray<T> : IEnumerable<T>
    {
        private readonly T[] _backingData;
       
        public PointerArray(uint length, params T[] array)
        {
            _backingData = new T[length];

            for (int i = 0; i < array.Length; i++)
            {
                _backingData[i] = array[i];
            }
        }

        public void Fill(T item)
        {
            for(int i = 0; i < _backingData.Length;i++)
            {
                _backingData[i] = item;
            }
        }

        public uint Length => (uint)_backingData.Length;

        public uint Pointer { get; set; }

        public T this[uint index]
        {
            get => _backingData[index];
            set => _backingData[index] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_backingData).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _backingData.GetEnumerator();
        }

        public Span<T> Slice(int startIndex, int length)
        {
            return _backingData.AsSpan().Slice(startIndex, length);
        }

        public void Clear()
        {
            Pointer = 0;
        }
    }
}