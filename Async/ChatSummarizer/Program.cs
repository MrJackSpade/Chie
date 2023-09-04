using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Repositories;
using ChieApi.Shared.Services;
using Llama.Data.Interfaces;
using LlamaApiClient;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Summary;

namespace UserSummarizer
{
    internal class Program
    {

        private static async Task Main(string[] args)
        {
            ConfigurationBuilder builder = new();
            builder.AddUserSecrets<Program>();
            IConfiguration configuration = builder.Build();

            ServiceCollection serviceCollection = new();

            serviceCollection.RegisterSecret<LoggingApiClientSettings>(configuration);
            serviceCollection.RegisterSecret<ChatSummarizerSettings>(configuration);
            serviceCollection.RegisterSecret<SummaryApiClientSettings>(configuration);

            _ = serviceCollection.AddSingleton<ChatRepository>();
			_ = serviceCollection.AddSingleton<ModelRepository>();

			_ = serviceCollection.AddSingleton<DictionaryCache>();
            _ = serviceCollection.AddSingleton<SummarizationRepository>();

            _ = serviceCollection.AddSingleton<ILogger, LoggingApiClient>();
            _ = serviceCollection.AddSingleton<SummaryApiClient>();

            _ = serviceCollection.AddSingleton<UserDataRepository>();
            _ = serviceCollection.AddSingleton<LlamaContextClient>();
            _ = serviceCollection.AddSingleton<IDictionaryService, DictionaryRepository>();

            _ = serviceCollection.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<ChatSummarizerSettings>>().Value);

            _ = serviceCollection.AddSingleton<ChatSummarizer>();

            LlamaClientSettings clientSettings = new("http://192.168.0.93:10030");

            _ = serviceCollection.AddSingleton(clientSettings);

            IServiceProvider provider = serviceCollection.BuildServiceProvider();

            ChatSummarizer summarizer = provider.GetService<ChatSummarizer>();

            try
            {
                await summarizer.Execute();
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }
    }
}