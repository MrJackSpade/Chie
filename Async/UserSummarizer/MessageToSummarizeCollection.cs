using ChieApi.Shared.Entities;
using Llama.Collections;
using Llama.Context;

namespace UserSummarizer
{
    public class MessageToSummarizeCollection
    {
        private readonly ContextEvaluator _context;
        public int MaxSize => (int)(this._context.ContextSize * 0.8);
        public int CurrentSize => this.TokenizedMessages.Sum(t => t.Count);
        public int Count => this._tokenizedMessages.Count;
        public long LastMessageId { get; set; }
        public IEnumerable<LlamaTokenCollection> TokenizedMessages => this._tokenizedMessages;
        private readonly List<LlamaTokenCollection> _tokenizedMessages = new();

        public MessageToSummarizeCollection(ContextEvaluator context)
        {
            this._context = context;
        }

        public void AddRange(IEnumerable<ChatEntry> entries)
        {
            foreach (ChatEntry entry in entries)
            {
                bool success = this.Add(entry);

                if (!success)
                {
                    return;
                }
            }
        }
        public bool Add(string user)
        {
            LlamaTokenCollection thisCollection = this._context.Tokenize(user);

            _tokenizedMessages.Add(thisCollection);

            return true;
        }
        public bool Add(ChatEntry chatEntry)
        {
            LlamaTokenCollection thisCollection = this._context.Tokenize(chatEntry.Content);

            if (this.CurrentSize + thisCollection.Count > this.MaxSize)
            {
                return false;
            }

            this.LastMessageId = Math.Max(chatEntry.Id, this.LastMessageId);

            _tokenizedMessages.Add(thisCollection);

            return true;
        }
    }
}
