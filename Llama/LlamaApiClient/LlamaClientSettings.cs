﻿namespace LlamaApiClient
{
    public class LlamaClientSettings
    {
        public LlamaClientSettings()
        {
        }

        public LlamaClientSettings(string host)
        {
            this.Host = host;
        }

        public string Host { get; set; }

        public Guid LlamaContextId { get; set; } = Guid.Empty;

        public int PollingFrequencyMs { get; set; } = 100;
    }
}