using ImageRecognition;
using Microsoft.Extensions.Options;

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
        builder.Services.Configure<BlipClientSettings>(configuration.GetSection(nameof(BlipClientSettings)));
        builder.Services.AddSingleton((s => s.GetService<IOptions<BlipClientSettings>>().Value));
        builder.Services.AddSingleton<BlipClient>();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}