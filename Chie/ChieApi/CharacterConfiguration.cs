using ChieApi.Services;

namespace ChieApi
{
    public class CharacterConfiguration : LlamaSettings
    {
        public int AsteriskCap { get; set; }

        public bool BreakOnNewline { get; set; } = true;

        public string CharacterName { get; set; }

        public bool ClientRepetitionPenalty { get; set; } = false;

        public string EndHeaderToken { get; set; } = ":";

        public string EndOfTextToken { get; set; } = string.Empty;

        /// <summary>
        /// Number of messages back to add the first token to the blocked list
        /// </summary>
        public int LookbackBlock { get; set; } = 1;

        public bool ManageResponseLength { get; set; } = true;

        public DateTime MemoryStart { get; set; }

        public bool MergeWords { get; set; } = true;

        public bool ReloadPrompt { get; set; }

        public float ResponseLengthAdjust { get; set; } = 0.1f;

        public float ResponseLengthBias { get; set; }

        /// <summary>
        /// Set to anything but 13 to trigger a non-return on newline.
        /// </summary>
        public int[] ReturnCharacters { get; set; } = new int[] { 13 };

        public bool RoleplayAsterisks { get; set; } = true;

        public bool SpellingCorrect { get; set; } = true;

        public bool SplitWords { get; set; } = true;

        public string StartHeaderToken { get; set; } = "|";

        public bool StartVisible { get; set; }

        public bool UserMemory { get; set; }

        public string? UserPrompt { get; set; }
    }
}