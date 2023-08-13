using Llama.Data.Scheduler;
using LlamaApi.Extensions;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Services;
using LlamaApi.Shared.Converters;
using LlamaApi.Utils;
using Logging;
using System.Text.Json.Serialization;

namespace LlamaApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddUserSecrets<Program>();

            IConfiguration configuration = configurationBuilder.Build();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(c =>
            {
                c.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                c.JsonSerializerOptions.Converters.Add(new LogitRuleCollectionConverter());
                c.JsonSerializerOptions.Converters.Add(new LogitRuleConverter());
                c.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            builder.Services.RegisterSecret<DatabaseConnectionSettings>(configuration);
            builder.Services.AddSingleton<IHasConnectionString>((s) => s.GetRequiredService<DatabaseConnectionSettings>());
            builder.Services.AddSingleton<IExecutionScheduler, ExecutionScheduler>();
            builder.Services.AddSingleton<ThreadManager>();
            builder.Services.AddSingleton<LoadedModel>();
            builder.Services.AddTransient<IJobService, JobService>();
            builder.Services.AddSingleton<IContextService, ContextService>();
            builder.Services.AddSingleton<JobServiceSettings>();

            LoggingApiClient loggingClient = new(new LoggingApiClientSettings()
            {
                Host = "http://127.0.0.1:10020",
                ApplicationName = "LlamaApi"
            });

            builder.Services.AddSingleton<ILogger>(loggingClient);

            WebApplication app = builder.Build();

            app.UseAuthorization();

            app.MapControllers();

            //app.UseMiddleware<RequestLoggingMiddleware>();

            app.Run();

            loggingClient.Dispose();
        }
    }
}