namespace Llama.Data.Models
{
    public record Embedding(string Object, string Model, EmbeddingData[] Data, EmbeddingUsage Usage);
}