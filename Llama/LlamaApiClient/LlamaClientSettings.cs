namespace LlamaApiClient
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

        public bool Async { get; set; }

        public string Host { get; set; }

        public int PollingFrequencyMs { get; set; } = 100;
    }
}