using ChieApi.Shared.Entities;
using LoggingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlipApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingController : ControllerBase
    {
        private readonly LogService _logService;

        public LoggingController(LogService logService)
        {
            this._logService = logService;
        }

        [HttpPost("Log")]
        public async Task<IActionResult> Log(LogEntry[] request)
        {
            await this._logService.Insert(request);

            return this.Ok();
        }
    }
}