using ChieApi.Models;
using ChieApi.Shared.Entities;
using Llama.Data.Interfaces;

namespace UserSummarizer
{
    public class MessageToSummarizeCollection
    {
        private readonly LlamaTokenCache _cache;

        private readonly List<IReadOnlyLlamaTokenCollection> _tokenizedMessages = new();

        public MessageToSummarizeCollection(LlamaTokenCache cache, int maxSize)
        {
            this._cache = cache;
            this.MaxSize = maxSize;
        }

        public int Count => this._tokenizedMessages.Count;

        public int CurrentSize => this.TokenizedMessages.Sum(t => t.Count);

        public long LastMessageId { get; set; }

        public int MaxSize { get; private set; }

        public IEnumerable<IReadOnlyLlamaTokenCollection> TokenizedMessages => this._tokenizedMessages;

        public async Task<bool> Add(string user)
        {
            IReadOnlyLlamaTokenCollection thisCollection = await this._cache.Get(user);

            this._tokenizedMessages.Add(thisCollection);

            return true;
        }

        public async Task<bool> Add(ChatEntry chatEntry)
        {
            IReadOnlyLlamaTokenCollection thisCollection = await this._cache.Get(chatEntry.Content);

            if (this.CurrentSize + thisCollection.Count > this.MaxSize)
            {
                return false;
            }

            this.LastMessageId = Math.Max(chatEntry.Id, this.LastMessageId);

            this._tokenizedMessages.Add(thisCollection);

            return true;
        }

        public async Task AddRange(IEnumerable<ChatEntry> entries)
        {
            foreach (ChatEntry entry in entries)
            {
                bool success = await this.Add(entry);

                if (!success)
                {
                    return;
                }
            }
        }
    }
}