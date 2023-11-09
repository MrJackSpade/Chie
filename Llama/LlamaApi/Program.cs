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

            Console.WriteLine("Testing Llama Interop...");

            Llama.Native.NativeApi.Test();

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            app.Run();
        }

        private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            LogError(e?.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogError(ex);
                }
                else
                {
                    Log.Logger.Error(e.ExceptionObject?.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void LogError(Exception? e)
        {
            try
            {
                Console.WriteLine(e!.ToString());

                Log.Logger.Error(e!.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}