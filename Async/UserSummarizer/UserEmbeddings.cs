namespace UserSummarizer
{
    public class UserEmbeddings
    {
        public string UserName { get; set; }
        public List<Embedding> Embeddings { get; set; } = new List<Embedding>();
        public override string ToString() => $"{this.UserName} [{this.Embeddings.Count}]";
    }
}