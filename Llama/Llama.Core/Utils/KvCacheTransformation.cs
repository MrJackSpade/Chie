namespace Llama.Core.Utils
{
    public class KvCacheTransformation<T>
    {
        public KvCacheTransformation()
        { }

        public KvCacheTransformation(T item, uint oldIndex, uint newIndex)
        {
            Item = item;
            OriginalIndex = oldIndex;
            NewIndex = newIndex;
        }

        public KvCacheTransformation(T item, uint index) : this(item, index, index)
        {
        }

        public int Delta => (int)(NewIndex - OriginalIndex);

        public T Item { get; set; }

        public uint NewIndex { get; set; }

        public uint OriginalIndex { get; set; }

        public override string ToString()
        {
            return $"{Item} [{OriginalIndex} => {NewIndex}]";
        }
    }
}