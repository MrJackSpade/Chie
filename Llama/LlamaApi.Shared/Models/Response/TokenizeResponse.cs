using Llama.Data.Models;

namespace LlamaApi.Models.Response
{
    public class TokenizeResponse
    {
        public LlamaToken[] Tokens { get; set; }
    }
}