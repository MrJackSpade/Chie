namespace ChatVectorizer
{
    internal class EmbeddingsJob
    {
        public EmbeddingsJob(long chatEntryId, string content)
        {
            this.ChatEntryId = chatEntryId;
            this.Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public long ChatEntryId { get; private set; }

        public string Content { get; private set; }
    }
}