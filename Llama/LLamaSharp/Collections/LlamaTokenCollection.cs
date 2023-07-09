using Llama.Collections.Interfaces;

/* Unmerged change from project 'LlamaSharp (net7.0)'
Before:
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Llama.Extensions;
using Llama.Constants;
After:
using System.Constants;
using System.Data;
using Llama.Extensions;
using System;
using Llama.Collections;
using System.Collections.Constants;
*/

using Llama.Data;
using Llama.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Llama.Collections
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

        public bool IsSingleLlamaTokenTag => this.LlamaTokenTags.Count() == 1;

        public IEnumerable<string> LlamaTokenTags => this._tokens.Select(t => t.Tag).Distinct();

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

        public virtual void Append(LlamaToken token)
        {
            if (token.Id == 30004)
            {
                Debugger.Break();
            }

            if(token.Value == "|")
            {
                if(this.Count > 1 && this.Last().Id != 13)
                {
                    //Debugger.Break();
                }
            }

            this._tokens.Add(token);
        }

        public virtual void Clear() => this._tokens.Clear();

        public virtual void Ensure()
        {
            LlamaTokenCollection[] lineCollection = this.Split(13).Select(l => l.Trim()).ToArray();

            for (int i = 0; i < lineCollection.Length; i++)
            {
                LlamaTokenCollection l = lineCollection[i];

                if (i == lineCollection.Length - 1 && l.Count == 0)
                {
                    continue;
                }

                if (l.IsNullOrWhiteSpace)
                {
                    //Debugger.Break();
                }

                if (!l.ToString().StartsWith("|") && l.Count > 0)
                {
                    //Debugger.Break();
                }

                if (!l.IsSingleLlamaTokenTag)
                {
                    List<string> tags = l.LlamaTokenTags.ToList();

                    tags.Remove(Llama.Constants.LlamaTokenTags.CONTROL);

                    if (tags.Count > 1 && tags.Contains(Llama.Constants.LlamaTokenTags.TEMPORARY))
                    {
                        Debugger.Break();
                    }
                }
            }
        }

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
                    llamaTokens.Append(tokens);
                    tokens.Clear();
                }
            }

            return llamaTokens;
        }
    }
}