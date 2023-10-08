using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChieApi.Client.Extensions
{
	internal static class HttpClientExtensions
	{
		private static readonly JsonSerializerOptions _options;

		static HttpClientExtensions()
		{
			_options = new JsonSerializerOptions();
			_options.Converters.Add(new JsonStringEnumConverter());
		}

		public static async Task<T?> GetJsonAsync<T>(this HttpClient client, string url)
		{
			string content = await client.GetStringAsync(url);
			return JsonSerializer.Deserialize<T>(content, _options);
		}

		public static async Task<T?> PostJsonAsync<T>(this HttpClient client, string url, object toPost)
		{
			string content = await (await client.PostAsync(url, JsonContent.Create(toPost, null, _options))).Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<T>(content, _options);
		}
	}
}