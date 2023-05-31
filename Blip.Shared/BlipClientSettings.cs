using System.Text.Json.Serialization;

namespace ImageRecognition
{
	public class BlipClientSettings
	{
		[JsonPropertyName("predictPath")]
		public string PredictPath { get; set; }

		[JsonPropertyName("pythonPath")]
		public string PythonPath { get; set; }
	}
}