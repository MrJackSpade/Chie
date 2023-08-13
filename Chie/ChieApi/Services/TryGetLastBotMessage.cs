using ChieApi.Models;

namespace ChieApi.Services
{
    public partial class LlamaService
    {
        private class TryGetLastBotMessage
        {
            public LlamaMessage Message { get; set; }

            public bool Success { get; set; }
        }
    }
}