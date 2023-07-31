using ChieApi.Interfaces;
using Llama.Data.Models;
using System.Collections;

namespace ChieApi.Models
{
    public class TokenCollectionCollection : IList<ITokenCollection>
    {
        private readonly List<ITokenCollection> _tokenCollections = new();

        public int Count => ((ICollection<ITokenCollection>)this._tokenCollections).Count;

        public bool IsReadOnly => ((ICollection<ITokenCollection>)this._tokenCollections).IsReadOnly;

        public ITokenCollection this[int index] { get => ((IList<ITokenCollection>)this._tokenCollections)[index]; set => ((IList<ITokenCollection>)this._tokenCollections)[index] = value; }

        public void Add(ITokenCollection item) => ((ICollection<ITokenCollection>)this._tokenCollections).Add(item);

        public void Clear() => ((ICollection<ITokenCollection>)this._tokenCollections).Clear();

        public bool Contains(ITokenCollection item) => ((ICollection<ITokenCollection>)this._tokenCollections).Contains(item);

        public void CopyTo(ITokenCollection[] array, int arrayIndex) => ((ICollection<ITokenCollection>)this._tokenCollections).CopyTo(array, arrayIndex);

        public IEnumerator<ITokenCollection> GetEnumerator() => ((IEnumerable<ITokenCollection>)this._tokenCollections).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._tokenCollections).GetEnumerator();

        public async Task<int> GetTokenCount()
        {
            int result = 0;

            foreach (ITokenCollection tokens in this)
            {
                await foreach (LlamaToken token in tokens)
                {
                    result++;
                }
            }

            return result;
        }

        public int IndexOf(ITokenCollection item) => ((IList<ITokenCollection>)this._tokenCollections).IndexOf(item);

        public void Insert(int index, ITokenCollection item) => ((IList<ITokenCollection>)this._tokenCollections).Insert(index, item);

        public bool Remove(ITokenCollection item) => ((ICollection<ITokenCollection>)this._tokenCollections).Remove(item);

        public void RemoveAt(int index) => ((IList<ITokenCollection>)this._tokenCollections).RemoveAt(index);
    }
}