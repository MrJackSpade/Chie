using Llama.Data.Scheduler;
using LlamaApi.Extensions;
using LlamaApi.Interfaces;
using LlamaApi.Models;
using LlamaApi.Services;
using LlamaApi.Utils;

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

            builder.Services.AddControllers();

            builder.Services.RegisterSecret<DatabaseConnectionSettings>(configuration);
            builder.Services.AddSingleton<IHasConnectionString>((s) => s.GetRequiredService<DatabaseConnectionSettings>());
            builder.Services.AddSingleton<IExecutionScheduler, ExecutionScheduler>();
            builder.Services.AddSingleton<ThreadManager>();
            builder.Services.AddSingleton<LoadedModel>();
            builder.Services.AddTransient<IJobService, JobService>();
            builder.Services.AddSingleton<IContextService, ContextService>();
            builder.Services.AddSingleton<JobServiceSettings>();

            WebApplication app = builder.Build();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}