using ImageRecognition;
using Microsoft.Extensions.Options;
using EmbeddingApi;

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
        builder.Services.Configure<EmbeddingClientSettings>(configuration.GetSection(nameof(EmbeddingClientSettings)));
        builder.Services.AddSingleton((s => s.GetService<IOptions<EmbeddingClientSettings>>().Value));
        builder.Services.AddSingleton<EmbeddingClient>();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}