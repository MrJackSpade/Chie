﻿using ChieApi.Services;
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

		[HttpGet("/")]
		public async Task<IActionResult> Home() => this.View(this._llamaService.LastContextModification);

	}
}
