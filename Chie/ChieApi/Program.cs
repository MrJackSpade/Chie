using Ai.Utils.Extensions;
using Blip;
using ChieApi.Factories;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Pipelines;
using ChieApi.Pipelines.MoodPipeline;
using ChieApi.Services;
using ChieApi.Shared.Services;
using ChieApi.Tasks.Boredom;
using LlamaApiClient;
using Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Summary;
using Summary.Interfaces;
using System.Diagnostics;
using System.Text.Json.Serialization;

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

            if (args.Length > 0)
            {
                _ = builder.Services.AddSingleton<ICharacterNameFactory>(new CommandLineCharacterNameFactory(args[0]));
            }
            else
            {
                _ = builder.Services.AddSingleton<ICharacterNameFactory, SecretCharacterNameFactory>();
            }

            builder.Services.RegisterSecret<LoggingApiClientSettings>(configuration);
            builder.Services.RegisterSecret<MoodPipelineSettings>(configuration);
            builder.Services.RegisterSecret<ChieApiSettings>(configuration);
            builder.Services.RegisterSecret<BlipApiClientSettings>(configuration);
            builder.Services.RegisterSecret<SummaryApiClientSettings>(configuration);
            builder.Services.RegisterSecret<BoredomTaskSettings>(configuration);

            _ = builder.Services.AddSingleton<ChatService>();
            _ = builder.Services.AddSingleton<ILogger, LoggingApiClient>();

            _ = builder.Services.AddSingleton<UserDataService>();
            _ = builder.Services.AddSingleton<LogitService>();
            _ = builder.Services.AddSingleton<BlipApiClient>();
            _ = builder.Services.AddSingleton<DictionaryCache>();
            _ = builder.Services.AddSingleton<ISummaryApiClient, SummaryApiClient>();
            _ = builder.Services.AddSingleton<LlamaService>();
            _ = builder.Services.AddSingleton<ICharacterFactory, CharacterService>();
            _ = builder.Services.AddTransient<IRequestPipeline, ImageRecognitionPipeline>();
            _ = builder.Services.AddTransient<IRequestPipeline, MessageCleaningPipeline>();
            _ = builder.Services.AddTransient<IRequestPipeline, NameCleaningPipeline>();
            _ = builder.Services.AddTransient<IRequestPipeline, ContentSplittingPipeline>();
            _ = builder.Services.AddTransient<IRequestPipeline, TimePassagePipeline>();
            _ = builder.Services.AddTransient<IRequestPipeline, BoredomTask>();
            _ = builder.Services.AddSingleton<IRequestPipeline, MoodPipeline>();
            //_ = builder.Services.AddSingleton<IBackgroundProcess, UserSummarizer>();
            _ = builder.Services.AddSingleton<IRequestPipeline, BootUpPipeline>();
            _ = builder.Services.AddScoped<IRequestPipeline, UserDataPipeline>();
            _ = builder.Services.AddTransient<IBackgroundTask, BoredomTask>();
            _ = builder.Services.AddSingleton<BoredomTaskData>();
            _ = builder.Services.AddSingleton<IHostLifetime>(new NullLifetime());
            _ = builder.Services.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<ChieApiSettings>>().Value);
            _ = builder.Services.AddSingleton<LlamaContextModel>();
            _ = builder.Services.AddSingleton(s =>
            {
                LlamaContextClient client = s.GetService<LlamaContextClient>();
                return new LlamaTokenCache(client.Tokenize);
            });

            _ = builder.Services.AddSingleton<SummarizationService>();
            _ = builder.Services.AddSingleton<DictionaryService>();

            LlamaClientSettings clientSettings = new("http://localhost:5059");
            _ = builder.Services.AddSingleton(clientSettings);
            _ = builder.Services.AddSingleton<LlamaContextClient>();
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