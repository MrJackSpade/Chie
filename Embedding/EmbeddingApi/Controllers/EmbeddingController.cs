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
        public async Task<EmbeddingResponse> Generate(EmbeddingRequest request)
        {
            _gate.WaitOne();

            try
            {
                float[][] response = await this._client.Generate(request.TextData);

                return new EmbeddingResponse()
                {
                    Content = response,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new EmbeddingResponse()
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