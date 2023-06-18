using Llama.Collections.Interfaces;

/* Unmerged change from project 'LLamaSharp (net7.0)'
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
using System;
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

        public virtual void Append(LlamaToken token) => this._tokens.Add(token);

        public void AppendControl(int id) => this.Append(new LlamaToken(id, IntPtr.Zero, Llama.Constants.LlamaTokenTags.CONTROL));

        public virtual void Clear() => this._tokens.Clear();

        public bool Contains(int tokenId)
        {
            foreach (LlamaToken token in this._tokens)
            {
                if (token.Id == tokenId)
                {
                    return true;
                }
            }

            return false;
        }

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

        public LlamaTokenCollection From(int startIndex, LlamaToken startToken)
        {
            // Calculate the index to start from
            int start = this._tokens.Count - startIndex;

            // Ensure the index is within valid bounds
            if (start < 0)
            {
                start = 0;
            }
            else if (start > this._tokens.Count)
            {
                start = this._tokens.Count;
            }

            // Find the first instance of startToken
            int index = this._tokens.FindIndex(start, token => startToken.Id == token.Id);

            // If startToken was not found, use the original start position
            if (index == -1)
            {
                index = start;
            }

            // Copy from the found position (or the original start position if startToken was not found)
            return new LlamaTokenCollection(this._tokens.Skip(index));
        }

        public IEnumerator<LlamaToken> GetEnumerator() => ((IEnumerable<LlamaToken>)this._tokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this._tokens).GetEnumerator();

        public LlamaTokenCollection Replace(LlamaTokenCollection toFind, LlamaTokenCollection toReplace)
        {
            LlamaTokenCollection toReturn = new();

            for (int i = 0; i < this.Count; i++)
            {
                bool isMatch = false;

                if (i + toFind.Count <= this.Count)
                {
                    for (int ii = 0; ii < toFind.Count; ii++)
                    {
                        LlamaToken tokenA = toFind[ii];
                        LlamaToken tokenB = this[ii + i];

                        if (tokenA.Value == tokenB.Value)
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    i += toFind.Count;
                    foreach (LlamaToken tokenA in toReplace)
                    {
                        toReturn.Append(tokenA);
                    }
                }
                else
                {
                    toReturn.Append(this[i]);
                }
            }

            return toReturn;
        }

        public void Shift(LlamaToken token)
        {
            this._tokens.RemoveAt(0);
            this._tokens.Add(token);
        }

        public IEnumerable<LlamaTokenCollection> Split(int id)
        {
            LlamaTokenCollection toReturn = new();

            foreach (LlamaToken token in this._tokens)
            {
                if (token.Id == id)
                {
                    yield return toReturn;
                    toReturn = new LlamaTokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }

        public IEnumerable<LlamaTokenCollection> Split(string value, StringComparison stringComparison = StringComparison.Ordinal)
        {
            LlamaTokenCollection toReturn = new();

            foreach (LlamaToken token in this._tokens)
            {
                if (string.Equals(token.Value, value, stringComparison))
                {
                    yield return toReturn;
                    toReturn = new LlamaTokenCollection();
                }
                else
                {
                    toReturn.Append(token);
                }
            }

            yield return toReturn;
        }

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