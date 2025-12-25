using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SSCA.website.API.Data;
using SSCA.website.API.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

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

        // Add MeetingService
        services.AddScoped<IMeetingService, MeetingService>();

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

host.Run();
