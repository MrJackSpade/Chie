using ImageRecognition;
using Microsoft.Extensions.Options;
using SummaryApi;

internal class Program
{
    private static void Main(string[] args)
    {
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddUserSecrets<Program>();
        IConfigurationRoot configuration = configurationBuilder.Build();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.Configure<SummaryClientSettings>(configuration.GetSection(nameof(SummaryClientSettings)));
        builder.Services.AddSingleton((s => s.GetService<IOptions<SummaryClientSettings>>().Value));
        builder.Services.AddSingleton<SummaryClient>();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}