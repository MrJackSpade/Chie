using Llama.Data.Enums;
using Llama.Data.Interfaces;
using Llama.Data.Models;
using System.Collections;

namespace Llama.Data.Collections
{
    public class LlamaTokenCollection : ILlamaTokenCollection
    {
        protected List<LlamaToken> _tokens = new();

        public LlamaTokenCollection(IEnumerable<LlamaToken> tokens)
        {
            foreach (LlamaToken token in tokens)
            {
                this.Append(token);
            }
        }

        public LlamaTokenCollection()
        {
        }

        public int Count => this._tokens.Count;

        public IEnumerable<int> Ids => this._tokens.Select(t => t.Id);

        public bool IsNullOrEmpty => this._tokens.Count == 0;

        public bool IsNullOrWhiteSpace => string.IsNullOrWhiteSpace(this.ToString());

        public bool IsSingleLlamaTokenTag => this.LlamaTokenType.Count() == 1;

        public IEnumerable<LlamaTokenType> LlamaTokenType => this._tokens.Select(t => t.TokenType).Distinct();

        public LlamaToken this[int index]
        {
            get => this._tokens[index];
            set => this._tokens[index] = value;
        }

        public static LlamaTokenCollection operator +(LlamaTokenCollection a, LlamaToken b)
        {
            LlamaTokenCollection toReturn = new();

            foreach (LlamaToken token in a)
            {
                toReturn.Append(token);
            }

            toReturn.Append(b);

            return toReturn;
        }

        public static LlamaTokenCollection operator +(LlamaToken a, LlamaTokenCollection b)
        {
            LlamaTokenCollection toReturn = new();

            toReturn.Append(a);

            foreach (LlamaToken token in b)
            {
                toReturn.Append(token);
            }

            return toReturn;
        }

        public virtual void Append(LlamaToken token) => this._tokens.Add(token);

        public virtual void Clear() => this._tokens.Clear();

        public IEnumerator<LlamaToken> GetEnumerator() => ((IEnumerable<LlamaToken>)this._tokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._tokens).GetEnumerator();

        public void Shift(LlamaToken token)
        {
            this._tokens.RemoveAt(0);
            this._tokens.Add(token);
        }

        public string ToEscapedString() => string.Join("", this._tokens.Select(t => t.EscapedValue));

        public override string ToString() => string.Join("", this._tokens.Select(t => t.Value));

        public virtual LlamaTokenCollection Trim(int id = 0)
        {
            LlamaTokenCollection llamaTokens = new();

            List<LlamaToken> tokens = new();

            bool isStarted = false;

            foreach (LlamaToken token in this._tokens)
            {
                if (token.Id != id)
                {
                    isStarted = true;
                }

                if (isStarted)
                {
                    tokens.Add(token);
                }

                if (token.Id != id)
                {
                    foreach (LlamaToken lToken in tokens)
                    {
                        llamaTokens.Append(lToken);
                    }

                    tokens.Clear();
                }
            }

            return llamaTokens;
        }
    }
}