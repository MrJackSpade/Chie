using System.Text.Json.Serialization;

namespace Blip
{
    public class BlipApiClientSettings
    {
        [JsonPropertyName("rootUrl")]
        public string RootUrl { get; set; }
    }
}