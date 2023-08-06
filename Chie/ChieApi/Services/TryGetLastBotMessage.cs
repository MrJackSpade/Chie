using ChieApi.Models;

namespace ChieApi.Services
{
    public partial class LlamaService
    {
        class TryGetLastBotMessage
        {
            public bool Success { get; set; }
            public LlamaMessage Message { get; set; }
        }
    }
}