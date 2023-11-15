using ChieApi.Services;

namespace ChieApi
{
    public class CharacterConfiguration : LlamaSettings
    {
        public int AsteriskCap { get; set; }

        public string CharacterName { get; set; }

        public DateTime MemoryStart { get; set; }

        public bool StartVisible { get; set; }

        public float Tfs { get; set; }
    }
}