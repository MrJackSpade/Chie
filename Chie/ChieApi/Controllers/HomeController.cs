using ChieApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChieApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly LlamaService _llamaService;

        public HomeController(LlamaService llamaService)
        {
            this._llamaService = llamaService;
        }

        [HttpGet("/Context")]
        public IActionResult Context() => this.View(this._llamaService.Context);

        [HttpGet("/")]
        public IActionResult Home() => this.View();
    }
}