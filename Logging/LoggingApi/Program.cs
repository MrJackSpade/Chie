using Ai.Utils.Extensions;
using LoggingApi.Interfaces;
using LoggingApi.Services;

namespace LoggingApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddUserSecrets<Program>();
            IConfigurationRoot configuration = configurationBuilder.Build();

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.RegisterSecret<LogServiceSettings>(configuration);
            builder.Services.AddSingleton<IHasConnectionString>(p => p.GetService<LogServiceSettings>());
            builder.Services.AddSingleton<LogService>();

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}