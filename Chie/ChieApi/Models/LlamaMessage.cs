using ChieApi.Interfaces;
using Llama.Data.Collections;
using Llama.Data.Extensions;
using Llama.Data.Interfaces;
using Llama.Data.Models;

namespace ChieApi.Models
{
	public class LlamaMessage : ITokenCollection
	{
		private readonly LlamaTokenCache _cache;

		private LlamaTokenCollection? _tokens;

		public LlamaMessage(string? header, string? content, string endOfText, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (string.IsNullOrEmpty(header))
			{
				throw new ArgumentException($"'{nameof(header)}' cannot be null or empty.", nameof(header));
			}

			if (string.IsNullOrEmpty(content))
			{
				throw new ArgumentException($"'{nameof(content)}' cannot be null or empty.", nameof(content));
			}

			if (cache is null)
			{
				throw new ArgumentNullException(nameof(cache));
			}

			this.EndOfText = new(endOfText, cache, true);
			this.Header = new(header, cache, true);
			this.Content = new(" " + content.Trim(), cache, false);
			this.Type = type;
			this._cache = cache;
		}

		public LlamaMessage(string? userName, IReadOnlyLlamaTokenCollection content, string endOfText, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (string.IsNullOrEmpty(userName))
			{
				throw new ArgumentException($"'{nameof(userName)}' cannot be null or empty.", nameof(userName));
			}

			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (cache is null)
			{
				throw new ArgumentNullException(nameof(cache));
			}

			this.EndOfText = new(endOfText, cache, true);
			this.Header = new(userName, cache, true);
			this.Content = new(content);
			this.Type = type;
			this._cache = cache;
		}

		public LlamaMessage(IReadOnlyLlamaTokenCollection header, IReadOnlyLlamaTokenCollection content, IReadOnlyLlamaTokenCollection? endOfText, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (header is null)
			{
				throw new ArgumentNullException(nameof(header));
			}

			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (endOfText is not null)
			{
				this.EndOfText = new(endOfText);
			}

			this.Header = new(header);
			this.Content = new(content);
			this.Type = type;
			this._cache = cache;
		}

		public LlamaMessage(CachedTokenCollection header, IReadOnlyLlamaTokenCollection content, CachedTokenCollection endOfText, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (header is null)
			{
				throw new ArgumentNullException(nameof(header));
			}

			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			this.EndOfText = endOfText;
			this.Header = header;
			this.Content = new(content);
			this.Type = type;
			this._cache = cache;
		}

		public LlamaMessage(CachedTokenCollection userName, CachedTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (userName is null)
			{
				throw new ArgumentNullException(nameof(userName));
			}

			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			this.Header = userName;
			this.Content = content;
			this.Type = type;
			this._cache = cache;
		}

		public LlamaMessage(IReadOnlyLlamaTokenCollection userName, CachedTokenCollection content, LlamaTokenType type, LlamaTokenCache cache)
		{
			if (userName is null)
			{
				throw new ArgumentNullException(nameof(userName));
			}

			if (content is null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			this.Header = new(userName);
			this.Content = content;
			this.Type = type;
			this._cache = cache;
		}

		public CachedTokenCollection Content { get; }

		public long Id { get; set; }

		public LlamaTokenType Type { get; }

		public CachedTokenCollection Header { get; }

		public CachedTokenCollection EndOfText { get; }
		public async IAsyncEnumerator<LlamaToken> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			await this.EnsureTokens();

			foreach (LlamaToken token in this._tokens!)
			{
				yield return token;
			}
		}

		private async Task EnsureTokens()
		{
			if (this._tokens == null)
			{
				LlamaTokenCollection tokens = new();
				await tokens.Append(this.Header);
				await tokens.Append(this.Content);
				await tokens.Append(this.EndOfText);
				this._tokens = tokens;
			}
		}
	}
}