﻿using Llama.Data.Scheduler;
using System.Text.Json.Serialization;

namespace LlamaApi.Models.Request
{
    public class ContextSnapshotRequest
    {
        [JsonPropertyName("contextId")]
        public Guid ContextId { get; set; }

        [JsonPropertyName("priority")]
        public ExecutionPriority Priority { get; set; }
    }
}
