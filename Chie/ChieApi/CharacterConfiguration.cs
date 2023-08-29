using ChieApi.Services;

namespace ChieApi
{
    public class CharacterConfiguration : LlamaSettings
    {
        public string CharacterName { get; set; }
        public DateTime MemoryStart { get; set; }
	}
}