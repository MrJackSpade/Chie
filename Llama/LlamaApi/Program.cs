using Llama.Data.Scheduler;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Services;
using LlamaApi.Shared.Converters;
using LlamaApi.Utils;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

namespace LlamaApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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

            builder.Services.AddSingleton<IExecutionScheduler, ExecutionScheduler>();
            builder.Services.AddSingleton<ThreadManager>();
            builder.Services.AddSingleton<LoadedModel>();
            builder.Services.AddSingleton<IContextService, ContextService>();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/LlamaApi.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog(); // Use the Serilog logger

            WebApplication app = builder.Build();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}