﻿using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ChieApi.Shared.Entities
{
    public class LogEntry
    {
        [JsonPropertyName("applicationName")]
        public string? ApplicationName { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("dateCreated")]
        public DateTime DateCreated { get; set; }

        [JsonPropertyName("eventId")]
        public int EventId { get; set; }

        [JsonPropertyName("eventName")]
        public string? EventName { get; set; }

        [Key]
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("level")]
        public LogLevel Level { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}