using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Logging
{
    public class LoggingApiClientSettings
    {
        [JsonPropertyName("applicationName")]
        public string ApplicationName { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("logLevel")]
        public LogLevel LogLevel { get; set; }

        [JsonPropertyName("logLevelName")]
        public string LogLevelName
        {
            get => this.LogLevel.ToString();
            set => this.LogLevel = Enum.Parse<LogLevel>(value);
        }
    }
}