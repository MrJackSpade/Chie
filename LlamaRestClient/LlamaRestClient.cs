using Llama.Models.Request;
using Llama.Models.Response;
using Loxifi;
using Loxifi.Settings;
using System.Text;
using System.Text.Json;

namespace Llama
{
	public class LlamaRestClient
	{
		public RunningProcess RunningProcess { get; private set; }

		private readonly JsonClientSettings _jsonClientSettings;

		private readonly LlamaRestClientSettings _settings;

		private readonly string _rootUrl;

		public LlamaRestClient(LlamaRestClientSettings Settings)
		{
			this._settings = Settings;
			this._rootUrl = $"http://{Settings.Host}:{Settings.Port}";
			this._jsonClientSettings = new JsonClientSettings
			{
				JsonSerializerSettings = new JsonSerializerSettings()
				{
					DefaultValueHandling = DefaultValueHandling.IgnoreDefault
				},
				HttpClient = new HttpClient()
				{
					Timeout = TimeSpan.FromDays(1)
				}
			};
		}

		private readonly StringBuilder _startupText = new();

		public bool IsConnected { get; private set; }

		private void Log(object? sender, string? message)
		{
			this._startupText.Append(message);
			Console.Write(message);

			string t = this._startupText.ToString();

			if (t.Contains(this._rootUrl))
			{
				this._startupTaskCompletionSource.SetResult();
			}
		}

		private readonly TaskCompletionSource _startupTaskCompletionSource = new();

		public async Task Connect()
		{
			if (this.IsConnected)
			{
				return;
			}

			Task startupTask = Task.Run(async () =>
			{
				ProcessSettings processSettings = new(this._settings.ServerExePath)
				{
					Arguments = _settings,
					StdOutWrite = Log,
					StdErrWrite = Log
				};

				this.RunningProcess = ProcessRunner.StartAsync(processSettings);
				await this.RunningProcess;
			});

			await this._startupTaskCompletionSource.Task;
			this.IsConnected = true;
		}

		public async IAsyncEnumerable<string> Complete(CompletionRequest request, CancellationToken cancellationToken)
		{
			request.SetAsLoop(true);

			string nextUrl = this._rootUrl + "/next-token";

			JsonClient client = new(this._jsonClientSettings);

			await client.PostJsonAsync<CompletionResponse>(this._rootUrl + "/completion", request);

			do
			{
				if (cancellationToken.IsCancellationRequested)
				{
					nextUrl += "?stop=true";
				}

				NextTokenResponse nextTokenResponse = await client.GetJsonAsync<NextTokenResponse>(nextUrl);

				if (!string.IsNullOrEmpty(nextTokenResponse.Content))
				{
					yield return nextTokenResponse.Content;
				}

				if (nextTokenResponse.Stop)
				{
					yield break;
				}
			} while (true);
		}

		public async Task<string> Complete(CompletionRequest request)
		{
			string jsonContent = JsonSerializer.Serialize(request);

			JsonClient client = new(this._jsonClientSettings);

			CompletionResponse response = await client.PostJsonAsync<CompletionResponse>(this._rootUrl + "/completion", jsonContent);

			return response.Content;
		}
	}
}