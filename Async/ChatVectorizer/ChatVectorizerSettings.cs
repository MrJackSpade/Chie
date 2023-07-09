using ChieApi.Interfaces;
using ChieApi.Shared.Services;
using Llama.Context;
using Llama.Shared;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace ChatVectorizer
{
    public class ChatVectorizerSettings : LlamaSettings, IHasConnectionString
    {
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }

        [JsonPropertyName("defaultModel")]
        public string DefaultModel { get; set; }
    }
}