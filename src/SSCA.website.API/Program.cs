using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SSCA.website.API.Data;
using SSCA.website.API.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

// Enable legacy timestamp behavior for Npgsql to handle DateTime properly
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add EF Core with PostgreSQL
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL"));
        });

        // Add caching
        services.AddMemoryCache();

        // Add MeetingService
        services.AddScoped<IMeetingService, MeetingService>();

        // Add EmailService
        services.AddScoped<IEmailService, EmailService>();

        // Add FileStorageService
        services.AddScoped<IFileStorageService, FileStorageService>();

        // Add HeroLinkService
        services.AddScoped<IHeroLinkService, HeroLinkService>();

        // Add Azure Blob Storage
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("AzureStorage") 
                ?? "UseDevelopmentStorage=true";
            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

// Auto-migrate database on startup
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception)
    {
        // Rethrowing to ensure the app doesn't start with a bad schema
        throw;
    }
}

host.Run();
