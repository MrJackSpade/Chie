using Ai.Utils.Extensions;
using ChieApi.Interfaces;
using ChieApi.Shared.Services;
using Llama;
using Llama.Context.Interfaces;
using Llama.Pipeline.ContextRollers;
using Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatVectorizer
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
            serviceCollection.RegisterSecret<ChatVectorizerSettings>(configuration);

            _ = serviceCollection.AddSingleton<ChatService>();
            _ = serviceCollection.AddSingleton<ILogger, LoggingApiClient>();

            _ = serviceCollection.AddSingleton<UserDataService>();

            _ = serviceCollection.AddSingleton<IHasConnectionString>(s => s.GetService<IOptions<ChatVectorizerSettings>>().Value);

            _ = serviceCollection.AddSingleton<IContext>((s) => new ContextEvaluatorBuilder(s.GetRequiredService<ChatVectorizerSettings>()).BuildChatContextWrapper(new DefaultContextRoller()));

            _ = serviceCollection.AddSingleton<ChatVectorizer>();

            IServiceProvider provider = serviceCollection.BuildServiceProvider();

            ChatVectorizer summarizer = provider.GetService<ChatVectorizer>();

            await summarizer.Execute();
        }
    }
}
