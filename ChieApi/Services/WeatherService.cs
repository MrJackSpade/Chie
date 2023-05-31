using ChieApi.Converters;
using Loxifi;
using System.Text.Json.Serialization;

namespace ChieApi.Services
{
	public record AirQuality([property: JsonPropertyName("co")] double Co, [property: JsonPropertyName("no2")] double No2, [property: JsonPropertyName("o3")] double O3, [property: JsonPropertyName("so2")] double So2, [property: JsonPropertyName("pm2_5")] double Pm25, [property: JsonPropertyName("pm10")] double Pm10, [property: JsonPropertyName("us-epa-index")] int UsEpaIndex, [property: JsonPropertyName("gb-defra-index")] int GbDefraIndex);
	public record Condition([property: JsonPropertyName("text")] string Text, [property: JsonPropertyName("icon")] string Icon, [property: JsonPropertyName("code")] int Code);
	public record Current([property: JsonPropertyName("last_updated_epoch")] int LastUpdatedEpoch, [property: JsonPropertyName("last_updated")] string LastUpdated, [property: JsonPropertyName("temp_c")] double TempC, [property: JsonPropertyName("temp_f")] double TempF, [property: JsonPropertyName("is_day")] int IsDay, [property: JsonPropertyName("condition")] Condition Condition, [property: JsonPropertyName("wind_mph")] double WindMph, [property: JsonPropertyName("wind_kph")] double WindKph, [property: JsonPropertyName("wind_degree")] int WindDegree, [property: JsonPropertyName("wind_dir")] string WindDir, [property: JsonPropertyName("pressure_mb")] double PressureMb, [property: JsonPropertyName("pressure_in")] double PressureIn, [property: JsonPropertyName("precip_mm")] double PrecipMm, [property: JsonPropertyName("precip_in")] double PrecipIn, [property: JsonPropertyName("humidity")] int Humidity, [property: JsonPropertyName("cloud")] int Cloud, [property: JsonPropertyName("feelslike_c")] double FeelslikeC, [property: JsonPropertyName("feelslike_f")] double FeelslikeF, [property: JsonPropertyName("vis_km")] double VisKm, [property: JsonPropertyName("vis_miles")] double VisMiles, [property: JsonPropertyName("uv")] double Uv, [property: JsonPropertyName("gust_mph")] double GustMph, [property: JsonPropertyName("gust_kph")] double GustKph, [property: JsonPropertyName("air_quality")] AirQuality AirQuality);
	public record Location([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("region")] string Region, [property: JsonPropertyName("country")] string Country, [property: JsonPropertyName("lat")] double Lat, [property: JsonPropertyName("lon")] double Lon, [property: JsonPropertyName("tz_id")] string TzId, [property: JsonPropertyName("localtime_epoch")] int LocaltimeEpoch, [property: JsonPropertyName("localtime")][property: JsonConverter(typeof(JsonDateTimeConverter))] DateTime Localtime);
	public record Weather([property: JsonPropertyName("location")] Location Location, [property: JsonPropertyName("current")] Current Current);

	public class WeatherService
	{
		private const string API_KEY = "4d5d2ef6d37b4052aea214808230605";
		private readonly string _query;

		public WeatherService(string query)
		{
			this._query = query;
		}

		public async Task<Weather> Get()
		{
			string url = $"http://api.weatherapi.com/v1/current.json?key={API_KEY}&q={this._query}&aqi=yes";
			return await new JsonClient().GetJsonAsync<Weather>(url);
		}
	}
}