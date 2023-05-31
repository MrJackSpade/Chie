using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Llama.Models.Response
{
	public class CompletionResponse
	{
		[JsonPropertyName("content")]
		public string? Content { get; set; }
	}
}
