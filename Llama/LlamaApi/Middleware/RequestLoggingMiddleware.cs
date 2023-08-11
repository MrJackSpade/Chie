using System.Text;

namespace LlamaApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string request = await this.FormatRequest(context.Request);

            this._logger.LogInformation($"Api Request", request);

            Stream originalBodyStream = context.Response.Body;

            using MemoryStream responseBody = new();

            context.Response.Body = responseBody;

            await this._next(context);

            string response = await this.FormatResponse(context.Response);

            this._logger.LogInformation($"Api Response", response);

            await responseBody.CopyToAsync(originalBodyStream);
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            MemoryStream memoryStream = new();

            await request.BodyReader.CopyToAsync(memoryStream);

            request.Body = memoryStream;

            byte[] buffer = memoryStream.ToArray();

            memoryStream.Seek(0, SeekOrigin.Begin);

            string bodyAsText = Encoding.UTF8.GetString(buffer);

            memoryStream.Position = 0;

            return $"{request.Method} {request.Path} {request.QueryString}\n\n{bodyAsText}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            string text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"{response.StatusCode}\n\n{text}";
        }
    }
}
