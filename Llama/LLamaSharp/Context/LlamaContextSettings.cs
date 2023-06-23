using System.Collections.Generic;
using System.Text;

namespace Llama.Context
{
    public class LlamaContextSettings
    {
        public List<string> Antiprompt { get; set; } = new();

        public bool AutoLoad { get; set; } = true;

        public bool AutoSave { get; set; } = true;

        public int BatchSize { get; set; } = 512;

        public int ContextSize { get; set; }

        public Encoding Encoding { get; set; }

        public string InputPrefix { get; set; } = string.Empty;

        public string InputSuffix { get; set; } = string.Empty;

        public bool Instruct { get; set; } = false;

        public bool Interactive { get; set; } = false;

        public bool InteractiveFirst { get; set; } = false;

        public int KeepContextTokenCount { get; set; } = 0;

        public Dictionary<int, float> LogitBias { get; set; } = new();

        public bool PenalizeNewlines { get; set; } = true;

        public int PredictCount { get; set; } = -1;

        public string Prompt { get; set; } = string.Empty;

        public string RootSaveName { get; set; } = "Context";

        public string SavePath { get; set; } = "StateSaves";

        public string SessionPath { get; set; } = string.Empty;

        public bool UseRandomPrompt { get; set; } = false;
    }
}