using Ai.Utils.Extensions;
using ChieApi;
using ChieApi.Interfaces;
using ChieApi.Shared.Services;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            _ = serviceCollection.AddSingleton<ChatService>();
            _ = serviceCollection.AddSingleton<ILogger, LoggingApiClient>();

            _ = serviceCollection.AddSingleton<UserDataService>();

            _ = serviceCollection.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<UserSummarizerSettings>>().Value);

            _ = serviceCollection.AddSingleton<UserSummarizer>();

            IServiceProvider provider = serviceCollection.BuildServiceProvider();

            UserSummarizer summarizer = provider.GetService<UserSummarizer>();

            await summarizer.Execute();
        }
    }
}