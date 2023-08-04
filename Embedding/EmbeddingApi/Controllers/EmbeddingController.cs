using Microsoft.AspNetCore.Mvc;
using ImageRecognition;
using Embedding.Models;

namespace EmbeddingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmbeddingController : ControllerBase
    {

        protected EmbeddingClient _client;

        private static readonly AutoResetEvent _gate = new(true);

        public EmbeddingController(EmbeddingClient client)
        {
            this._client = client;
        }

        [HttpPost("Generate")]
        public async Task<TokenizeResponse> Generate(TokenizeRequest request)
        {
            _gate.WaitOne();

            try
            {
                float[][] response = await this._client.Generate(request.TextData);

                return new TokenizeResponse()
                {
                    Content = response,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new TokenizeResponse()
                {
                    Exception = ex.Message,
                    Success = false
                };
            }
            finally
            {
                _gate.Set();
            }
        }
    }
}