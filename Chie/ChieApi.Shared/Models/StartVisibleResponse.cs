using System.Text.Json.Serialization;

namespace ChieApi.Shared.Models
{
	public class StartVisibleResponse
	{
		[JsonPropertyName("startVisible")]
		public bool StartVisible { get; set; }
	}
}