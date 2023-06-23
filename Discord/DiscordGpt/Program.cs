using Ai.Utils.Extensions;
using Chie;
using ChieApi.Client;
using ChieApi.Shared.Interfaces;
using DiscordGpt.EmojiReactions;
using DiscordGpt.Interfaces;
using DiscordGpt.Models;
using DiscordGpt.Services;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordGpt
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigurationBuilder builder = new();

            builder.AddUserSecrets<Program>();

            IConfigurationRoot configuration = builder.Build();

            ServiceCollection serviceCollection = new();
            serviceCollection.RegisterSecret<DiscordIntegrationSettings>(configuration);
            serviceCollection.RegisterSecret<DiscordClientSettings>(configuration);
            serviceCollection.RegisterSecret<LoggingApiClientSettings>(configuration);

            serviceCollection.AddSingleton<DiscordIntegrationService>();
            serviceCollection.AddSingleton<NameService>();
            serviceCollection.AddSingleton<ActiveChannelCollection>();
            serviceCollection.AddSingleton<ChieMessageService>();
            serviceCollection.AddSingleton<ChieClient>();
            serviceCollection.AddSingleton<ILogger, LoggingApiClient>();
            serviceCollection.AddSingleton<DiscordClient>();
            serviceCollection.AddSingleton<StartInfo>();
            serviceCollection.AddSingleton<IReactionAction, EyesReaction>();
            serviceCollection.AddSingleton<IReactionAction, ContinueReaction>();
            serviceCollection.AddSingleton<IReactionAction, StopReaction>();

            ActiveMessageContainer activeMessageContainer = new();

            serviceCollection.AddSingleton<ISingletonContainer<ActiveMessage>>(activeMessageContainer);
            serviceCollection.AddSingleton<IReadOnlySingletonContainer<ActiveMessage>>(activeMessageContainer);
            serviceCollection.AddSingleton<IActiveMessageContainer>(activeMessageContainer);
            serviceCollection.AddSingleton<IChieClient>(new ChieClient());

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            DiscordIntegrationService discordIntegrationService = serviceProvider.GetService<DiscordIntegrationService>();

            await discordIntegrationService.Start();
        }
    }
}