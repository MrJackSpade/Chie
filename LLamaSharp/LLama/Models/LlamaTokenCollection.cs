using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LLama.Models
{
	public class LlamaTokenCollection : IEnumerable<LlamaToken>
	{
		private readonly List<LlamaToken> _tokens = new();

		public LlamaTokenCollection(IEnumerable<LlamaToken> tokens)
		{
			this._tokens.AddRange(tokens);
		}

		public LlamaTokenCollection()
		{
		}

		public int Count => this._tokens.Count;

		public IEnumerable<int> Ids => this._tokens.Select(t => t.Id);

		public LlamaToken this[int index] => this._tokens[index];

		public static LlamaTokenCollection operator +(LlamaTokenCollection a, LlamaToken b)
		{
			LlamaTokenCollection toReturn = new();

			toReturn.Append(a);

			toReturn.Append(b);

			return toReturn;
		}

		public static LlamaTokenCollection operator +(LlamaToken a, LlamaTokenCollection b)
		{
			LlamaTokenCollection toReturn = new();

			toReturn.Append(a);

			toReturn.Append(b);

			return toReturn;
		}

		public void Append(LlamaToken token) => this._tokens.Add(token);

		public void Append(int id, string value) => this._tokens.Add(new LlamaToken(id, value));

		public void Append(IEnumerable<LlamaToken> collection)
		{
			foreach (LlamaToken token in collection)
			{
				this.Append(token);
			}
		}

		public void AppendControl(IEnumerable<int> ids)
		{
			foreach (int id in ids)
			{
				this.AppendControl(id);
			}
		}

		public void AppendControl(int id) => this._tokens.Add(new LlamaToken(id, null));

		public void Clear() => this._tokens.Clear();

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
					toReturn.Append(toReplace);
				} else
				{
					toReturn.Append(this[i]);
				}
			}

			return toReturn;
		}

		public void Slide(LlamaToken token)
		{
			this._tokens.RemoveAt(0);
			this._tokens.Add(token);
		}

		public void Slide(IEnumerable<LlamaToken> tokens)
		{
			foreach (LlamaToken token in tokens)
			{
				this.Slide(token);
			}
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

		public override string ToString()
		{
			StringBuilder sb = new();

			foreach (LlamaToken token in this._tokens)
			{
				sb.Append(token.ToString());
			}

			return sb.ToString();
		}
	}
}