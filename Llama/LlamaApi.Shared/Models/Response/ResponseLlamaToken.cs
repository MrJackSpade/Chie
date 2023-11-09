using Llama.Data.Models;

namespace LlamaApi.Shared.Models.Response
{
    public class ResponseLlamaToken
    {
        public ResponseLlamaToken()
        {
        }

        public ResponseLlamaToken(LlamaToken t)
        {
            this.Id = t.Id;
            this.Value = t.Value;
        }

        public int Id { get; set; }

        public string Value { get; set; }
    }
}