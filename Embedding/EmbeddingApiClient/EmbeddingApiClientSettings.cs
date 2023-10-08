using System.Text.Json.Serialization;

namespace Embedding
{
	public class EmbeddingApiClientSettings
	{
		[JsonPropertyName("rootUrl")]
		public string RootUrl { get; set; }
	}
}