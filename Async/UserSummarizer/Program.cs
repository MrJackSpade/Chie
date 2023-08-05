using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using ChieApi.Models;
using ChieApi.Shared.Services;
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
            serviceCollection.RegisterSecret<UserSummarizerSettings>(configuration);
            serviceCollection.RegisterSecret<SummaryApiClientSettings>(configuration);

            _ = serviceCollection.AddSingleton<ChatService>();
            _ = serviceCollection.AddSingleton<ILogger, LoggingApiClient>();
            _ = serviceCollection.AddSingleton<SummaryApiClient>();

            _ = serviceCollection.AddSingleton<UserDataService>();
            _ = serviceCollection.AddSingleton<LlamaContextClient>();

            _ = serviceCollection.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<UserSummarizerSettings>>().Value);

            _ = serviceCollection.AddSingleton<UserSummarizer>();

            LlamaClientSettings clientSettings = new("http://192.168.0.93:10030");

            _ = serviceCollection.AddSingleton(clientSettings);

            _ = serviceCollection.AddSingleton(s => new LlamaTokenCache(s.GetService<LlamaContextClient>().Tokenize));

            IServiceProvider provider = serviceCollection.BuildServiceProvider();

            UserSummarizer summarizer = provider.GetService<UserSummarizer>();

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