using ChieApi.Interfaces;
using System.Text.Json.Serialization;

namespace ChieApi
{
	public class ChieApiSettings : IHasConnectionString
	{
		[JsonPropertyName("connectionString")]
		public string ConnectionString { get; set; }

		[JsonPropertyName("defaultModel")]
		public string DefaultModel { get; set; }

		[JsonPropertyName("LlamaMainExe")]
		public string LlamaMainExe { get; set; }
	}
}