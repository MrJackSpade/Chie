using Ai.Utils.Extensions;
using Chie;
using ChieApi.Client;
using DiscordGpt.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

			serviceCollection.AddSingleton<DiscordIntegrationService>();
			serviceCollection.AddSingleton<NameService>();
			serviceCollection.AddSingleton<ActiveChannelCollection>();
			serviceCollection.AddSingleton<ChieMessageService>();
			serviceCollection.AddSingleton<ChieClient>();
			serviceCollection.AddSingleton<Logger>();
			serviceCollection.AddSingleton<DiscordClient>();
			serviceCollection.AddSingleton<StartInfo>();

			IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

			DiscordIntegrationService discordIntegrationService = serviceProvider.GetService<DiscordIntegrationService>();

			await discordIntegrationService.Start();
		}
	}
}