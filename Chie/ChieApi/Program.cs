using Ai.Utils.Extensions;
using Blip;
using ChieApi.Clients;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Pipelines;
using ChieApi.Pipelines.MoodPipeline;
using ChieApi.Services;
using ChieApi.Shared.Services;
using ChieApi.Tasks.Boredom;
using Llama.Core.Samplers.Mirostat;
using Llama.Data;
using Llama.Data.Models.Settings;
using LlamaApi.Shared.Models.Request;
using LlamaApi.Shared.Models.Response;
using LlamaApiClient;
using Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Summary;
using Summary.Interfaces;
using System.Diagnostics;
using System.Text.Json.Serialization;
using MirostatSamplerSettings = LlamaApi.Models.Request.MirostatSamplerSettings;

namespace ChieApi
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Console.OutputEncoding = System.Text.Encoding.UTF8;
			Console.InputEncoding = System.Text.Encoding.UTF8;
			Console.BufferHeight = Console.WindowHeight;
			Console.BufferWidth = Console.WindowWidth;
			ConfigurationBuilder configurationBuilder = new();
			configurationBuilder.AddUserSecrets<Program>();
			IConfigurationRoot configuration = configurationBuilder.Build();

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			_ = builder.Services.AddControllersWithViews();

			builder.Services.RegisterSecret<LoggingApiClientSettings>(configuration);
			builder.Services.RegisterSecret<MoodPipelineSettings>(configuration);
			builder.Services.RegisterSecret<ChieApiSettings>(configuration);
			builder.Services.RegisterSecret<BlipApiClientSettings>(configuration);
			builder.Services.RegisterSecret<SummaryApiClientSettings>(configuration);
			builder.Services.RegisterSecret<BoredomTaskSettings>(configuration);
            builder.Services.RegisterSecret<LlamaClientSettings>(configuration);

            _ = builder.Services.AddSingleton<ChatRepository>();
			_ = builder.Services.AddSingleton<ILogger, LoggingApiClient>();

			_ = builder.Services.AddSingleton<UserDataRepository>();
			_ = builder.Services.AddSingleton<LogitService>();
			_ = builder.Services.AddSingleton<CharacterService>();
			_ = builder.Services.AddSingleton(s => s.GetRequiredService<CharacterService>().Build());
			_ = builder.Services.AddSingleton<BlipApiClient>();
			_ = builder.Services.AddSingleton<DictionaryCache>();
			_ = builder.Services.AddSingleton<ISummaryApiClient, SummaryApiClient>();
			_ = builder.Services.AddSingleton<LlamaService>();
			_ = builder.Services.AddTransient<IRequestPipeline, ImageRecognitionPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, MessageCleaningPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, NameCleaningPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, ContentSplittingPipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, TimePassagePipeline>();
			_ = builder.Services.AddTransient<IRequestPipeline, BoredomTask>();
			_ = builder.Services.AddSingleton<IRequestPipeline, MoodPipeline>();
			_ = builder.Services.AddSingleton<IRequestPipeline, BootUpPipeline>();
			_ = builder.Services.AddScoped<IRequestPipeline, UserDataPipeline>();
			_ = builder.Services.AddTransient<IBackgroundTask, BoredomTask>();
			_ = builder.Services.AddSingleton<BoredomTaskData>();
			_ = builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
			_ = builder.Services.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<ChieApiSettings>>().Value);
			_ = builder.Services.AddSingleton<LlamaContextModel>();
			_ = builder.Services.AddSingleton(s =>
			{
				LlamaClient client = s.GetService<LlamaClient>();
				return new LlamaTokenCache(client.Tokenize);
			});

			_ = builder.Services.AddSingleton<SummarizationService>();
			_ = builder.Services.AddSingleton<DictionaryRepository>();

			_ = builder.Services.AddSingleton((s) =>
			{
				LlamaClientSettings settings = s.GetRequiredService<LlamaClientSettings>();
				LlamaContextSettings contextSettings = s.GetRequiredService<LlamaContextSettings>();
				LlamaModelSettings llamaModelSettings = s.GetRequiredService<LlamaModelSettings>();
				ContextRequestSettings contextRequestSettings = s.GetRequiredService<ContextRequestSettings>();

				if(settings.Host.Contains("api.runpod.ai", StringComparison.OrdinalIgnoreCase))
				{
					return new RunpodClient(settings, contextSettings, llamaModelSettings, contextRequestSettings);
				} else
				{
                    return new LlamaClient(settings, contextSettings, llamaModelSettings, contextRequestSettings);
                }
            });
			_ = builder.Services.AddSingleton((s) =>
			{
                CharacterConfiguration _characterConfiguration = s.GetRequiredService<CharacterConfiguration>();
				return new LlamaModelSettings()
				{
					GpuLayerCount = _characterConfiguration.GpuLayers,
					Model = _characterConfiguration.ModelPath,
					UseMemoryMap = !_characterConfiguration.NoMemoryMap,
					UseMemoryLock = true
				};
            });

			_ = builder.Services.AddSingleton((s) =>
			{
                CharacterConfiguration _characterConfiguration = s.GetRequiredService<CharacterConfiguration>();

				ContextRequestSettings c = new();

                if (_characterConfiguration.MiroStat != MirostatType.None)
                {
                    if (_characterConfiguration.MiroStat == MirostatType.Three)
                    {
                        c.MirostatTempSamplerSettings = new MirostatTempSamplerSettings()
                        {
                            Target = _characterConfiguration.MiroStatEntropy,
                            LearningRate = _characterConfiguration.LearningRate,
                            InitialTemperature = _characterConfiguration.Temperature,
                            Tfs = _characterConfiguration.Tfs
                        };
                    }
                    else
                    {
                        c.MirostatSamplerSettings = new MirostatSamplerSettings()
                        {
                            Tau = _characterConfiguration.MiroStatEntropy,
                            Temperature = _characterConfiguration.Temperature,
                            MirostatType = _characterConfiguration.MiroStat
                        };
                    }
                }
                else
                {
                    c.TemperatureSamplerSettings = new TemperatureSamplerSettings()
                    {
                        Temperature = _characterConfiguration.Temperature,
                        TopP = _characterConfiguration.TopP,
                    };
                }

                c.RepetitionSamplerSettings = new RepetitionSamplerSettings()
                {
                    RepeatPenalty = _characterConfiguration.RepeatPenalty,
                    RepeatTokenPenaltyWindow = _characterConfiguration.RepeatPenaltyWindow
                };

                c.ComplexPresencePenaltySettings = new ComplexPresencePenaltySettings()
                {
                    LengthScale = 1.8f,
                    GroupScale = 1.575f,
                    MinGroupLength = 3,
                    RepeatTokenPenaltyWindow = -1
                };

				return c;
            });

            _ = builder.Services.AddSingleton((s) =>
            {
                CharacterConfiguration _characterConfiguration = s.GetRequiredService<CharacterConfiguration>();

				return new LlamaContextSettings()
				{
					BatchSize = _characterConfiguration.BatchSize,
					ContextSize = _characterConfiguration.ContextLength,
					EvalThreadCount = _characterConfiguration.Threads ?? (uint)(System.Environment.ProcessorCount / 2),
					MemoryMode = _characterConfiguration.MemoryMode,
					ThreadCount = _characterConfiguration.Threads ?? (uint)(Environment.ProcessorCount / 2),
					RopeFrequencyScaling = 1 / _characterConfiguration.RopeScale,
					RopeFrequencyBase = _characterConfiguration.RopeBase,
					RopeScalingType = _characterConfiguration.RopeScalingType,
					YarnAttnFactor = _characterConfiguration.YarnAttnFactor,
					YarnBetaFast = _characterConfiguration.YarnBetaFast,
					YarnBetaSlow = _characterConfiguration.YarnBetaSlow,
					YarnExtFactor = _characterConfiguration.YarnExtFactor,
					YarnOrigCtx = _characterConfiguration.YarnOrigCtx,
				};
            });

            _ = builder.Services.Configure<JsonOptions>(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

			WebApplication app = builder.Build();

			_ = app.UseAuthorization();

			_ = app.MapControllers();

			CancellationTokenSource cts = new();

			Task t = app.RunAsync(cts.Token);

			//Needs to be executed so client starts before
			//first request
			await Task.Run(async () =>
			{
				await Task.Delay(3000);
				app.Services.GetService<LlamaService>();
			});

			List<IBackgroundTask> tasks = app.Services.GetServices<IBackgroundTask>().ToList();

			foreach (IBackgroundTask task in tasks)
			{
				await task.Initialize();
			}

			do
			{
				await Task.Delay(1000 * 60);
				foreach (IBackgroundTask task in tasks)
				{
					await task.TickMinute();
				}
			} while (!cts.IsCancellationRequested);

			await t;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Console.WriteLine(e.ExceptionObject?.ToString());

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
		}
	}
}