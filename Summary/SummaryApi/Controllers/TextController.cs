using Microsoft.AspNetCore.Mvc;
using ImageRecognition;
using Summary.Models;

namespace SummaryApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TextController : ControllerBase
    {

        protected SummaryClient _client;

        private static readonly AutoResetEvent _gate = new(true);

        public TextController(SummaryClient client)
        {
            this._client = client;
        }

        [HttpPost("Tokenize")]
        public async Task<TokenizeResponse> Tokenize(TokenizeRequest request)
        {
            _gate.WaitOne();

            try
            {
                string[] response = await this._client.Tokenize(request.TextData);

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

        [HttpPost("Summarize")]
        public async Task<SummaryResponse> Summarize(SummaryRequest request)
        {
            _gate.WaitOne();

            try
            {
                string response = await this._client.Summarize(request.TextData, request.MaxLength);

                return new SummaryResponse()
                {
                    Content = response,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new SummaryResponse()
                {
                    Content = ex.Message,
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