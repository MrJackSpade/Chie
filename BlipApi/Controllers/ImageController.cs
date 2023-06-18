using Blip.Shared.Models;
using ImageRecognition;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace BlipApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {
        public List<string> RemoveRegex = new()
        {
            "^there is ",
            "(^|\\s)arafed ",
            "\\r\\n.*"
        };

        protected BlipClient _client;

        private static readonly AutoResetEvent _gate = new(true);

        public ImageController(BlipClient client)
        {
            this._client = client;
        }

        [HttpPost("Describe")]
        public async Task<DescribeResponse> Describe(DescribeRequest request)
        {
            _gate.WaitOne();

            try
            {
                byte[] data;
                if (!string.IsNullOrEmpty(request.FilePath))
                {
                    if (request.FilePath.StartsWith("http"))
                    {
                        data = await new HttpClient().GetByteArrayAsync(request.FilePath);
                    }
                    else
                    {
                        data = System.IO.File.ReadAllBytes(request.FilePath);
                    }
                }
                else if (request.FileData != null && request.FileData.Length > 0)
                {
                    data = request.FileData;
                }
                else
                {
                    throw new Exception("File path or data must be provided");
                }

                string response = await this._client.Describe(data);

                foreach (string removeRegex in this.RemoveRegex)
                {
                    response = Regex.Replace(response, removeRegex, "", RegexOptions.IgnoreCase);
                }

                while (response.Contains("  "))
                {
                    response = response.Replace("  ", " ");
                }

                return new DescribeResponse()
                {
                    Content = response,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new DescribeResponse()
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