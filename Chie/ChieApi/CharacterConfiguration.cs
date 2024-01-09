using ChieApi.Services;

namespace ChieApi
{
    public class CharacterConfiguration : LlamaSettings
    {
        public int AsteriskCap { get; set; }

        public string CharacterName { get; set; }

        public DateTime MemoryStart { get; set; }

        public bool ReloadPrompt { get; set; }

        public float ResponseLengthAdjust { get; set; } = 0.1f;

        public float ResponseLengthBias { get; set; }

        public bool StartVisible { get; set; }

        public string? UserPrompt { get; set; }
    }
}