using Llama.Collections;
using Llama.Collections.Interfaces;

namespace ChatVectorizer
{
    internal class EmbeddingsJob
    {
        public EmbeddingsJob(long chatEntryId, string content, LlamaTokenCollection tokens)
        {
            this.ChatEntryId = chatEntryId;
            this.Content = content ?? throw new ArgumentNullException(nameof(content));
            this.Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public long ChatEntryId { get; private set; }

        public string Content { get; private set; }

        public IReadOnlyLlamaTokenCollection Tokens { get; private set; }
    }
}